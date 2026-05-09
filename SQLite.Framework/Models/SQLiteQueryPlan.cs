namespace SQLite.Framework.Models;

/// <summary>
/// The result of <c>EXPLAIN QUERY PLAN</c> for a query, organised as a tree.
/// </summary>
public sealed class SQLiteQueryPlan
{
    /// <summary>
    /// The root nodes of the plan, ordered by id. Each may have its own
    /// <see cref="SQLiteQueryPlanNode.Children" />.
    /// </summary>
    public required IReadOnlyList<SQLiteQueryPlanNode> Roots { get; init; }

    /// <summary>
    /// Renders the plan as a plain ASCII tree, two spaces of indentation per level and a
    /// <c>&gt; </c> prefix on every line. Suitable for logging and copy-paste output.
    /// </summary>
    public override string ToString()
    {
        StringBuilder sb = new();
        sb.Append("QUERY PLAN");
        foreach (SQLiteQueryPlanNode root in Roots)
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
