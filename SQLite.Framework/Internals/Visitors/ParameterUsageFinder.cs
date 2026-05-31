namespace SQLite.Framework.Internals.Visitors;

/// <summary>
/// Walks an expression tree and reports whether it references a given lambda parameter. Used to
/// tell a constant or function value (safe on an insert) apart from one that reads a column of the
/// row, which SQLite cannot do while the row is being inserted.
/// </summary>
internal sealed class ParameterUsageFinder : ExpressionVisitor
{
    private readonly ParameterExpression parameter;
    private bool found;

    private ParameterUsageFinder(ParameterExpression parameter)
    {
        this.parameter = parameter;
    }

    public static bool Uses(LambdaExpression lambda)
    {
        ParameterUsageFinder finder = new(lambda.Parameters[0]);
        finder.Visit(lambda.Body);
        return finder.found;
    }

    protected override Expression VisitParameter(ParameterExpression node)
    {
        if (node == parameter)
        {
            found = true;
        }

        return node;
    }
}
