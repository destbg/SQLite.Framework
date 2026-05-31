namespace SQLite.Framework.Internals.Helpers;

/// <summary>
/// Renders a <see cref="SQLiteQueryPlan" /> as a plain ASCII tree, two spaces of indentation per
/// level and a <c>&gt; </c> prefix on every line.
/// </summary>
internal static class QueryPlanFormatter
{
    public static string Format(SQLiteQueryPlan plan)
    {
        StringBuilder sb = new();
        sb.Append("QUERY PLAN");
        foreach (SQLiteQueryPlanNode root in plan.Roots)
        {
            AppendNode(sb, root, depth: 0);
        }
        return sb.ToString();
    }

    private static void AppendNode(StringBuilder sb, SQLiteQueryPlanNode node, int depth)
    {
        sb.Append(Environment.NewLine);
        for (int i = 0; i < depth; i++)
        {
            sb.Append("  ");
        }
        sb.Append("> ");
        sb.Append(node.Detail);

        foreach (SQLiteQueryPlanNode child in node.Children)
        {
            AppendNode(sb, child, depth + 1);
        }
    }
}
