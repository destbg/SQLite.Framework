namespace SQLite.Framework;

/// <summary>
/// References a column by its database name inside an expression that the framework translates to
/// SQL. This includes columns that have no CLR property, such as shadow columns declared with
/// <see cref="SQLiteEntityTypeBuilder{T}.Column" />.
/// </summary>
public static class SQLiteColumn
{
    /// <summary>
    /// References the column named <paramref name="name" /> on <paramref name="row" />, typed as
    /// <typeparamref name="TValue" />. It is only meaningful inside an expression the framework
    /// translates to SQL: a query (<c>Where</c>, <c>Select</c>, <c>OrderBy</c>, <c>GroupBy</c>,
    /// <c>Join</c>), CHECK constraints, computed columns, index filters, UPSERT set and where and the
    /// value expressions of <c>Migrate(...)</c>. It is never executed as C#, so calling it directly throws.
    /// </summary>
    /// <param name="row">The row the column belongs to.</param>
    /// <param name="name">The database column name.</param>
    public static TValue Of<TValue>(object row, string name)
    {
        throw new InvalidOperationException(
            "SQLiteColumn.Of<TValue>(row, name) is only valid inside an expression that the framework translates to SQL.");
    }
}
