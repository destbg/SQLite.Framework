namespace SQLite.Framework;

/// <summary>
/// Hook that observes every <see cref="SQLiteCommand" /> the framework runs against the database.
/// Register an instance through <see cref="SQLiteOptionsBuilder.AddCommandInterceptor" />.
/// The interceptor is called for the synchronous and asynchronous execution paths once per command.
/// </summary>
public interface ISQLiteCommandInterceptor
{
    /// <summary>
    /// Called right before the command runs.
    /// </summary>
    void OnExecuting(SQLiteCommand command);

    /// <summary>
    /// Called after the command runs without throwing.
    /// </summary>
    /// <param name="command">The command that ran.</param>
    /// <param name="rowsAffected">
    /// The number of rows affected for write paths. <see langword="null" /> for the reader
    /// path (where rows are fetched lazily) and for scalar reads.
    /// </param>
    void OnExecuted(SQLiteCommand command, int? rowsAffected);

    /// <summary>
    /// Called when the command throws. The exception is rethrown after every
    /// interceptor has been notified.
    /// </summary>
    void OnFailed(SQLiteCommand command, Exception exception);
}
