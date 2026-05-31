namespace SQLite.Framework;

/// <summary>
/// Collects the column assignments for an <c>ON CONFLICT (...) DO UPDATE SET</c> built from expressions.
/// Passed to the lambda given to <see cref="SQLiteUpsertConflictTarget{T}.DoUpdate(Action{SQLiteUpsertSetBuilder{T}})" />.
/// Each <see cref="Set{TValue}(Expression{Func{T, TValue}}, Expression{Func{T, T, TValue}})" /> adds
/// one <c>col = expression</c> pair. The expression can read the existing row and the incoming
/// <c>excluded</c> row, which is the everyday shape for counters and merges.
/// </summary>
public sealed class SQLiteUpsertSetBuilder<T>
{
    private readonly List<(string Column, LambdaExpression Rhs)> setters = [];

    internal SQLiteUpsertSetBuilder()
    {
    }

    internal IReadOnlyList<(string Column, LambdaExpression Rhs)> Setters => setters;

    /// <summary>
    /// Assigns <paramref name="column" /> to an expression of both rows, as in
    /// <c>Set(b =&gt; b.Count, (current, excluded) =&gt; current.Count + excluded.Count)</c>. The
    /// first setter parameter is the row already stored in the table. The second is the incoming row
    /// that failed to insert, mapped to SQLite's <c>excluded</c> row.
    /// </summary>
    public SQLiteUpsertSetBuilder<T> Set<TValue>(Expression<Func<T, TValue>> column, Expression<Func<T, T, TValue>> setter)
    {
        return Add(column, setter);
    }

    /// <summary>
    /// Assigns <paramref name="column" /> to an expression of the existing row only, as in
    /// <c>Set(b =&gt; b.Version, current =&gt; current.Version + 1)</c>. The parameter is the row
    /// already stored in the table.
    /// </summary>
    public SQLiteUpsertSetBuilder<T> Set<TValue>(Expression<Func<T, TValue>> column, Expression<Func<T, TValue>> setter)
    {
        return Add(column, setter);
    }

    /// <summary>
    /// Assigns <paramref name="column" /> to a constant value, as in
    /// <c>Set(b =&gt; b.Status, "merged")</c>.
    /// </summary>
    public SQLiteUpsertSetBuilder<T> Set<TValue>(Expression<Func<T, TValue>> column, TValue value)
    {
        Expression<Func<T, TValue>> setter = _ => value;
        return Add(column, setter);
    }

    private SQLiteUpsertSetBuilder<T> Add<TValue>(Expression<Func<T, TValue>> column, LambdaExpression setter)
    {
        string name = UpsertExpressionParser.ResolveMemberName(column);
        setters.Add((name, setter));
        return this;
    }
}
