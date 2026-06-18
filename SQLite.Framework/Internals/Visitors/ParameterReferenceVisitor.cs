namespace SQLite.Framework.Internals.Visitors;

/// <summary>
/// Walks an expression tree and reports two facts about it. First, whether it references a given
/// lambda parameter. Second, whether it references any other parameter. Used to split one side of
/// an equality into a value that depends only on the local element and a key that reads a column
/// of the query row.
/// </summary>
internal sealed class ParameterReferenceVisitor : ExpressionVisitor
{
    private readonly ParameterExpression target;

    public ParameterReferenceVisitor(ParameterExpression target)
    {
        this.target = target;
    }

    public bool ReferencesTarget { get; private set; }

    public bool ReferencesOther { get; private set; }

    protected override Expression VisitParameter(ParameterExpression node)
    {
        if (node == target)
        {
            ReferencesTarget = true;
        }
        else
        {
            ReferencesOther = true;
        }

        return node;
    }
}
