namespace SQLite.Framework.Enums;

/// <summary>
/// A built-in SQLite default value that maps to one of the deterministic time keywords.
/// Pass one of these to <see cref="SQLiteSchema.AddColumn{T}(string, SQLiteColumnDefault)" /> when
/// you want the database to fill the column with the current time on insert and as a backfill
/// value for existing rows.
/// </summary>
public enum SQLiteColumnDefault
{
    /// <summary>
    /// Maps to <c>CURRENT_TIME</c>. Stores the current UTC time as a string like <c>HH:MM:SS</c>.
    /// </summary>
    CurrentTime,

    /// <summary>
    /// Maps to <c>CURRENT_DATE</c>. Stores the current UTC date as a string like <c>YYYY-MM-DD</c>.
    /// </summary>
    CurrentDate,

    /// <summary>
    /// Maps to <c>CURRENT_TIMESTAMP</c>. Stores the current UTC date and time as a string like
    /// <c>YYYY-MM-DD HH:MM:SS</c>.
    /// </summary>
    CurrentTimestamp,
}
