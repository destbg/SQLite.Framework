namespace SQLite.Framework;

/// <summary>
/// Collects column values to write while a table is rebuilt during <c>Migrate(...)</c>. Use it to
/// fill a new <c>NOT NULL</c> column that has no default, or to recompute a column from the old
/// row. Each value is read from the old row and inserted into the rebuilt table. A column not set
/// here is copied across unchanged when it still exists.
/// </summary>
public sealed class SQLiteMigrationBuilder<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] T>
{
    private readonly SQLiteDatabase database;
    private readonly TableMapping mapping;
    private readonly List<(string Column, string ValueSql)> sets = [];

    internal SQLiteMigrationBuilder(SQLiteDatabase database, TableMapping mapping)
    {
        this.database = database;
        this.mapping = mapping;
    }

    internal IReadOnlyList<(string Column, string ValueSql)> Sets => sets;

    /// <summary>
    /// Sets the target column to a constant value. The target is a property on
    /// <typeparamref name="T" /> or <c>SQLiteColumn.Of&lt;TValue&gt;(row, "Name")</c> for a column
    /// with no CLR property.
    /// </summary>
    public SQLiteMigrationBuilder<T> Set<TValue>(Expression<Func<T, TValue>> column, TValue value)
    {
        sets.Add((ColumnTargetResolver.Resolve(mapping, column), SqlLiteralHelper.FormatLiteral(value)));
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
        sets.Add((ColumnTargetResolver.Resolve(mapping, column), BareSqlTranslator.Translate(database, mapping, value)));
        return this;
    }
}
