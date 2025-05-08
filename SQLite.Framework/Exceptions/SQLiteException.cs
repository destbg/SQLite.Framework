using SQLite.Framework.Enums;

namespace SQLite.Framework.Exceptions;

#pragma warning disable RCS1194

/// <summary>
/// Represents errors that occur during SQLite operations.
/// </summary>
public class SQLiteException : Exception
{
    /// <summary>
    /// The SQLite result code associated with the error.
    /// </summary>
    public SQLiteResult Result { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="SQLiteException"/> class with a specified error code and message.
    /// </summary>
    public SQLiteException(SQLiteResult result, string message) : base(message)
    {
        Result = result;
    }
}
