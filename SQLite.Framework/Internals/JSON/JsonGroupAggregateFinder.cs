namespace SQLite.Framework.Internals.JSON;

/// <summary>
/// Walks a lambda body over a JSON grouping element and reports whether it calls an aggregate
/// over the grouping parameter. Used after a paged grouping window is materialized, where the
/// wrapper rows no longer carry the group elements an aggregate would need.
/// </summary>
internal sealed class JsonGroupAggregateFinder : ExpressionVisitor
{
    private readonly ParameterExpression grouping;

    public JsonGroupAggregateFinder(ParameterExpression grouping)
    {
        this.grouping = grouping;
    }

    public bool Found { get; private set; }

    protected override Expression VisitMethodCall(MethodCallExpression node)
    {
        if (!Found
            && node.Method.DeclaringType == typeof(Enumerable)
            && node.Arguments.Count > 0
            && node.Arguments[0] == grouping
            && node.Method.Name != nameof(Enumerable.Contains))
        {
            Found = true;
            return node;
        }

        return base.VisitMethodCall(node);
    }
}
