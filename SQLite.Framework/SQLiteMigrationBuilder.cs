namespace SQLite.Framework;

/// <summary>
/// Collects column values to write while a table is rebuilt during <c>Migrate(...)</c>. Use it to
/// fill a new <c>NOT NULL</c> column that has no default or to recompute a column from the old
/// row. Each value is read from the old row and inserted into the rebuilt table. A column not set
/// here is copied across unchanged when it still exists.
/// </summary>
public sealed class SQLiteMigrationBuilder<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] T>
{
    private readonly SQLiteDatabase database;
    private readonly TableMapping mapping;
    private readonly List<MigrationSetValue> sets = [];

    internal SQLiteMigrationBuilder(SQLiteDatabase database, TableMapping mapping)
    {
        this.database = database;
        this.mapping = mapping;
    }

    internal IReadOnlyList<MigrationSetValue> Sets => sets;

    /// <summary>
    /// Sets the target column to a constant value. The target is a property on
    /// <typeparamref name="T" /> or <c>SQLiteColumn.Of&lt;TValue&gt;(row, "Name")</c> for a column
    /// with no CLR property.
    /// </summary>
    public SQLiteMigrationBuilder<T> Set<TValue>(Expression<Func<T, TValue>> column, TValue value)
    {
        sets.Add(new MigrationSetValue
        {
            Column = ResolveWritableColumn(column),
            ValueSql = ConverterSql.WrapParameter(SqlLiteralHelper.FormatLiteral(value, database.Options), typeof(TValue), database.Options),
        });
        return this;
    }

    /// <summary>
    /// Sets the target column to an expression evaluated over the old row. The target is a property
    /// on <typeparamref name="T" /> or <c>SQLiteColumn.Of&lt;TValue&gt;(row, "Name")</c>. The value
    /// expression may read any column of the old row, including ones with no CLR property through
    /// <c>SQLiteColumn.Of&lt;TValue&gt;(row, "Name")</c>.
    /// </summary>
    public SQLiteMigrationBuilder<T> Set<TValue>(Expression<Func<T, TValue>> column, Expression<Func<T, TValue>> value)
    {
        SetReadColumnCollector reads = new(mapping, value.Parameters[0]);
        reads.Visit(CommonHelpers.Inline(value.Body));
        sets.Add(new MigrationSetValue
        {
            Column = ResolveWritableColumn(column),
            ValueSql = BareSqlTranslator.Translate(database, mapping, value),
            ReadColumns = reads.Columns,
        });
        return this;
    }

    private string ResolveWritableColumn<TValue>(Expression<Func<T, TValue>> column)
    {
        string name = CommonHelpers.Resolve(mapping, column);
        if (mapping.ComputedColumns.Any(c => string.Equals(c.Column.Name, name, StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException(
                $"Column '{name}' on table '{mapping.TableName}' is computed, so a migration fill cannot write it. Remove the Set call and let SQLite compute the value.");
        }

        if (!mapping.Columns.Any(c => string.Equals(c.Name, name, StringComparison.OrdinalIgnoreCase))
            && !mapping.ShadowColumns.Any(s => string.Equals(s.Name, name, StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException(
                $"Column '{name}' is not a column on table '{mapping.TableName}'. Set can only fill a mapped column or a shadow column declared in OnModelCreating.");
        }

        return name;
    }
}
