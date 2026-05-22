using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SQLite.Framework.Models;

/// <summary>
/// Represents a column in a database table.
/// </summary>
public class TableColumn
{
    private object? clrDefaultBox;
    private bool clrDefaultBoxComputed;

    /// <summary>
    /// Initializes a new instance of the <see cref="TableColumn"/> class.
    /// </summary>
    public TableColumn(PropertyInfo property, SQLiteOptions options, bool isFtsRowId = false)
    {
        ColumnAttribute? columnAttribute = property.GetCustomAttribute<ColumnAttribute>();
        Type type = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
        KeyAttribute? keyProperty = property.GetCustomAttribute<KeyAttribute>();
        NullabilityInfoContext nullabilityInfoContext = new();

        PropertyInfo = property;
        Name = isFtsRowId ? "rowid" : columnAttribute?.Name ?? property.Name;
        PropertyType = type;
        Indices = property.GetCustomAttributes<IndexedAttribute>().ToArray();
        IsPrimaryKey = keyProperty != null;
        IsAutoIncrement = property.GetCustomAttribute<AutoIncrementAttribute>() != null;
        IsFtsRowId = isFtsRowId;
        IsNullable = !IsPrimaryKey && !isFtsRowId && (
            Nullable.GetUnderlyingType(property.PropertyType) != null
            || nullabilityInfoContext.Create(property).ReadState == NullabilityState.Nullable
        );

        if (IsNullable && property.GetCustomAttribute<RequiredAttribute>() != null)
        {
            IsNullable = false;
        }

        ColumnType = TypeHelpers.TypeToSQLiteType(type, options);
        ReferencesTableAttribute = property.GetCustomAttribute<ReferencesTableAttribute>();
        ForeignKeyAttribute = property.GetCustomAttribute<ForeignKeyAttribute>();

        DefaultValueAttribute? defaultValueAttribute = property.GetCustomAttribute<DefaultValueAttribute>();
        if (defaultValueAttribute != null)
        {
            DefaultSql = SqlLiteralHelper.FormatLiteral(defaultValueAttribute.Value);
        }

        if (ReferencesTableAttribute != null && ForeignKeyAttribute != null)
        {
            throw new InvalidOperationException(
                $"Property '{property.DeclaringType!.Name}.{property.Name}' carries both [ReferencesTable] and " +
                "[System.ComponentModel.DataAnnotations.Schema.ForeignKey]. Pick one or the other, but not both.");
        }
    }

    /// <summary>
    /// The name of the column in the database.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// The property info of the column in the class.
    /// </summary>
    public PropertyInfo PropertyInfo { get; }

    /// <summary>
    /// The indices associated with the column, if any.
    /// </summary>
    public IReadOnlyList<IndexedAttribute> Indices { get; }

    /// <summary>
    /// The type of the property in the class.
    /// </summary>
    public Type PropertyType { get; }

    /// <summary>
    /// The type of the column in the database.
    /// </summary>
    public SQLiteColumnType ColumnType { get; }

    /// <summary>
    /// Indicates whether the column is a primary key.
    /// </summary>
    public bool IsPrimaryKey { get; }

    /// <summary>
    /// Indicates whether the column is an auto-incrementing primary key.
    /// </summary>
    public bool IsAutoIncrement { get; }

    /// <summary>
    /// Indicates whether the column is the implicit <c>rowid</c> on an FTS5 virtual table.
    /// </summary>
    public bool IsFtsRowId { get; }

    /// <summary>
    /// Indicates whether the column can be null.
    /// </summary>
    public bool IsNullable { get; }

    /// <summary>
    /// The <see cref="Attributes.ReferencesTableAttribute" /> on the property, if any.
    /// Mutually exclusive with <see cref="ForeignKeyAttribute" />.
    /// </summary>
    public ReferencesTableAttribute? ReferencesTableAttribute { get; }

    /// <summary>
    /// The EF-style
    /// <see cref="System.ComponentModel.DataAnnotations.Schema.ForeignKeyAttribute" /> on the
    /// property, if any. The framework reads <c>Name</c> as the target class name and infers the
    /// primary key.
    /// </summary>
    public ForeignKeyAttribute? ForeignKeyAttribute { get; }

