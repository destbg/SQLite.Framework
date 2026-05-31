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
        return QueryPlanFormatter.Format(this);
    }
}
