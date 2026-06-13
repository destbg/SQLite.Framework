namespace SQLite.Framework.Extensions;

/// <summary>
/// Async wrappers for <see cref="SQLiteCommand" />.
/// The non-streaming variants take the connection lock asynchronously on a thread-pool worker
/// and run the sync version inside it. <see cref="ExecuteReaderAsync" /> stays on the caller's
/// flow because the returned reader is iterated synchronously on that flow.
/// </summary>
public static class AsyncSQLiteCommandExtensions
{
    /// <summary>
    /// Async version of <see cref="SQLiteCommand.ExecuteReader" />.
    /// The returned reader holds the read lock until it is disposed.
    /// </summary>
    public static async Task<SQLiteDataReader> ExecuteReaderAsync(this SQLiteCommand command, CancellationToken cancellationToken = default)
    {
        IDisposable connectionLock = await command.Database.ReadLockAsync(cancellationToken);

        command.NotifyExecuting();
        SQLiteDataReader? reader = null;
        try
        {
            sqlite3_stmt statement = command.CreateStatement();
            reader = new(command.Database.GetActiveHandle(), statement, connectionLock, command.Database)
            {
                PooledSql = command.CommandText,
            };
            command.NotifyExecuted(rowsAffected: null);
            return reader;
        }
        catch (Exception exception)
        {
            if (reader != null)
            {
                reader.Dispose();
            }
            else
            {
                connectionLock.Dispose();
            }

            command.NotifyFailed(exception);
            throw;
        }
    }

    /// <summary>
    /// Async version of <see cref="SQLiteCommand.ExecuteNonQuery" />.
    /// </summary>
    public static Task<int> ExecuteNonQueryAsync(this SQLiteCommand command, CancellationToken cancellationToken = default)
    {
        return AsyncRunner.Run(async () =>
        {
            using IDisposable _ = await command.Database.LockAsync(cancellationToken);
            return command.ExecuteNonQueryCore();
        }, cancellationToken);
    }

    /// <summary>
    /// Async version of <see cref="SQLiteCommand.ExecuteWithLastRowId" />.
    /// </summary>
    public static Task<(int Changes, long RowId)> ExecuteWithLastRowIdAsync(this SQLiteCommand command, CancellationToken cancellationToken = default)
    {
        return AsyncRunner.Run(async () =>
        {
            using IDisposable _ = await command.Database.LockAsync(cancellationToken);
            return command.ExecuteWithLastRowIdCore();
        }, cancellationToken);
    }
}
