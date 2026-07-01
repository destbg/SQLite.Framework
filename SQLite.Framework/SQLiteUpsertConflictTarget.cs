namespace SQLite.Framework;

/// <summary>
/// Picks what happens when an INSERT conflicts on the chosen target column or columns.
/// Returned by <see cref="SQLiteUpsertBuilder{T}.OnConflict{TKey}" />.
/// </summary>
public sealed class SQLiteUpsertConflictTarget<T>
{
    private SQLiteUpsertAction<T>? action;

    internal SQLiteUpsertConflictTarget(IReadOnlyList<string> conflictColumns)
    {
        ConflictColumns = conflictColumns;
    }

    internal IReadOnlyList<string> ConflictColumns { get; }

    internal Expression<Func<T, bool>>? WherePredicate { get; private set; }

    internal SQLiteUpsertAction<T> ResolvedAction => action
        ?? throw new InvalidOperationException("Upsert configuration is missing a DoNothing(), DoUpdateAll() or DoUpdate(...) call.");

    /// <summary>
    /// Targets a partial unique index by adding a <c>WHERE</c> clause to the conflict target, as in
    /// <c>ON CONFLICT (col) WHERE pred</c>. The predicate must match the partial index's own
    /// <c>WHERE</c> clause. It is translated to SQL the same way <c>Where</c> clauses are.
    /// </summary>
    public SQLiteUpsertConflictTarget<T> Where(Expression<Func<T, bool>> predicate)
    {
        if (WherePredicate != null)
        {
            throw new InvalidOperationException("Where was already called for this OnConflict target.");
        }

        WherePredicate = predicate;
        return this;
    }

    /// <summary>
    /// <c>ON CONFLICT (...) DO NOTHING</c>. Keeps the existing row, drops the new one.
    /// </summary>
    public SQLiteUpsertAction<T> DoNothing()
    {
        return Set(UpsertActionFactory.DoNothing<T>());
    }

    /// <summary>
    /// <c>ON CONFLICT (...) DO UPDATE SET col = excluded.col</c> for every non-conflict column.
    /// </summary>
    public SQLiteUpsertAction<T> DoUpdateAll()
    {
        return Set(UpsertActionFactory.DoUpdateAll<T>());
    }

    /// <summary>
    /// <c>ON CONFLICT (...) DO UPDATE SET col = excluded.col</c> for the listed columns only.
    /// </summary>
    public SQLiteUpsertAction<T> DoUpdate(params Expression<Func<T, object?>>[] columns)
    {
        if (columns.Length == 0)
        {
            throw new ArgumentException("DoUpdate requires at least one column. Use DoUpdateAll for every column or DoNothing to keep the existing row.", nameof(columns));
        }

        IReadOnlyList<string> names = UpsertExpressionParser.ResolveColumnList(columns);
        return Set(UpsertActionFactory.DoUpdate<T>(names));
    }

    /// <summary>
    /// Each <c>Set</c> call on the builder assigns one column to an expression that can read
    /// the existing row and the incoming <c>excluded</c> row, as in
    /// <c>DoUpdate(s =&gt; s.Set(b =&gt; b.Count, (current, excluded) =&gt; current.Count + excluded.Count))</c>.
    /// </summary>
    public SQLiteUpsertAction<T> DoUpdate(Action<SQLiteUpsertSetBuilder<T>> configure)
    {
        SQLiteUpsertSetBuilder<T> builder = new();
        configure(builder);

        if (builder.Setters.Count == 0)
        {
            throw new ArgumentException("DoUpdate requires at least one Set(...) call. Use DoUpdateAll for every column or DoNothing to keep the existing row.", nameof(configure));
        }

        return Set(UpsertActionFactory.DoUpdateSet<T>(builder.Setters));
    }

    private SQLiteUpsertAction<T> Set(SQLiteUpsertAction<T> next)
    {
        if (action != null)
        {
            throw new InvalidOperationException("Upsert action was already configured.");
        }

        action = next;
        return next;
    }
}
