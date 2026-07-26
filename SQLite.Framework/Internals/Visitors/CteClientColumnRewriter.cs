namespace SQLite.Framework.Internals.Visitors;

/// <summary>
/// Rewrites a CTE body column expression so every SQL leaf it holds reads the matching column of the
/// registered CTE instead of the body's own table alias. Used when a CTE body projects a client-built
/// member, for example an inline array, whose backing columns the outer query reads through the CTE.
/// </summary>
internal sealed class CteClientColumnRewriter : SelectVisitor
{
    private readonly Dictionary<string, string> outerNameBySelectSql;
    private readonly string alias;
    private readonly SQLiteCounters counters;

    public CteClientColumnRewriter(IReadOnlyList<SQLiteExpression> selects, string[]? columnNames, string alias, SQLiteCounters counters)
        : base([])
    {
        this.alias = alias;
        this.counters = counters;
        outerNameBySelectSql = new Dictionary<string, string>(StringComparer.Ordinal);
        for (int i = 0; i < selects.Count; i++)
        {
            SQLiteExpression select = selects[i];
            SQLiteExpression inner = select is AliasSqlExpression aliasSelect ? aliasSelect.Inner : select;
            outerNameBySelectSql.TryAdd(CanonicalSql(inner), columnNames != null ? columnNames[i] : select.IdentifierText);
        }
    }

    public Expression Rewrite(Expression expression)
    {
        return Visit(expression);
    }

    public override Expression VisitSQLExpression(SQLiteExpression node)
    {
        if (outerNameBySelectSql.TryGetValue(CanonicalSql(node), out string? outerName))
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

    private static string CanonicalSql(SQLiteExpression node)
    {
        string sql = node.ToString();
        SQLiteParameter[]? parameters = node.Parameters;
        if (parameters == null || parameters.Length == 0)
        {
            return sql;
        }

        int[] byLongestName = Enumerable.Range(0, parameters.Length)
            .OrderByDescending(i => parameters[i].Name.Length)
            .ToArray();
        foreach (int i in byLongestName)
        {
            sql = sql.Replace(parameters[i].Name, $"?{i}", StringComparison.Ordinal);
        }

        StringBuilder builder = StringBuilderPool.Rent();
        builder.Append(sql);
        foreach (SQLiteParameter parameter in parameters)
        {
            builder.Append('');
            builder.Append(Convert.ToString(parameter.Value, CultureInfo.InvariantCulture));
        }

        return StringBuilderPool.ToStringAndReturn(builder);
    }
}
