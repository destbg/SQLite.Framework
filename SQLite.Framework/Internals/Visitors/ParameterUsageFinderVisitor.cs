namespace SQLite.Framework.Internals.Visitors;

/// <summary>
/// Walks an expression tree and reports whether it references a given lambda parameter. Used to
/// tell a constant or function value (safe on an insert) apart from one that reads a column of the
/// row, which SQLite cannot do while the row is being inserted.
/// </summary>
internal sealed class ParameterUsageFinderVisitor : ExpressionVisitor
{
    private readonly ParameterExpression parameter;

    public ParameterUsageFinderVisitor(ParameterExpression parameter)
    {
        this.parameter = parameter;
    }

    public bool Found { get; private set; }

    protected override Expression VisitParameter(ParameterExpression node)
    {
        if (node == parameter)
        {
            Found = true;
        }

        return node;
    }
}
