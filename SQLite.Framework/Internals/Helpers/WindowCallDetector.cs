namespace SQLite.Framework.Internals.Helpers;

internal sealed class WindowCallDetector : ExpressionVisitor
{
    private bool found;
    protected override Expression VisitMethodCall(MethodCallExpression node)
    {
        Type? declaringType = node.Method.DeclaringType;
        if (declaringType == typeof(SQLiteWindowFunctions)
            || (declaringType is { IsGenericType: true } && declaringType.GetGenericTypeDefinition() == typeof(SQLiteWindow<>)))
        {
            found = true;
        }

        return base.VisitMethodCall(node);
    }

    public static bool Contains(Expression body)
    {
        WindowCallDetector detector = new();
        detector.Visit(body);
        return detector.found;
    }
}
