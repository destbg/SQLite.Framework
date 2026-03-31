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
    public SQLiteDataReader(sqlite3 handle, sqlite3_stmt statement, IDisposable connectionLock, SQLiteStorageOptions storageOptions)
    {
        this.handle = handle;
        this.connectionLock = connectionLock;
        StorageOptions = storageOptions;
        Statement = statement;
    }

    /// <summary>
    /// The storage options used by the data reader, which may affect how certain types are read from the database.
    /// </summary>
    public SQLiteStorageOptions StorageOptions { get; }

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
        return CommandHelpers.ReadColumnValue(Statement, index, columnType, type, StorageOptions);
    }
}