namespace SQLite.Framework.Internals.Helpers;

internal static class CollationHelper
{
    public static string Clause(SQLiteCollation collation)
    {
        return collation switch
        {
            SQLiteCollation.Inherit => string.Empty,
            SQLiteCollation.Binary => " COLLATE BINARY",
            SQLiteCollation.NoCase => " COLLATE NOCASE",
            SQLiteCollation.Rtrim => " COLLATE RTRIM",
            _ => throw new ArgumentOutOfRangeException(nameof(collation), collation, null),
        };
    }
}
