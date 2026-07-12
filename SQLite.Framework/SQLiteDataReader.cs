namespace SQLite.Framework;

/// <summary>
/// Represents a data reader for reading a forward-only stream of rows from a SQLite database.
/// </summary>
public class SQLiteDataReader : IDisposable
{
    private readonly IDisposable connectionLock;
    private readonly sqlite3 handle;
    private bool disposed;
    private int readCount;

    internal readonly sqlite3_stmt Statement;

    /// <summary>
    /// Initializes a new instance of the <see cref="SQLiteDataReader" /> class.
    /// </summary>
    public SQLiteDataReader(sqlite3 handle, sqlite3_stmt statement, IDisposable connectionLock, SQLiteCommand command)
    {
        this.handle = handle;
        this.connectionLock = connectionLock;
        Command = command;
        Statement = statement;
    }

    /// <summary>
    /// The command that produced this reader.
    /// </summary>
    public SQLiteCommand Command { get; }

    /// <summary>
    /// The database the reader was created for.
    /// </summary>
    public SQLiteDatabase Database => Command.Database;

    /// <summary>
    /// The storage options used by the data reader, which may affect how certain types are read from the database.
    /// </summary>
    public SQLiteOptions Options => Database.Options;

    /// <summary>
    /// The number of columns in the current row.
    /// </summary>
    public int FieldCount
    {
        get
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            return raw.sqlite3_column_count(Statement);
        }
    }

    /// <summary>
    /// When set, the statement is returned to the database statement pool on dispose instead of being
    /// finalized, so the next query with the same SQL can reuse it. Left null for readers created
    /// directly through the public constructor, which keep the finalize on dispose behavior.
    /// </summary>
    internal string? PooledSql { get; init; }

    /// <inheritdoc />
    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;

        try
        {
            Command.NotifyReaderClosing(this, readCount);
        }
        finally
        {
            if (PooledSql != null)
            {
                Database.ReturnStatement(PooledSql, Statement);
            }
            else
            {
                raw.sqlite3_finalize(Statement);
            }

            connectionLock.Dispose();
        }
    }

    /// <summary>
    /// Reads the next row from the data reader.
    /// </summary>
    /// <returns>If the next row was retrieved.</returns>
    /// <exception cref="SQLiteException">When reading past the max rows.</exception>
    public bool Read()
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        SQLiteResult result = (SQLiteResult)raw.sqlite3_step(Statement);
        if (result == SQLiteResult.Row)
        {
            readCount++;
            Command.NotifyRowRead(this);
            return true;
        }
        else if (result == SQLiteResult.Done)
        {
            return false;
        }

        SQLiteException exception = new(result, raw.sqlite3_errmsg(handle).utf8_to_string(), null);
        Command.NotifyFailed(exception);
        throw exception;
    }

    /// <summary>
    /// Gets the name of the column at the specified index.
    /// </summary>
    public string GetName(int index)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        return raw.sqlite3_column_name(Statement, index).utf8_to_string();
    }

    /// <summary>
    /// Gets the type of the column at the specified index.
    /// </summary>
    public SQLiteColumnType GetColumnType(int index)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        return (SQLiteColumnType)raw.sqlite3_column_type(Statement, index);
    }

    /// <summary>
    /// Gets the value of the column at the specified index.
    /// </summary>
    public object? GetValue(int index, SQLiteColumnType columnType, Type type)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        return CommandHelpers.ReadColumnValue(Statement, index, columnType, type, Options);
    }

    /// <summary>
    /// Returns true when the column at <paramref name="index" /> is SQL NULL.
    /// </summary>
    public bool IsDBNull(int index)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        return raw.sqlite3_column_type(Statement, index) == raw.SQLITE_NULL;
    }

    /// <summary>
    /// Returns true when a custom type converter is registered for <paramref name="type" />.
    /// Generated materializers call this to decide whether to read a column through the converter
    /// instead of the fast direct accessor, matching the reflection path.
    /// </summary>
    public bool HasConverter(Type type)
    {
        return Options.TypeConverters.Count != 0 && Options.TypeConverters.ContainsKey(type);
    }

    /// <summary>
    /// Reads a 32-bit signed integer column directly. NULL columns return 0.
    /// </summary>
    public int GetInt32(int index)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        return raw.sqlite3_column_int(Statement, index);
    }

    /// <summary>
    /// Reads a 64-bit signed integer column directly. NULL columns return 0.
    /// </summary>
    public long GetInt64(int index)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        return raw.sqlite3_column_int64(Statement, index);
    }

    /// <summary>
    /// Reads a 16-bit signed integer column directly. A stored value outside the
    /// <see cref="short" /> range throws an <see cref="OverflowException" />.
    /// </summary>
    public short GetInt16(int index)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        return checked((short)raw.sqlite3_column_int64(Statement, index));
    }

    /// <summary>
    /// Reads a 16-bit unsigned integer column directly. A stored value outside the
    /// <see cref="ushort" /> range throws an <see cref="OverflowException" />.
    /// </summary>
    public ushort GetUInt16(int index)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        return checked((ushort)raw.sqlite3_column_int64(Statement, index));
    }

    /// <summary>
    /// Reads an 8-bit unsigned integer column directly. A stored value outside the
    /// <see cref="byte" /> range throws an <see cref="OverflowException" />.
    /// </summary>
    public byte GetByteValue(int index)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        return checked((byte)raw.sqlite3_column_int64(Statement, index));
    }

    /// <summary>
    /// Reads an 8-bit signed integer column directly. A stored value outside the
    /// <see cref="sbyte" /> range throws an <see cref="OverflowException" />.
    /// </summary>
    public sbyte GetSByteValue(int index)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        return checked((sbyte)raw.sqlite3_column_int64(Statement, index));
    }

    /// <summary>
    /// Reads a 32-bit unsigned integer column directly. The stored 64-bit value is
    /// reinterpreted, so a value outside the <see cref="uint" /> range wraps.
    /// </summary>
    public uint GetUInt32(int index)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        return unchecked((uint)raw.sqlite3_column_int(Statement, index));
    }

    /// <summary>
    /// Reads a 64-bit unsigned integer column directly. The stored signed value is
    /// reinterpreted, so a value at or above 2 to the power 63 reads back as itself.
    /// </summary>
    public ulong GetUInt64(int index)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        return unchecked((ulong)raw.sqlite3_column_int64(Statement, index));
    }

    /// <summary>
    /// Reads a double-precision floating-point column directly. NULL columns return 0.0.
    /// </summary>
    public double GetDouble(int index)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        return raw.sqlite3_column_double(Statement, index);
    }

    /// <summary>
    /// Reads a single-precision floating-point column directly. The stored 64-bit value
    /// is narrowed to 32 bits, so it can lose precision.
    /// </summary>
    public float GetSingle(int index)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        return (float)raw.sqlite3_column_double(Statement, index);
    }

    /// <summary>
    /// Reads a boolean column stored as INTEGER. Any non-zero value reads as <see langword="true" />.
    /// </summary>
    public bool GetBoolean(int index)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        return raw.sqlite3_column_int(Statement, index) != 0;
    }

    /// <summary>
    /// Reads a TEXT column directly without going through the generic
    /// <see cref="GetValue(int, SQLiteColumnType, Type)" /> path. NULL columns return
    /// <see langword="null" />.
    /// </summary>
    public string? GetString(int index)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        if (raw.sqlite3_column_type(Statement, index) == raw.SQLITE_NULL)
        {
            return null;
        }
        return raw.sqlite3_column_text(Statement, index).utf8_to_string();
    }

    /// <summary>
    /// Reads a BLOB column as a read-only span over SQLite's own buffer, without copying the
    /// bytes into a new array. The span is only valid while the reader stays on the current row,
    /// so consume it before the next <see cref="Read" /> or <see cref="Dispose" /> call. NULL
    /// and empty BLOB columns return an empty span.
    /// </summary>
    public ReadOnlySpan<byte> GetBlobSpan(int index)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        return raw.sqlite3_column_blob(Statement, index);
    }

    /// <summary>
    /// Reads a <see cref="DateTime" /> column without boxing. A NULL column returns the default value.
    /// Honors the database <see cref="SQLiteOptions.DateTimeStorage" /> mode, the same as the generic
    /// <see cref="GetValue(int, SQLiteColumnType, Type)" /> path.
    /// </summary>
    public DateTime GetDateTimeValue(int index)
    {
        SQLiteColumnType columnType = GetColumnType(index);
        return columnType == SQLiteColumnType.Null
            ? default
            : CommandHelpers.ReadDateTime(Statement, index, columnType, Options);
    }

    /// <summary>
    /// Reads a <see cref="DateTimeOffset" /> column without boxing. A NULL column returns the default value.
    /// </summary>
    public DateTimeOffset GetDateTimeOffsetValue(int index)
    {
        SQLiteColumnType columnType = GetColumnType(index);
        return columnType == SQLiteColumnType.Null
            ? default
            : CommandHelpers.ReadDateTimeOffset(Statement, index, columnType, Options);
    }

    /// <summary>
    /// Reads a <see cref="TimeSpan" /> column without boxing. A NULL column returns the default value.
    /// </summary>
    public TimeSpan GetTimeSpanValue(int index)
    {
        SQLiteColumnType columnType = GetColumnType(index);
        return columnType == SQLiteColumnType.Null
            ? default
            : CommandHelpers.ReadTimeSpan(Statement, index, columnType, Options);
    }

    /// <summary>
    /// Reads a <see cref="DateOnly" /> column without boxing. A NULL column returns the default value.
    /// </summary>
    public DateOnly GetDateOnlyValue(int index)
    {
        SQLiteColumnType columnType = GetColumnType(index);
        return columnType == SQLiteColumnType.Null
            ? default
            : CommandHelpers.ReadDateOnly(Statement, index, columnType, Options);
    }

    /// <summary>
    /// Reads a <see cref="TimeOnly" /> column without boxing. A NULL column returns the default value.
    /// </summary>
    public TimeOnly GetTimeOnlyValue(int index)
    {
        SQLiteColumnType columnType = GetColumnType(index);
        return columnType == SQLiteColumnType.Null
            ? default
            : CommandHelpers.ReadTimeOnly(Statement, index, columnType, Options);
    }

    /// <summary>
    /// Reads a <see cref="Guid" /> column without boxing. A NULL column returns the default value.
    /// </summary>
    public Guid GetGuidValue(int index)
    {
        SQLiteColumnType columnType = GetColumnType(index);
        return columnType == SQLiteColumnType.Null
            ? default
            : CommandHelpers.ReadGuid(Statement, index, columnType);
    }

    /// <summary>
    /// Reads a <see cref="decimal" /> column without boxing. A NULL column returns the default value.
    /// </summary>
    public decimal GetDecimalValue(int index)
    {
        SQLiteColumnType columnType = GetColumnType(index);
        return columnType == SQLiteColumnType.Null
            ? default
            : CommandHelpers.ReadDecimal(Statement, index, columnType, Options);
    }
}