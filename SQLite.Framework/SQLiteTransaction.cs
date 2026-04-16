using SQLitePCL;

namespace SQLite.Framework;

/// <summary>
/// Represents a transaction in SQLite.
/// </summary>
public class SQLiteTransaction : IDisposable, IAsyncDisposable
{
    private readonly sqlite3? ownedHandle;
    private readonly bool ownsLock;
    private bool completed;
    private bool disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="SQLiteTransaction"/> class.
    /// </summary>
    public SQLiteTransaction(SQLiteDatabase database, string savepointName, bool ownsLock)
    {
        Database = database;
        SavepointName = savepointName;
        this.ownsLock = ownsLock;
    }

    internal SQLiteTransaction(SQLiteDatabase database, sqlite3 ownedHandle)
    {
        Database = database;
        SavepointName = string.Empty;
        this.ownedHandle = ownedHandle;
    }

    /// <summary>
    /// The SQLite database.
    /// </summary>
    public SQLiteDatabase Database { get; }

    /// <summary>
    /// The name of the savepoint.
    /// </summary>
    public string SavepointName { get; }

    /// <summary>
    /// Commits the transaction.
    /// </summary>
    public void Commit()
    {
        if (completed)
        {
            throw new InvalidOperationException("The transaction has already been committed or rolled back.");
        }

        completed = true;
        disposed = true;

        if (ownedHandle != null)
        {
            Database.CommitOwnedConnection(ownedHandle);
            return;
        }

        Database.CreateCommand($"RELEASE {SavepointName}", []).ExecuteNonQuery();

        if (ownsLock)
        {
            Database.ReleaseLock();
        }
    }

    /// <summary>
    /// Commits the transaction.
    /// </summary>
    public Task CommitAsync(CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        Commit();
        return Task.CompletedTask;
    }

    /// <summary>
    /// Rolls back the transaction.
    /// </summary>
    public void Rollback()
    {
        if (completed)
        {
            throw new InvalidOperationException("The transaction has already been committed or rolled back.");
        }

        completed = true;
        disposed = true;

        if (ownedHandle != null)
        {
            Database.RollbackOwnedConnection(ownedHandle);
            return;
        }

        Database.CreateCommand($"ROLLBACK TO {SavepointName}", []).ExecuteNonQuery();

        if (ownsLock)
        {
            Database.ReleaseLock();
        }
    }

    /// <summary>
    /// Rolls back the transaction.
    /// </summary>
    public Task RollbackAsync(CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        Rollback();
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        completed = true;
        disposed = true;

        if (ownedHandle != null)
        {
            Database.RollbackOwnedConnection(ownedHandle);
            return;
        }

        Database.CreateCommand($"ROLLBACK TO {SavepointName}", []).ExecuteNonQuery();

        if (ownsLock)
        {
            Database.ReleaseLock();
        }
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync()
    {
        Dispose();
        return ValueTask.CompletedTask;
    }
}
