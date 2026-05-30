namespace SQLite.Framework.Enums;

/// <summary>
/// Where null values sort inside an <c>ORDER BY</c> key. Passed to the ordering overloads that add
/// a <c>NULLS FIRST</c> or <c>NULLS LAST</c> clause. The clause requires SQLite 3.30.0 or newer.
/// </summary>
public enum SQLiteNullsOrder
{
    /// <summary>
    /// Emits no <c>NULLS</c> clause. SQLite then uses its default, where nulls sort first under
    /// <c>ASC</c> and last under <c>DESC</c>.
    /// </summary>
    Default,

    /// <summary>
    /// Emits <c>NULLS FIRST</c> so null values sort before non-null values.
    /// </summary>
    First,

    /// <summary>
    /// Emits <c>NULLS LAST</c> so null values sort after non-null values.
    /// </summary>
    Last,
}
