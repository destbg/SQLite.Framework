namespace SQLite.Framework.Extensions;

/// <summary>
/// Async wrappers for <see cref="SQLiteTransaction" />.
/// </summary>
public static class AsyncTransactionExtensions
{
    /// <summary>
    /// Commits the transaction.
    /// </summary>
    public static Task CommitAsync(this SQLiteTransaction transaction, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        transaction.Commit();
        return Task.CompletedTask;
    }

    /// <summary>
    /// Rolls back the transaction.
    /// </summary>
    public static Task RollbackAsync(this SQLiteTransaction transaction, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        transaction.Rollback();
        return Task.CompletedTask;
    }
}
