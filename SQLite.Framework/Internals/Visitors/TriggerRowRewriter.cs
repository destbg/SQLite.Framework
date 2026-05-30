namespace SQLite.Framework.Internals.Visitors;

/// <summary>
/// Rewrites member access on a trigger builder's <c>Old</c> and <c>New</c> rows into member access
/// on dedicated parameters. For example <c>builder.New.Price</c> becomes <c>newRow.Price</c>, which
/// the SQL visitor then resolves to <c>NEW."BookPrice"</c>. The builder rows are captured through a
/// closure, so they reach the expression tree as a member access whose declaring type is the trigger
/// builder.
/// </summary>
internal sealed class TriggerRowRewriter : ExpressionVisitor
{
    private readonly ParameterExpression oldRow;
    private readonly ParameterExpression newRow;
    private readonly Type builderType;

    public TriggerRowRewriter(ParameterExpression oldRow, ParameterExpression newRow, Type builderType)
    {
        this.oldRow = oldRow;
        this.newRow = newRow;
        this.builderType = builderType;
    }

    protected override Expression VisitMember(MemberExpression node)
    {
        if (node.Expression is MemberExpression rowAccess && rowAccess.Member.DeclaringType == builderType)
        {
            ParameterExpression row = rowAccess.Member.Name == nameof(SQLiteTriggerBuilder<>.Old) ? oldRow : newRow;
            return Expression.MakeMemberAccess(row, node.Member);
        }

        return base.VisitMember(node);
    }
}
