namespace SQLite.Framework.Models;

/// <summary>
/// Builds a single <c>WHERE</c> predicate by composing smaller predicates with <c>AND</c>
/// and <c>OR</c>. Use it through <see cref="QueryableExtensions.WhereBuilder{T}" /> when you
/// want a mix of <c>AND</c> and <c>OR</c>, or when you need to add predicates in a loop.
/// </summary>
/// <remarks>
/// Calls compose left to right, with no operator precedence:
/// <c>f.And(a).Or(b).And(c)</c> produces <c>((a OR b) AND c)</c>. Use the group overloads
/// (<c>And</c> or <c>Or</c> with an <see cref="Action{T}" /> argument) to make the grouping
/// explicit.
/// </remarks>
public sealed class SQLiteWhereBuilder<T>
{
    private Expression<Func<T, bool>>? predicate;

    /// <summary>
    /// Combines the current predicate with <paramref name="next" /> using <c>AND</c>. When the
    /// builder is empty, <paramref name="next" /> becomes the predicate.
    /// </summary>
    public SQLiteWhereBuilder<T> And(Expression<Func<T, bool>> next)
    {
        ArgumentNullException.ThrowIfNull(next);
        predicate = predicate == null ? next : Combine(predicate, next, isAnd: true);
        return this;
    }

    /// <summary>
    /// Combines the current predicate with <paramref name="next" /> using <c>OR</c>. When the
    /// builder is empty, <paramref name="next" /> becomes the predicate.
    /// </summary>
    public SQLiteWhereBuilder<T> Or(Expression<Func<T, bool>> next)
    {
        ArgumentNullException.ThrowIfNull(next);
        predicate = predicate == null ? next : Combine(predicate, next, isAnd: false);
        return this;
    }

    /// <summary>
    /// Builds a sub-group with its own builder, then combines that group's predicate with the
    /// current predicate using <c>AND</c>. An empty group is a no-op.
    /// </summary>
    public SQLiteWhereBuilder<T> And(Action<SQLiteWhereBuilder<T>> group)
    {
        ArgumentNullException.ThrowIfNull(group);
        Expression<Func<T, bool>>? inner = BuildGroup(group);
        if (inner != null)
        {
            predicate = predicate == null ? inner : Combine(predicate, inner, isAnd: true);
        }
        return this;
    }

    /// <summary>
    /// Builds a sub-group with its own builder, then combines that group's predicate with the
    /// current predicate using <c>OR</c>. An empty group is a no-op.
    /// </summary>
    public SQLiteWhereBuilder<T> Or(Action<SQLiteWhereBuilder<T>> group)
    {
        ArgumentNullException.ThrowIfNull(group);
        Expression<Func<T, bool>>? inner = BuildGroup(group);
        if (inner != null)
        {
            predicate = predicate == null ? inner : Combine(predicate, inner, isAnd: false);
        }
        return this;
    }

    internal Expression<Func<T, bool>>? Build()
    {
        return predicate;
    }

    private static Expression<Func<T, bool>>? BuildGroup(Action<SQLiteWhereBuilder<T>> group)
    {
        SQLiteWhereBuilder<T> inner = new();
        group(inner);
        return inner.predicate;
    }

    private static Expression<Func<T, bool>> Combine(Expression<Func<T, bool>> left, Expression<Func<T, bool>> right, bool isAnd)
    {
        ParameterExpression parameter = left.Parameters[0];
        Expression rightBody = new ParameterSubstitutor(right.Parameters[0], parameter).Visit(right.Body)!;
        Expression body = isAnd
            ? Expression.AndAlso(left.Body, rightBody)
            : Expression.OrElse(left.Body, rightBody);
        return Expression.Lambda<Func<T, bool>>(body, parameter);
    }
}