    /// <summary>
    /// The resolved foreign key for this column, or <see langword="null" /> when the column does
    /// not carry a single-column foreign key. Composite keys live on <see cref="TableMapping" />.
    /// </summary>
    public ForeignKeyInfo? ForeignKey { get; internal set; }

    /// <summary>
    /// The raw SQL fragment used in this column's <c>DEFAULT</c> clause, or <see langword="null" />
    /// when no default is configured. Set from a <see cref="DefaultValueAttribute" /> on the
    /// property, or by the fluent builder. When set, the framework also omits this column from
    /// <c>Add</c> inserts whenever the CLR value equals <c>default(T)</c>, so SQLite applies the
    /// configured default instead.
    /// </summary>
    public string? DefaultSql { get; internal set; }

    /// <summary>
    /// Whether <see cref="DefaultSql" /> is set. When <see langword="true" />, the framework will
    /// omit this column from <c>Add</c> inserts whenever the CLR value equals <c>default(T)</c>.
    /// </summary>
    public bool HasDatabaseDefault => DefaultSql != null;

    /// <summary>
    /// Returns true when <paramref name="value" /> equals <c>default</c> for the property's
    /// declared type. Used to decide whether to omit the column from an <c>Add</c> insert when
    /// the column has a database default.
    /// </summary>
    [UnconditionalSuppressMessage("AOT", "IL2072", Justification = "Activator.CreateInstance is only called for value types, which always have a public parameterless constructor.")]
    public bool IsClrDefaultValue(object? value)
    {
        if (!clrDefaultBoxComputed)
        {
            Type declaredType = PropertyInfo.PropertyType;
            clrDefaultBox = declaredType.IsValueType ? Activator.CreateInstance(declaredType) : null;
            clrDefaultBoxComputed = true;
        }
        return Equals(clrDefaultBox, value);
    }

    /// <summary>
    /// Gets the SQL statement to create the column in the database.
    /// </summary>
    /// <param name="emitInlinePrimaryKey">
    /// When false, do not emit inline <c>PRIMARY KEY</c> or <c>AUTOINCREMENT</c>. The caller is
    /// expected to add a table-level <c>PRIMARY KEY (col1, col2)</c> for a composite key.
    /// </param>
    /// <param name="defaultOverride">
    /// When non-null, the framework emits <c>DEFAULT &lt;value&gt;</c> using this string and
    /// ignores <see cref="DefaultSql" />. When null, the framework emits <c>DEFAULT</c> from
    /// <see cref="DefaultSql" /> if it is set.
    /// </param>
    public string GetCreateColumnSql(bool emitInlinePrimaryKey = true, string? defaultOverride = null)
    {
        string columnType = ColumnType.ToString().ToUpperInvariant();
        bool inlinePk = emitInlinePrimaryKey && IsPrimaryKey;
        string nullability = inlinePk ? string.Empty : IsNullable ? "NULL" : "NOT NULL";
        string primaryKey = inlinePk ? "PRIMARY KEY" : string.Empty;
        string autoIncrement = inlinePk && IsAutoIncrement ? "AUTOINCREMENT" : string.Empty;

        StringBuilder sb = new();
        sb.Append(Name);
        sb.Append(' ');
        sb.Append(columnType);
        if (nullability.Length > 0)
        {
            sb.Append(' ');
            sb.Append(nullability);
        }
        if (primaryKey.Length > 0)
        {
            sb.Append(' ');
            sb.Append(primaryKey);
        }
        if (autoIncrement.Length > 0)
        {
            sb.Append(' ');
            sb.Append(autoIncrement);
        }
        if (ForeignKey != null)
        {
            sb.Append(' ');
            ForeignKey.WriteSql(sb, inline: true);
        }

        string? defaultSql = defaultOverride ?? DefaultSql;
        if (defaultSql != null)
        {
            sb.Append(" DEFAULT ");
            sb.Append(defaultSql);
        }
        return sb.ToString();
    }
}
