using System.Linq.Expressions;
using SQLite.Framework.Internals.Models;

namespace SQLite.Framework.Internals.Visitors;

/// <summary>
/// Gathers all SQL expressions in the select clause.
/// </summary>
internal class SelectVisitor : ExpressionVisitor
{
    public SelectVisitor(List<SQLExpression> selects)
    {
        Selects = selects;
    }

    public List<SQLExpression> Selects { get; }

    public Expression VisitSQLExpression(SQLExpression node)
    {
        Selects.Add(node);
        return node;
    }
}