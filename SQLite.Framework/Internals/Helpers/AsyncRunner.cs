namespace SQLite.Framework.Internals.Helpers;

/// <summary>
/// Wraps the <see cref="Task.Factory" /> calls used by the async extension methods so the same
/// background-thread shape (<see cref="TaskCreationOptions.DenyChildAttach" /> on
/// <see cref="TaskScheduler.Default" />) does not have to be repeated in every file.
/// </summary>
[ExcludeFromCodeCoverage]
internal static class AsyncRunner
{
    public static Task Run(Action action, CancellationToken ct)
    {
        return Task.Factory.StartNew(action, ct, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
    }

    public static Task<T> Run<T>(Func<T> func, CancellationToken ct)
    {
        return Task.Factory.StartNew(func, ct, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
    }

    public static Task<T> Run<T, TP>(Func<TP, T> func, TP parameter, CancellationToken ct)
    {
        return Task.Factory.StartNew(() => func(parameter), ct, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
    }

    public static Task<T> Run<T, TP1, TP2>(Func<TP1, TP2, T> func, TP1 parameter1, TP2 parameter2, CancellationToken ct)
    {
        return Task.Factory.StartNew(() => func(parameter1, parameter2), ct, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
    }

    public static Task<T> Run<T, TP1, TP2, TP3>(Func<TP1, TP2, TP3, T> func, TP1 parameter1, TP2 parameter2, TP3 parameter3, CancellationToken ct)
    {
        return Task.Factory.StartNew(() => func(parameter1, parameter2, parameter3), ct, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
    }
}
