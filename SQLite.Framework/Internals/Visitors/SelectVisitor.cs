namespace SQLite.Framework.Internals.Visitors;

/// <summary>
/// Gathers all SQL expressions in the select clause.
/// </summary>
internal class SelectVisitor : ExpressionVisitor
{
    public SelectVisitor(List<SQLiteExpression> selects)
    {
        Selects = selects;
    }

    public List<SQLiteExpression> Selects { get; }

    public Expression VisitSQLExpression(SQLiteExpression node)
    {
        Selects.Add(node);
        return node;
    }
}