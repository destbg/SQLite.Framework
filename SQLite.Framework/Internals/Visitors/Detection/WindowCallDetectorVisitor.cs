namespace SQLite.Framework.Internals.Visitors.Detection;

internal sealed class WindowCallDetectorVisitor : ExpressionVisitor
{
    public bool Found { get; private set; }

    protected override Expression VisitLambda<T>(Expression<T> node)
    {
        return node;
    }

    protected override Expression VisitMethodCall(MethodCallExpression node)
    {
        Type? declaringType = node.Method.DeclaringType;
        if (declaringType == typeof(SQLiteWindowFunctions)
            || declaringType is { IsGenericType: true }
                && declaringType.GetGenericTypeDefinition() == typeof(SQLiteWindow<>))
        {
            Found = true;
            return node;
        }

        return base.VisitMethodCall(node);
    }
}
