namespace SQLite.Framework.Internals.Helpers;

/// <summary>
/// Builds the terminal <see cref="SQLiteUpsertAction{T}" /> nodes for the upsert builder.
/// </summary>
internal static class UpsertActionFactory
{
    public static SQLiteUpsertAction<T> DoNothing<T>()
    {
        return new SQLiteUpsertAction<T>(UpsertActionKind.DoNothing, null);
    }

    public static SQLiteUpsertAction<T> DoUpdateAll<T>()
    {
        return new SQLiteUpsertAction<T>(UpsertActionKind.DoUpdateAll, null);
    }

    public static SQLiteUpsertAction<T> DoUpdate<T>(IReadOnlyList<string> columns)
    {
        return new SQLiteUpsertAction<T>(UpsertActionKind.DoUpdate, columns);
    }

    public static SQLiteUpsertAction<T> DoUpdateSet<T>(IReadOnlyList<(string Column, LambdaExpression Rhs)> setters)
    {
        return new SQLiteUpsertAction<T>(UpsertActionKind.DoUpdateSet, null) { Setters = setters };
    }
}
