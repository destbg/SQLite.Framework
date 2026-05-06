namespace SQLite.Framework.Internals.Helpers;

/// <summary>
/// Wraps the <see cref="Task.Factory" /> calls used by the async extension methods so the same
/// background-thread shape (<see cref="TaskCreationOptions.DenyChildAttach" /> on
/// <see cref="TaskScheduler.Default" />) does not have to be repeated in every file.
/// The overloads accept a <see cref="Func{Task}" /> so the caller can take the connection
/// lock asynchronously inside the worker thread, keeping the lock and the sync work on the
/// same execution context.
/// </summary>
internal static class AsyncRunner
{
    public static Task Run(Func<Task> func, CancellationToken ct)
    {
        return Task.Factory.StartNew(func, ct, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default).Unwrap();
    }

    public static Task<T> Run<T>(Func<Task<T>> func, CancellationToken ct)
    {
        return Task.Factory.StartNew(func, ct, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default).Unwrap();
    }
}
