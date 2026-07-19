namespace SQLite.Framework.Internals.Visitors;

/// <summary>
/// Rewrites a CTE body column expression so every SQL leaf it holds reads the matching column of the
/// registered CTE instead of the body's own table alias. Used when a CTE body projects a client-built
/// member, for example an inline array, whose backing columns the outer query reads through the CTE.
/// </summary>
internal sealed class CteClientColumnRewriter : SelectVisitor
{
    private readonly Dictionary<SQLiteExpression, string> outerNameBySelectInner;
    private readonly string alias;
    private readonly SQLiteCounters counters;

    public CteClientColumnRewriter(IReadOnlyList<SQLiteExpression> selects, string[]? columnNames, string alias, SQLiteCounters counters)
        : base([])
    {
        this.alias = alias;
        this.counters = counters;
        outerNameBySelectInner = new Dictionary<SQLiteExpression, string>();
        for (int i = 0; i < selects.Count; i++)
        {
            SQLiteExpression select = selects[i];
            SQLiteExpression inner = select is AliasSqlExpression aliasSelect ? aliasSelect.Inner : select;
            outerNameBySelectInner.TryAdd(inner, columnNames != null ? columnNames[i] : select.IdentifierText);
        }
    }

    public Expression Rewrite(Expression expression)
    {
        return Visit(expression);
    }

    public override Expression VisitSQLExpression(SQLiteExpression node)
    {
        if (outerNameBySelectInner.TryGetValue(node, out string? outerName))
        {
            SQLiteExpression leaf = SQLiteExpression.Leaf(node.Type, counters.NextIdentifier(), $"{alias}.{IdentifierGuard.Quote(outerName)}");
            if (node.IsDayOfWeekInteger)
            {
                leaf.WithDayOfWeekInteger();
            }
            if (node.IsJsonSource)
            {
                leaf.WithJsonSource();
            }
            return leaf;
        }

        return node;
    }
}
