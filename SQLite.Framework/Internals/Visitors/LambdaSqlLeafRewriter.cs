namespace SQLite.Framework.Internals.Visitors;

/// <summary>
/// Replaces the SQL leaf reads inside a client evaluated lambda with the values of the current
/// row, so the lambda can run as plain code against an in memory collection.
/// </summary>
internal sealed class LambdaSqlLeafRewriter : ExpressionVisitor
{
    private readonly QueryCompilerVisitor owner;
    private readonly SQLiteQueryContext context;

    public LambdaSqlLeafRewriter(QueryCompilerVisitor owner, SQLiteQueryContext context)
    {
        this.owner = owner;
        this.context = context;
    }

    [return: NotNullIfNotNull(nameof(node))]
    public override Expression? Visit(Expression? node)
    {
        if (node is SQLiteExpression sqlNode)
        {
            CompiledExpression leaf = (CompiledExpression)owner.VisitSQLExpression(sqlNode);
            return Expression.Constant(leaf.Call(context), sqlNode.Type);
        }

        return base.Visit(node);
    }
}
