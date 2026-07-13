namespace SQLite.Framework.Internals.Visitors;

/// <summary>
/// Walks an expression tree and reports whether it contains a method call or a delegate
/// invocation. A call inside an inline built object can keep parts of the projection in memory,
/// so a chained read of that object cannot rely on a generated select materializer.
/// </summary>
internal sealed class CallFinderVisitor : ExpressionVisitor
{
    public bool Found { get; private set; }

    protected override Expression VisitMethodCall(MethodCallExpression node)
    {
        Found = true;
        return node;
    }

    protected override Expression VisitInvocation(InvocationExpression node)
    {
        Found = true;
        return node;
    }
}
