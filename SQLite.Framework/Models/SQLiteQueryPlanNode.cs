namespace SQLite.Framework.Models;

/// <summary>
/// One node in a <see cref="SQLiteQueryPlan" />. Mirrors a row from <c>EXPLAIN QUERY PLAN</c>.
/// </summary>
public sealed class SQLiteQueryPlanNode
{
    /// <summary>
    /// The id of this node. Unique inside one plan.
    /// </summary>
    public required int Id { get; init; }

    /// <summary>
    /// The id of the parent node, or <c>0</c> for a root node.
    /// </summary>
    public required int ParentId { get; init; }

    /// <summary>
    /// The raw description SQLite returned for this step, for example
    /// <c>SCAN Books AS b0</c> or <c>USE TEMP B-TREE FOR ORDER BY</c>.
    /// </summary>
    public required string Detail { get; init; }

    /// <summary>
    /// The child nodes, ordered by id.
    /// </summary>
    public required IReadOnlyList<SQLiteQueryPlanNode> Children { get; init; }
}
