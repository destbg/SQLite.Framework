namespace SQLite.Framework.Models;

/// <summary>
/// Picks what happens when an INSERT conflicts on the chosen target column or columns.
/// Returned by <see cref="UpsertBuilder{T}.OnConflict{TKey}" />.
/// </summary>
public sealed class UpsertConflictTarget<T>
{
    private UpsertAction<T>? action;
    private Expression<Func<T, bool>>? wherePredicate;

    internal UpsertConflictTarget(IReadOnlyList<string> conflictColumns)
    {
        ConflictColumns = conflictColumns;
    }

    internal IReadOnlyList<string> ConflictColumns { get; }

    internal Expression<Func<T, bool>>? WherePredicate => wherePredicate;

    internal UpsertAction<T> ResolvedAction => action
        ?? throw new InvalidOperationException("Upsert configuration is missing a DoNothing(), DoUpdateAll(), or DoUpdate(...) call.");

    /// <summary>
    /// Targets a partial unique index by adding a <c>WHERE</c> clause to the conflict target, as in
    /// <c>ON CONFLICT (col) WHERE pred</c>. The predicate must match the partial index's own
    /// <c>WHERE</c> clause. It is translated to SQL the same way <c>Where</c> clauses are.
    /// </summary>
    public UpsertConflictTarget<T> Where(Expression<Func<T, bool>> predicate)
    {
        if (wherePredicate != null)
        {
            throw new InvalidOperationException("Where was already called for this OnConflict target.");
        }

        wherePredicate = predicate;
        return this;
    }

    /// <summary>
    /// <c>ON CONFLICT (...) DO NOTHING</c>. Keeps the existing row, drops the new one.
    /// </summary>
    public UpsertAction<T> DoNothing()
    {
        return Set(UpsertAction<T>.DoNothing());
    }

    /// <summary>
    /// <c>ON CONFLICT (...) DO UPDATE SET col = excluded.col</c> for every non-conflict column.
    /// </summary>
    public UpsertAction<T> DoUpdateAll()
    {
        return Set(UpsertAction<T>.DoUpdateAll());
    }

    /// <summary>
    /// <c>ON CONFLICT (...) DO UPDATE SET col = excluded.col</c> for the listed columns only.
    /// </summary>
    public UpsertAction<T> DoUpdate(params Expression<Func<T, object?>>[] columns)
    {
        if (columns.Length == 0)
        {
            throw new ArgumentException("DoUpdate requires at least one column. Use DoUpdateAll for every column or DoNothing to keep the existing row.", nameof(columns));
        }

        IReadOnlyList<string> names = UpsertExpressionParser.ResolveColumnList(columns);
        return Set(UpsertAction<T>.DoUpdate(names));
    }

    private UpsertAction<T> Set(UpsertAction<T> next)
    {
        if (action != null)
        {
            throw new InvalidOperationException("Upsert action was already configured.");
        }

        action = next;
        return next;
    }
}
