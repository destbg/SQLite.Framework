namespace SQLite.Framework.Internals.Visitors;

/// <summary>
/// Rewrites a CTE body column expression so every SQL leaf it holds reads the matching column of the
/// registered CTE instead of the body's own table alias. Used when a CTE body projects a client-built
/// member, for example an inline array, whose backing columns the outer query reads through the CTE.
/// </summary>
internal sealed class CteClientColumnRewriter : SelectVisitor
{
    private readonly IReadOnlyList<SQLiteExpression> selects;
    private readonly string[]? columnNames;
    private readonly Dictionary<string, string> outerNameBySelectSql;
    private readonly Dictionary<string, string> bodyIdentifierMap;
    private readonly HashSet<string> bodyTokens;
    private readonly string alias;
    private readonly SQLiteCounters counters;
    private bool seeding;
    private bool prepared;

    public CteClientColumnRewriter(IReadOnlyList<SQLiteExpression> selects, string[]? columnNames, string alias, SQLiteCounters counters)
        : base([])
    {
        this.selects = selects;
        this.columnNames = columnNames;
        this.alias = alias;
        this.counters = counters;
        outerNameBySelectSql = new Dictionary<string, string>(StringComparer.Ordinal);
        bodyIdentifierMap = new Dictionary<string, string>(StringComparer.Ordinal);
        bodyTokens = new HashSet<string>(StringComparer.Ordinal);
    }

    public void Seed(Expression expression)
    {
        seeding = true;
        Visit(expression);
        seeding = false;
    }

    public Expression Rewrite(Expression expression)
    {
        EnsurePrepared();
        return Visit(expression);
    }

    public override Expression VisitSQLExpression(SQLiteExpression node)
    {
        if (seeding)
        {
            CteSqlCanonicalizer.CollectGeneratedIdentifiers(node.ToString(), bodyTokens);
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

    private void EnsurePrepared()
    {
        if (prepared)
        {
            return;
        }

        prepared = true;
        HashSet<string> selectTokens = new(StringComparer.Ordinal);
        foreach (SQLiteExpression select in selects)
        {
            SQLiteExpression inner = select is AliasSqlExpression aliasSelect ? aliasSelect.Inner : select;
            CteSqlCanonicalizer.CollectGeneratedIdentifiers(inner.ToString(), selectTokens);
        }

        Dictionary<string, string> selectIdentifierMap = new(StringComparer.Ordinal);
        foreach (string token in selectTokens)
        {
            if (bodyTokens.Contains(token))
            {
                selectIdentifierMap[token] = token;
                bodyIdentifierMap[token] = token;
            }
        }

        for (int i = 0; i < selects.Count; i++)
        {
            SQLiteExpression select = selects[i];
            SQLiteExpression inner = select is AliasSqlExpression aliasSelect ? aliasSelect.Inner : select;
            outerNameBySelectSql.TryAdd(CteSqlCanonicalizer.Canonicalize(inner, selectIdentifierMap), columnNames != null ? columnNames[i] : select.IdentifierText);
        }
    }
}
