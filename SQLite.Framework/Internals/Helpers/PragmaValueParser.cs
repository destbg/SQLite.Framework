namespace SQLite.Framework.Internals.Helpers;

/// <summary>
/// Parses raw PRAGMA result strings into their typed enum values.
/// </summary>
internal static class PragmaValueParser
{
    public static SQLiteEncoding ParseEncoding(string? value)
    {
        return value switch
        {
            "UTF-8" => SQLiteEncoding.Utf8,
            "UTF-16le" => SQLiteEncoding.Utf16le,
            "UTF-16be" => SQLiteEncoding.Utf16be,
            _ => throw new InvalidOperationException($"Unrecognized PRAGMA encoding value '{value ?? "<null>"}'."),
        };
    }

    public static SQLiteLockingMode ParseLockingMode(string? value)
    {
        return value switch
        {
            "normal" => SQLiteLockingMode.Normal,
            "exclusive" => SQLiteLockingMode.Exclusive,
            _ => throw new InvalidOperationException($"Unrecognized PRAGMA locking_mode value '{value ?? "<null>"}'."),
        };
    }

    public static SQLiteJournalMode ParseJournalMode(string? value)
    {
        return value switch
        {
            "delete" => SQLiteJournalMode.Delete,
            "truncate" => SQLiteJournalMode.Truncate,
            "persist" => SQLiteJournalMode.Persist,
            "memory" => SQLiteJournalMode.Memory,
            "wal" => SQLiteJournalMode.Wal,
            "off" => SQLiteJournalMode.Off,
            _ => throw new InvalidOperationException($"Unrecognized PRAGMA journal_mode value '{value ?? "<null>"}'."),
        };
    }
}
