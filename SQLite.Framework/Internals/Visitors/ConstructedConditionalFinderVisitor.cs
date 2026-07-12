namespace SQLite.Framework.Internals.Visitors;

/// <summary>
/// Walks an expression tree and reports whether it contains a conditional that builds an object
/// in one branch and yields null in the other, or a null comparison against an inline built
/// object. A chained Select produces these shapes for an optional nested projection member, and
/// the column layout must then follow the flattened body.
/// </summary>
internal sealed class ConstructedConditionalFinderVisitor : ExpressionVisitor
{
    public bool Found { get; private set; }

    protected override Expression VisitConditional(ConditionalExpression node)
    {
        if ((node.IfTrue is NewExpression or MemberInitExpression && node.IfFalse is ConstantExpression { Value: null })
            || (node.IfFalse is NewExpression or MemberInitExpression && node.IfTrue is ConstantExpression { Value: null }))
        {
            Found = true;
            return node;
        }

        return base.VisitConditional(node);
    }

    protected override Expression VisitBinary(BinaryExpression node)
    {
        if (node.NodeType is ExpressionType.Equal or ExpressionType.NotEqual
            && (IsNullConstant(node.Left) && IsConstructedOperand(node.Right)
                || IsNullConstant(node.Right) && IsConstructedOperand(node.Left)))
        {
            Found = true;
            return node;
        }

        return base.VisitBinary(node);
    }

    private static bool IsNullConstant(Expression node)
    {
        return node is ConstantExpression { Value: null };
    }

    private static bool IsConstructedOperand(Expression node)
    {
        return node is NewExpression or MemberInitExpression
            || node is MemberExpression { Expression: NewExpression or MemberInitExpression or ConditionalExpression };
    }
}
