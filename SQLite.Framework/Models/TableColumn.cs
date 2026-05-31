using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SQLite.Framework.Models;

/// <summary>
/// Represents a column in a database table.
/// </summary>
public class TableColumn
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TableColumn"/> class.
    /// </summary>
    [UnconditionalSuppressMessage("AOT", "IL2072", Justification = "Activator.CreateInstance is only called for value types, which always have a public parameterless constructor.")]
    public TableColumn(PropertyInfo property, SQLiteOptions options, bool isFtsRowId = false)
    {
        ColumnAttribute? columnAttribute = property.GetCustomAttribute<ColumnAttribute>();
        Type type = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
        KeyAttribute? keyProperty = property.GetCustomAttribute<KeyAttribute>();
        NullabilityInfoContext nullabilityInfoContext = new();

        PropertyInfo = property;
        Name = isFtsRowId ? "rowid" : columnAttribute?.Name ?? property.Name;
        IdentifierGuard.EnsureNoQuote(Name, "Column");
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
            DefaultSql = defaultValueAttribute.Value is Enum enumDefault && options.EnumStorage == EnumStorageMode.Text
                ? SqlLiteralHelper.FormatLiteral(enumDefault.ToString())
                : SqlLiteralHelper.FormatLiteral(defaultValueAttribute.Value);
        }

        if (ReferencesTableAttribute != null && ForeignKeyAttribute != null)
        {
            throw new InvalidOperationException(
                $"Property '{property.DeclaringType!.Name}.{property.Name}' carries both [ReferencesTable] and " +
                "[System.ComponentModel.DataAnnotations.Schema.ForeignKey]. Pick one or the other, but not both.");
        }

        ClrDefaultBox = property.PropertyType.IsValueType ? Activator.CreateInstance(property.PropertyType) : null;
    }

    /// <summary>
    /// The name of the column in the database.
    /// </summary>
    public string Name { get; internal set; }

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
    public SQLiteColumnType ColumnType { get; internal set; }

    /// <summary>
    /// Indicates whether the column is a primary key.
    /// </summary>
    public bool IsPrimaryKey { get; internal set; }

    /// <summary>
    /// Indicates whether the column is an auto-incrementing primary key.
    /// </summary>
    public bool IsAutoIncrement { get; internal set; }

    /// <summary>
    /// Indicates whether the column is the implicit <c>rowid</c> on an FTS5 virtual table.
    /// </summary>
    public bool IsFtsRowId { get; }

    /// <summary>
    /// Indicates whether the column can be null.
    /// </summary>
    public bool IsNullable { get; internal set; }

    /// <summary>
    /// The <see cref="Attributes.ReferencesTableAttribute" /> on the property, if any.
    /// Mutually exclusive with <see cref="ForeignKeyAttribute" />.
    /// </summary>
    public ReferencesTableAttribute? ReferencesTableAttribute { get; }

    /// <summary>
    /// The framework reads <c>Name</c> as the target class name and infers the primary key.
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
    /// The boxed <c>default</c> value for the property's declared type (zero for value types,
    /// <see langword="null" /> for reference and nullable types). The framework omits the column
    /// from an <c>Add</c> insert when the value to insert equals this and the column has a database
    /// default.
    /// </summary>
    internal object? ClrDefaultBox { get; }
}
