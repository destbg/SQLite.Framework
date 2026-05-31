namespace SQLite.Framework;

/// <summary>
/// Collects the column assignments for an <c>UPDATE</c> or <c>INSERT</c> statement inside a
/// LINQ-typed trigger body. Each <see cref="Set{TValue}" /> pairs a target column with a value
/// expression. The value can read the target row and the trigger's <c>Old</c> and <c>New</c> rows.
/// </summary>
public sealed class SQLiteTriggerSetBuilder<T>
{
    private readonly List<(string Column, LambdaExpression Value)> setters = [];

    internal SQLiteTriggerSetBuilder()
    {
    }

    internal IReadOnlyList<(string Column, LambdaExpression Value)> Setters => setters;

    /// <summary>
    /// Assigns <paramref name="column" /> to <paramref name="value" />. The value expression can use
    /// the target row and, through the trigger builder, the <c>Old</c> and <c>New</c> rows, as in
    /// <c>Set(a =&gt; a.Count, a =&gt; a.Count + 1)</c> or <c>Set(a =&gt; a.BookId, _ =&gt; t.New.Id)</c>.
    /// </summary>
    public SQLiteTriggerSetBuilder<T> Set<TValue>(Expression<Func<T, TValue>> column, Expression<Func<T, TValue>> value)
    {
        ArgumentNullException.ThrowIfNull(column);
        ArgumentNullException.ThrowIfNull(value);

        setters.Add((UpsertExpressionParser.ResolveMemberName(column), value));
        return this;
    }
}
