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

    /// <summary>
    /// Called once for each row read through a <see cref="SQLiteDataReader" />, so the data a query
    /// returns can be observed. Correlate it to the command through <see cref="SQLiteCommand.Id" />.
    /// Read column values from <paramref name="reader" /> with its get methods. Do not call
    /// <see cref="SQLiteDataReader.Read" /> or <see cref="SQLiteDataReader.Dispose" /> here, since
    /// that advances or closes the stream the caller is reading. Only the reader path raises this,
    /// not scalar or non-query execution.
    /// </summary>
    void OnRowRead(SQLiteCommand command, SQLiteDataReader reader);

    /// <summary>
    /// Called when a <see cref="SQLiteDataReader" /> is disposed, after the caller has read from it.
    /// <see cref="OnExecuted" /> fires when the reader is ready, before any rows are read, so it
    /// cannot report how long reading took or how many rows came back. This fires at the end and
    /// carries <paramref name="readCount" />, the number of rows the caller actually read.
    /// </summary>
    void OnReaderClosing(SQLiteCommand command, SQLiteDataReader reader, int readCount);
}
