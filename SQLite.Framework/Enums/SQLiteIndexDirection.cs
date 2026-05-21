namespace SQLite.Framework.Enums;

/// <summary>
/// Sort direction for a column inside a <c>CREATE INDEX</c> declaration. SQLite stores the
/// index in this order, which lets the query planner read it forward for matching
/// <c>ORDER BY</c> clauses without a separate sort step.
/// </summary>
public enum SQLiteIndexDirection
{
    /// <summary>
    /// Default. Emits no clause. SQLite treats this as ascending. Use this when you do not
    /// want the framework to write a direction token at all.
    /// </summary>
    Inherit,

    /// <summary>
    /// Emits <c>ASC</c> after the column or expression.
    /// </summary>
    Ascending,

    /// <summary>
    /// Emits <c>DESC</c> after the column or expression.
    /// </summary>
    Descending,
}
