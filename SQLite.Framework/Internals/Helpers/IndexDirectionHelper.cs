namespace SQLite.Framework.Internals.Helpers;

internal static class IndexDirectionHelper
{
    public static string Clause(SQLiteIndexDirection direction)
    {
        return direction switch
        {
            SQLiteIndexDirection.Inherit => string.Empty,
            SQLiteIndexDirection.Ascending => " ASC",
            SQLiteIndexDirection.Descending => " DESC",
            _ => throw new ArgumentOutOfRangeException(nameof(direction), direction, null),
        };
    }
}
