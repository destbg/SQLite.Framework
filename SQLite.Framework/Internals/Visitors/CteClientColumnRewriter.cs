namespace SQLite.Framework.Internals.Visitors;

/// <summary>
/// Rewrites a CTE body column expression so every SQL leaf it holds reads the matching column of the
/// registered CTE instead of the body's own table alias. Used when a CTE body projects a client-built
/// member, for example an inline array, whose backing columns the outer query reads through the CTE.
/// </summary>
internal sealed class CteClientColumnRewriter : SelectVisitor
{
    private readonly Dictionary<string, string> outerNameBySelectSql;
    private readonly Dictionary<string, string> bodyIdentifierMap;
    private readonly string alias;
    private readonly SQLiteCounters counters;
    private bool seeding;

    public CteClientColumnRewriter(IReadOnlyList<SQLiteExpression> selects, string[]? columnNames, string alias, SQLiteCounters counters)
        : base([])
    {
        this.alias = alias;
        this.counters = counters;
        outerNameBySelectSql = new Dictionary<string, string>(StringComparer.Ordinal);
        bodyIdentifierMap = new Dictionary<string, string>(StringComparer.Ordinal);
        Dictionary<string, string> selectIdentifierMap = new(StringComparer.Ordinal);
        for (int i = 0; i < selects.Count; i++)
        {
            SQLiteExpression select = selects[i];
            SQLiteExpression inner = select is AliasSqlExpression aliasSelect ? aliasSelect.Inner : select;
            outerNameBySelectSql.TryAdd(CteSqlCanonicalizer.Canonicalize(inner, selectIdentifierMap), columnNames != null ? columnNames[i] : select.IdentifierText);
        }
    }

    public void Seed(Expression expression)
    {
        seeding = true;
        Visit(expression);
        seeding = false;
    }

    public Expression Rewrite(Expression expression)
    {
        return Visit(expression);
    }

    public override Expression VisitSQLExpression(SQLiteExpression node)
    {
        if (seeding)
        {
            CteSqlCanonicalizer.Canonicalize(node, bodyIdentifierMap);
            return node;
        }

        if (outerNameBySelectSql.TryGetValue(CteSqlCanonicalizer.Canonicalize(node, bodyIdentifierMap), out string? outerName))
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
