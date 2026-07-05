namespace SQLite.Framework.Internals.Helpers;

/// <summary>
/// Runs a setter callback against a recording <see cref="SQLitePropertyCalls{T}"/> instance to
/// collect the value lambdas it declares, without translating anything.
/// </summary>
internal static class SetterRecorder
{
    /// <summary>
    /// Returns the value lambdas the setter callback declares. The statement pre-scans them for
    /// raw SQL parameter names and filter opt-out markers before the source translates.
    /// </summary>
    public static IReadOnlyList<LambdaExpression> RecordSetterBodies<T>(Func<SQLitePropertyCalls<T>, SQLitePropertyCalls<T>> setters)
    {
        SQLitePropertyCalls<T> recorder = new();
        setters(recorder);
        return recorder.RecordedSetters!;
    }
}
