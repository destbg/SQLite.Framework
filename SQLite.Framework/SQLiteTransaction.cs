namespace SQLite.Framework;

/// <summary>
/// Represents a transaction in SQLite.
/// </summary>
public class SQLiteTransaction : IDisposable, IAsyncDisposable
{
    private readonly bool ownsLock;
    private readonly long nativeRollbackCount;
    private bool consumedByOuterCommit;
    private bool cancelledByOuterRollback;
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
        nativeRollbackCount = database.NativeRollbackCount;
        database.RegisterSavepoint(this);
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
            Database.CreateCommand($"RELEASE {IdentifierGuard.Quote(SavepointName)}", []).ExecuteNonQuery();
            Database.CompleteSavepoint(this, committedByOuter: true);
        }
        catch (SQLiteException ex) when (ex.Message.StartsWith("no such savepoint", StringComparison.Ordinal))
        {
            Database.RemoveSavepoint(this);
            if (cancelledByOuterRollback)
            {
                throw new InvalidOperationException(
                    "The savepoint was already rolled back by an outer transaction, so there is nothing to commit. " +
                    "Complete inner transactions before rolling back the outer one.");
            }

            if (!consumedByOuterCommit && nativeRollbackCount != Database.NativeRollbackCount)
            {
                throw new InvalidOperationException(
                    "The transaction was already rolled back by SQLite. A conflict resolution of Rollback " +
                    "or an OR ROLLBACK statement aborted the whole transaction, so there is nothing to commit.");
            }
        }
        catch
        {
            Database.CompleteSavepoint(this, committedByOuter: false);
            Database.ForceSavepointRollback(SavepointName);
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
            Database.CreateCommand($"ROLLBACK TO {IdentifierGuard.Quote(SavepointName)}", []).ExecuteNonQuery();
            Database.CreateCommand($"RELEASE {IdentifierGuard.Quote(SavepointName)}", []).ExecuteNonQuery();
            Database.CompleteSavepoint(this, committedByOuter: false);
        }
        catch (SQLiteException ex) when (ex.Message.StartsWith("no such savepoint", StringComparison.Ordinal))
        {
            Database.RemoveSavepoint(this);
            if (consumedByOuterCommit)
            {
                throw new InvalidOperationException(
                    "The savepoint was already committed by an outer transaction, so the rollback did not happen. " +
                    "Complete inner transactions before committing the outer one.");
            }
        }
        catch
        {
            Database.CompleteSavepoint(this, committedByOuter: false);
            Database.ForceSavepointRollback(SavepointName);
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
            Database.CreateCommand($"ROLLBACK TO {IdentifierGuard.Quote(SavepointName)}", []).ExecuteNonQuery();
            Database.CreateCommand($"RELEASE {IdentifierGuard.Quote(SavepointName)}", []).ExecuteNonQuery();
            Database.CompleteSavepoint(this, committedByOuter: false);
        }
        catch (SQLiteException ex) when (ex.Message.StartsWith("no such savepoint", StringComparison.Ordinal))
        {
            Database.RemoveSavepoint(this);
        }
        catch
        {
            Database.CompleteSavepoint(this, committedByOuter: false);
            Database.ForceSavepointRollback(SavepointName);
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

    internal void MarkCompletedByOuter(bool committed)
    {
        if (committed)
        {
            consumedByOuterCommit = true;
        }
        else
        {
            cancelledByOuterRollback = true;
        }
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync()
    {
        Dispose();
        return ValueTask.CompletedTask;
    }
}
