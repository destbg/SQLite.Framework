namespace SQLite.Framework;

/// <summary>
/// A <see cref="Stream" /> over a single BLOB column in a single row. Backed by SQLite's
/// incremental BLOB I/O API (<c>sqlite3_blob_open</c>, <c>sqlite3_blob_read</c>,
/// <c>sqlite3_blob_write</c>). Use it to read or write BLOB columns without materializing the
/// whole payload as a <see cref="byte" />[] in memory. Get an instance with
/// <see cref="SQLiteDatabase.OpenBlobStream{T}(long, Expression{Func{T, byte[]}}, bool, string)" />
/// or one of the other <c>OpenBlobStream</c> overloads.
/// </summary>
/// <remarks>
/// The stream is fixed-length. The blob in the database must already exist at the size you
/// want to write. Pre-allocate by inserting or updating the row with a byte array of the
/// target size, or by executing raw SQL such as
/// <c>INSERT INTO Books (Cover) VALUES (zeroblob(1048576))</c>.
///
/// The stream holds a connection-level lock for its lifetime. Always dispose it (use
/// <c>using</c>) before issuing other write operations on the same database.
/// </remarks>
public sealed class SQLiteBlobStream : Stream
{
    private readonly SQLiteDatabase database;
    private readonly IDisposable connectionLock;
    private readonly bool writable;
    private readonly int length;

    private sqlite3_blob? handle;
    private long position;

    internal SQLiteBlobStream(SQLiteDatabase database, sqlite3_blob handle, bool writable, IDisposable connectionLock)
    {
        this.database = database;
        this.handle = handle;
        this.writable = writable;
        this.connectionLock = connectionLock;
        length = raw.sqlite3_blob_bytes(handle);
    }

    /// <inheritdoc />
    public override bool CanRead => handle != null;

    /// <inheritdoc />
    public override bool CanWrite => writable && handle != null;

    /// <inheritdoc />
    public override bool CanSeek => handle != null;

    /// <inheritdoc />
    public override long Length
    {
        get
        {
            ThrowIfDisposed();
            return length;
        }
    }

    /// <inheritdoc />
    public override long Position
    {
        get
        {
            ThrowIfDisposed();
            return position;
        }
        set
        {
            ThrowIfDisposed();
            ArgumentOutOfRangeException.ThrowIfNegative(value);
            ArgumentOutOfRangeException.ThrowIfGreaterThan(value, length);
            position = value;
        }
    }

    /// <summary>
    /// Reopens the underlying blob handle to point at a different row in the same column. The
    /// new row must have a blob of the same length as the current blob. The stream's
    /// <see cref="Position" /> resets to 0.
    /// </summary>
    public void Reopen(long rowid)
    {
        ThrowIfDisposed();
        ThrowOnError((SQLiteResult)raw.sqlite3_blob_reopen(handle!, rowid));
        position = 0;
    }

    /// <inheritdoc />
    public override int Read(byte[] buffer, int offset, int count)
    {
        ArgumentNullException.ThrowIfNull(buffer);
        ArgumentOutOfRangeException.ThrowIfNegative(offset);
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        if (offset + count > buffer.Length)
        {
            throw new ArgumentException("offset + count exceeds buffer length.", nameof(count));
        }
        return Read(buffer.AsSpan(offset, count));
    }

    /// <inheritdoc />
    public override int Read(Span<byte> buffer)
    {
        ThrowIfDisposed();
        int toRead = (int)Math.Min(buffer.Length, length - position);
        if (toRead <= 0)
        {
            return 0;
        }

        ThrowOnError((SQLiteResult)raw.sqlite3_blob_read(handle!, buffer[..toRead], (int)position));
        position += toRead;
        return toRead;
    }

    /// <inheritdoc />
    public override void Write(byte[] buffer, int offset, int count)
    {
        ArgumentNullException.ThrowIfNull(buffer);
        ArgumentOutOfRangeException.ThrowIfNegative(offset);
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        if (offset + count > buffer.Length)
        {
            throw new ArgumentException("offset + count exceeds buffer length.", nameof(count));
        }
        Write(buffer.AsSpan(offset, count));
    }

    /// <inheritdoc />
    public override void Write(ReadOnlySpan<byte> buffer)
    {
        ThrowIfDisposed();
        if (!writable)
        {
            throw new NotSupportedException("This blob stream was opened read-only. Open with writable: true to write.");
        }
        if (position + buffer.Length > length)
        {
            throw new InvalidOperationException(
                $"Write would exceed the blob size ({length} bytes). " +
                "SQLite incremental I/O cannot grow a blob. Pre-allocate the target size " +
                "with zeroblob() or by writing a sized byte[] through Add/Update first.");
        }

        ThrowOnError((SQLiteResult)raw.sqlite3_blob_write(handle!, buffer, (int)position));
        position += buffer.Length;
    }

    /// <inheritdoc />
    public override long Seek(long offset, SeekOrigin origin)
    {
        ThrowIfDisposed();
        long newPosition = origin switch
        {
            SeekOrigin.Begin => offset,
            SeekOrigin.Current => position + offset,
            SeekOrigin.End => length + offset,
            _ => throw new ArgumentOutOfRangeException(nameof(origin)),
        };
        if (newPosition < 0 || newPosition > length)
        {
            throw new IOException("Seek would move position outside the blob.");
        }
        position = newPosition;
        return position;
    }

    /// <inheritdoc />
    public override void Flush()
    {
    }

    /// <inheritdoc />
    public override void SetLength(long value)
    {
        throw new NotSupportedException(
            "SQLite blobs are fixed size. Use Add/Update with a sized byte[] or execute " +
            "raw SQL with zeroblob(n) to allocate the desired length first.");
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (handle != null)
        {
            raw.sqlite3_blob_close(handle);
            handle = null;
            connectionLock.Dispose();
        }
        base.Dispose(disposing);
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(handle == null, this);
    }

    private void ThrowOnError(SQLiteResult result)
    {
        if (result != SQLiteResult.OK)
        {
            throw new SQLiteException(result, raw.sqlite3_errmsg(database.Handle!).utf8_to_string(), null);
        }
    }
}
