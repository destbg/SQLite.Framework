using SQLite.Framework.Enums;
using SQLite.Framework.Exceptions;
using SQLite.Framework.Internals.Helpers;
using SQLitePCL;

namespace SQLite.Framework;

/// <summary>
/// Represents a data reader for reading a forward-only stream of rows from a SQLite database.
/// </summary>
public class SQLiteDataReader : IDisposable
{
    private readonly IDisposable connectionLock;
    private readonly sqlite3 handle;

    internal readonly sqlite3_stmt Statement;

    /// <summary>
    /// Initializes a new instance of the <see cref="SQLiteDataReader" /> class.
    /// </summary>
    public SQLiteDataReader(sqlite3 handle, sqlite3_stmt statement, IDisposable connectionLock, SQLiteDatabase database)
    {
        this.handle = handle;
        this.connectionLock = connectionLock;
        Database = database;
        Statement = statement;
    }

    /// <summary>
    /// The database the reader was created for.
    /// </summary>
    public SQLiteDatabase Database { get; }

    /// <summary>
    /// The storage options used by the data reader, which may affect how certain types are read from the database.
    /// </summary>
    public SQLiteOptions Options => Database.Options;

    /// <summary>
    /// The number of columns in the current row.
    /// </summary>
    public int FieldCount => raw.sqlite3_column_count(Statement);

    /// <inheritdoc />
    public void Dispose()
    {
        raw.sqlite3_finalize(Statement);
        connectionLock.Dispose();
    }

    /// <summary>
    /// Reads the next row from the data reader.
    /// </summary>
    /// <returns>If the next row was retrieved.</returns>
    /// <exception cref="SQLiteException">When reading past the max rows.</exception>
    public bool Read()
    {
        SQLiteResult result = (SQLiteResult)raw.sqlite3_step(Statement);
        if (result == SQLiteResult.Row)
        {
            return true;
        }
        else if (result == SQLiteResult.Done)
        {
            return false;
        }

        throw new SQLiteException(result, raw.sqlite3_errmsg(handle).utf8_to_string(), null);
    }

    /// <summary>
    /// Gets the name of the column at the specified index.
    /// </summary>
    public string GetName(int index)
    {
        return raw.sqlite3_column_name(Statement, index).utf8_to_string();
    }

    /// <summary>
    /// Gets the type of the column at the specified index.
    /// </summary>
    public SQLiteColumnType GetColumnType(int index)
    {
        return (SQLiteColumnType)raw.sqlite3_column_type(Statement, index);
    }

    /// <summary>
    /// Gets the value of the column at the specified index.
    /// </summary>
    public object? GetValue(int index, SQLiteColumnType columnType, Type type)
    {
        return CommandHelpers.ReadColumnValue(Statement, index, columnType, type, Options);
    }

    /// <summary>
    /// Returns true when the column at <paramref name="index" /> is SQL NULL.
    /// </summary>
    public bool IsDBNull(int index)
    {
        return raw.sqlite3_column_type(Statement, index) == raw.SQLITE_NULL;
    }

    /// <summary>
    /// Reads a 32-bit signed integer column directly. NULL columns return 0.
    /// </summary>
    public int GetInt32(int index)
    {
        return raw.sqlite3_column_int(Statement, index);
    }

    /// <summary>
    /// Reads a 64-bit signed integer column directly. NULL columns return 0.
    /// </summary>
    public long GetInt64(int index)
    {
        return raw.sqlite3_column_int64(Statement, index);
    }

    /// <summary>
    /// Reads a double-precision floating-point column directly. NULL columns return 0.0.
    /// </summary>
    public double GetDouble(int index)
    {
        return raw.sqlite3_column_double(Statement, index);
    }

    /// <summary>
    /// Reads a boolean column stored as INTEGER. Any non-zero value reads as <see langword="true" />.
    /// </summary>
    public bool GetBoolean(int index)
    {
        return raw.sqlite3_column_int(Statement, index) != 0;
    }
}