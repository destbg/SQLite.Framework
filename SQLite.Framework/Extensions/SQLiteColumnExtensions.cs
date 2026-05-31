namespace SQLite.Framework.Extensions;

/// <summary>
/// Lets a translated expression reference a column by its database name, including columns that
/// have no CLR property (such as shadow columns declared with
/// <see cref="SQLiteEntityTypeBuilder{T}.Column" />).
/// </summary>
public static class SQLiteColumnExtensions
{
    /// <summary>
    /// References the column named <paramref name="name" /> on the row, typed as
    /// <typeparamref name="TValue" />. This is only meaningful inside an expression the framework
    /// translates to SQL: CHECK constraints, computed columns, index filters, UPSERT set/where, and
    /// the value expressions of <c>Migrate(...)</c>. It is never executed as C#, so calling it
    /// directly throws.
    /// </summary>
    /// <param name="row">The row the column belongs to.</param>
    /// <param name="name">The database column name.</param>
    public static TValue Column<TValue>(this object row, string name)
    {
        throw new InvalidOperationException(
            "Column<TValue>(name) is only valid inside an expression that the framework translates to SQL.");
    }
}
