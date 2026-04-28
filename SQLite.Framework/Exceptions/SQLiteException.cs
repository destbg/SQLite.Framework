namespace SQLite.Framework.Exceptions;

/// <summary>
/// Represents errors that occur during SQLite operations.
/// </summary>
public class SQLiteException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SQLiteException" /> class with a specified error code and message.
    /// </summary>
    public SQLiteException(SQLiteResult result, string message, string? sql) : base(message)
    {
        Result = result;
        Sql = sql;
    }

    /// <summary>
    /// The SQLite result code associated with the error.
    /// </summary>
    public SQLiteResult Result { get; }

    /// <summary>
    /// The executed SQL that resulted in the error.
    /// </summary>
    public string? Sql { get; }
}