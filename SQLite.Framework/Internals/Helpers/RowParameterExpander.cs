namespace SQLite.Framework.Internals.Helpers;

/// <summary>
/// Expands table-row references passed as method-call arguments into member-init expressions.
/// </summary>
internal static class RowParameterExpander
{
    [UnconditionalSuppressMessage("AOT", "IL3050", Justification = "The returned LambdaExpression is consumed as an expression tree by the translator, never compiled into a delegate.")]
    public static LambdaExpression ExpandRowsInMethodCalls(LambdaExpression lambda, IEnumerable<ParameterExpression> rowParameters)
    {
        HashSet<ParameterExpression> set = [.. rowParameters];

        if (set.Count == 0)
        {
            return lambda;
        }

        RowParameterExpanderVisitor expander = new(set);
        Expression body = expander.Visit(lambda.Body);
        return body == lambda.Body ? lambda : Expression.Lambda(body, lambda.Parameters);
    }
}
