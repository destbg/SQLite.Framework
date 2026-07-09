namespace SQLite.Framework;

/// <summary>
/// Represents a transaction in SQLite.
/// </summary>
public class SQLiteTransaction : IDisposable, IAsyncDisposable
{
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

    /// <summary>
    /// The SQLite database.
    /// </summary>
    public SQLiteDatabase Database { get; }

    /// <summary>
    /// The name of the savepoint.
    /// </summary>
    public string SavepointName { get; }

    /// <summary>
    /// The lock acquisition this transaction owns, when it took the connection lock itself.
    /// </summary>
    internal LockToken? OwnedLockToken { get; init; }

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

        try
        {
            Database.CreateCommand($"RELEASE {SavepointName}", []).ExecuteNonQuery();
        }
        catch (SQLiteException ex) when (ex.Message.StartsWith("no such savepoint", StringComparison.Ordinal))
        {
            // An enclosing rollback destroyed the savepoint
        }
        catch
        {
            Database.CreateCommand($"ROLLBACK TO {SavepointName}", []).ExecuteNonQuery();
            Database.CreateCommand($"RELEASE {SavepointName}", []).ExecuteNonQuery();
            throw;
        }
        finally
        {
            if (ownsLock)
            {
                Database.ReleaseLock(OwnedLockToken!);
            }

            Database.NotifyTransactionEnded();
        }
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

        try
        {
            Database.CreateCommand($"ROLLBACK TO {SavepointName}", []).ExecuteNonQuery();
            Database.CreateCommand($"RELEASE {SavepointName}", []).ExecuteNonQuery();
        }
        catch (SQLiteException ex) when (ex.Message.StartsWith("no such savepoint", StringComparison.Ordinal))
        {
            // An enclosing rollback destroyed the savepoint
        }
        finally
        {
            if (ownsLock)
            {
                Database.ReleaseLock(OwnedLockToken!);
            }

            Database.NotifyTransactionEnded();
        }
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

        try
        {
            Database.CreateCommand($"ROLLBACK TO {SavepointName}", []).ExecuteNonQuery();
            Database.CreateCommand($"RELEASE {SavepointName}", []).ExecuteNonQuery();
        }
        catch (SQLiteException ex) when (ex.Message.StartsWith("no such savepoint", StringComparison.Ordinal))
        {
            // An enclosing rollback already destroyed the savepoint.
        }
        finally
        {
            if (ownsLock)
            {
                Database.ReleaseLock(OwnedLockToken!);
            }

            Database.NotifyTransactionEnded();
        }
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync()
    {
        Dispose();
        return ValueTask.CompletedTask;
    }
}
