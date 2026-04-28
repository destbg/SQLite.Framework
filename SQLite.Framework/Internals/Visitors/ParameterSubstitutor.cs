namespace SQLite.Framework.Internals.Visitors;

internal sealed class ParameterSubstitutor : ExpressionVisitor
{
    private readonly ParameterExpression target;
    private readonly Expression replacement;

    public ParameterSubstitutor(ParameterExpression target, Expression replacement)
    {
        this.target = target;
        this.replacement = replacement;
    }

    protected override Expression VisitParameter(ParameterExpression node)
    {
        return node == target ? replacement : base.VisitParameter(node);
    }
}
