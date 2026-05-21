namespace SQLite.Framework.Enums;

/// <summary>
/// Materialization hint for a Common Table Expression. SQLite 3.35 added <c>MATERIALIZED</c>
/// and <c>NOT MATERIALIZED</c> hints that force or block CTE inlining. Without a hint, SQLite
/// picks the strategy itself based on the query shape.
/// </summary>
public enum SQLiteCteMaterialization
{
    /// <summary>
    /// Default. Emits no hint. SQLite chooses whether to materialize or inline the CTE.
    /// </summary>
    Default,

    /// <summary>
    /// Emits <c>MATERIALIZED</c>. SQLite computes the CTE once into a temporary table and reuses
    /// the result on every reference. Useful when the CTE body is expensive and referenced
    /// multiple times.
    /// </summary>
    Materialized,

    /// <summary>
    /// Emits <c>NOT MATERIALIZED</c>. SQLite always inlines the CTE body at every reference.
    /// Useful when the CTE is just a name for a subquery and the optimizer would benefit from
    /// seeing it in place.
    /// </summary>
    NotMaterialized,
}
