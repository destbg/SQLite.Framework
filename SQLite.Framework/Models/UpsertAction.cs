namespace SQLite.Framework.Models;

/// <summary>
/// Terminal node of <see cref="UpsertBuilder{T}" />. Represents one of <c>DO NOTHING</c>,
/// <c>DO UPDATE SET ...</c> with a fixed list of columns, or <c>DO UPDATE SET</c> for every
/// non-conflict column. An optional <see cref="Where(Expression{Func{T, T, bool}})" /> guard turns
/// it into a conditional <c>DO UPDATE ... WHERE</c>.
/// </summary>
public sealed class UpsertAction<T>
{
    private UpsertAction(UpsertActionKind kind, IReadOnlyList<string>? columns)
    {
        Kind = kind;
        Columns = columns;
    }

    internal UpsertActionKind Kind { get; }

    internal IReadOnlyList<string>? Columns { get; }

    internal LambdaExpression? UpdateWhere { get; private set; }

    /// <summary>
    /// Adds a <c>WHERE</c> guard to the <c>DO UPDATE</c> branch using the existing row only, as in
    /// <c>DO UPDATE SET ... WHERE pred</c>. The update is skipped when the guard is false. The new
    /// row is then dropped, just like <c>DO NOTHING</c>. The parameter is the row already stored in
    /// the table. The predicate is translated to SQL the same way <c>Where</c> clauses are.
    /// </summary>
    public UpsertAction<T> Where(Expression<Func<T, bool>> predicate)
    {
        return SetWhere(predicate);
    }

    /// <summary>
    /// Adds a <c>WHERE</c> guard to the <c>DO UPDATE</c> branch that can compare the existing row
    /// against the incoming row, as in <c>DO UPDATE SET ... WHERE excluded.x &gt; x</c>. The first
    /// parameter is the row already stored in the table. The second parameter is the incoming row
    /// that failed to insert, mapped to SQLite's <c>excluded</c> row. This is the shape for
    /// last-write-wins, for example
    /// <c>Where((current, excluded) =&gt; excluded.UpdatedAt &gt; current.UpdatedAt)</c>. The update
    /// is skipped when the guard is false. The predicate is translated to SQL the same way
    /// <c>Where</c> clauses are.
    /// </summary>
    public UpsertAction<T> Where(Expression<Func<T, T, bool>> predicate)
    {
        return SetWhere(predicate);
    }

    private UpsertAction<T> SetWhere(LambdaExpression predicate)
    {
        if (Kind == UpsertActionKind.DoNothing)
        {
            throw new InvalidOperationException("DO NOTHING has no WHERE clause. Use DoUpdate or DoUpdateAll to add a conditional guard.");
        }

        if (UpdateWhere != null)
        {
            throw new InvalidOperationException("Where was already called for this Upsert action.");
        }

        UpdateWhere = predicate;
        return this;
    }

    internal static UpsertAction<T> DoNothing()
    {
        return new UpsertAction<T>(UpsertActionKind.DoNothing, null);
    }

    internal static UpsertAction<T> DoUpdateAll()
    {
        return new UpsertAction<T>(UpsertActionKind.DoUpdateAll, null);
    }

    internal static UpsertAction<T> DoUpdate(IReadOnlyList<string> columns)
    {
        return new UpsertAction<T>(UpsertActionKind.DoUpdate, columns);
    }
}
