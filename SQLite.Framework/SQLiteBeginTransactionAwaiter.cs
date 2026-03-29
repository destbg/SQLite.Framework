using System.Runtime.CompilerServices;

namespace SQLite.Framework;

/// <summary>
/// Awaiter for <see cref="SQLiteBeginTransactionAwaitable" />.
/// </summary>
/// <remarks>
/// <see cref="GetResult" /> is invoked in the caller's execution context after the <see langword="await" />
/// completes. This is the only place where <see cref="AsyncLocal{T}" /> mutations are visible to the
/// caller's subsequent continuations, setting the connection-lock flag here means that every
/// <c>Lock()</c> call the caller makes afterward correctly short-circuits to a no-op.
/// </remarks>
public readonly struct SQLiteBeginTransactionAwaiter : ICriticalNotifyCompletion
{
    private readonly SQLiteDatabase database;
    private readonly Task<string> savepointTask;
    private readonly bool ownsLock;

    internal SQLiteBeginTransactionAwaiter(SQLiteDatabase database)
    {
        this.database = database;
        ownsLock = !database.HoldsConnectionLock;

        // When we already hold the lock, create the savepoint synchronously right now
        // (still in the caller's flow) and wrap in a completed task so no real await happens.
        // When we need to acquire the lock, kick off the async acquisition immediately.
        savepointTask = ownsLock
            ? database.AcquireConnectionAndCreateSavepoint()
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
        string savepointName = savepointTask.GetAwaiter().GetResult();

        if (ownsLock)
        {
            database.SetConnectionLock();
        }

        return new SQLiteTransaction(database, savepointName, ownsLock);
    }
}