namespace SQLite.Framework;

/// <summary>
/// Awaiter for <see cref="SQLiteBeginTransactionAwaitable" />.
/// </summary>
/// <remarks>
/// <see cref="GetResult" /> runs in the calling thread right after the <see langword="await" />.
/// Setting the connection lock flag in this step makes later <c>Lock()</c> calls in the same
/// flow do nothing.
/// </remarks>
public readonly struct SQLiteBeginTransactionAwaiter : ICriticalNotifyCompletion
{
    private readonly SQLiteDatabase database;
    private readonly Task<string> savepointTask;
    private readonly sqlite3? ownedHandle;
    private readonly bool ownsLock;

    internal SQLiteBeginTransactionAwaiter(SQLiteDatabase database, bool separateConnection, CancellationToken cancellationToken)
    {
        this.database = database;
        ownsLock = !database.HoldsConnectionLock;

        if (ownsLock && separateConnection)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ownedHandle = database.OpenTransactionConnection();
            savepointTask = Task.FromResult(string.Empty);
            return;
        }

        savepointTask = ownsLock
            ? database.AcquireConnectionAndCreateSavepoint(cancellationToken)
            : Task.FromResult(database.CreateSavepoint());
    }

    /// <summary>
    /// Indicates that the transaction has been started.
    /// </summary>
    public bool IsCompleted => savepointTask.IsCompleted;

    /// <inheritdoc />
    public void OnCompleted(Action continuation)
    {
        savepointTask.GetAwaiter().OnCompleted(continuation);
    }

    /// <inheritdoc />
    public void UnsafeOnCompleted(Action continuation)
    {
        savepointTask.GetAwaiter().UnsafeOnCompleted(continuation);
    }

    /// <summary>
    /// Returns the started <see cref="SQLiteTransaction" />.
    /// </summary>
    /// <remarks>
    /// Runs in the caller's execution context. Setting <c>holdsConnectionLock</c> here makes the flag
    /// visible to all code the caller runs after the <see langword="await" />, including lambdas passed to
    /// <c>Task.Factory.StartNew</c> that inherit the caller's <see cref="AsyncLocal{T}" /> snapshot.
    /// </remarks>
    public SQLiteTransaction GetResult()
    {
        if (ownedHandle != null)
        {
            // SetTransactionConnection must run here, in the caller's execution context, so that
            // the AsyncLocal mutations are visible to all subsequent continuations the caller runs.
            database.SetTransactionConnection(ownedHandle);
            return new SQLiteTransaction(database, ownedHandle);
        }

        string savepointName = savepointTask.GetAwaiter().GetResult();

        if (ownsLock)
        {
            database.SetConnectionLock();
        }

        return new SQLiteTransaction(database, savepointName, ownsLock);
    }
}