namespace SQLite.Framework.Enums;

/// <summary>
/// Coordinate storage for a SQLite R-Tree virtual table. SQLite ships two R-Tree modules: the
/// default <c>rtree</c> stores coordinates as 32-bit floats and the <c>rtree_i32</c> variant
/// stores them as 32-bit signed integers. The integer variant uses half the disk space and
/// supports exact comparisons but cannot represent fractional coordinates.
/// </summary>
public enum SQLiteRTreeStorage
{
    /// <summary>
    /// Default. Emits <c>USING rtree(...)</c>. Coordinates are stored as 32-bit floats.
    /// Property types <c>float</c>, <c>double</c>, and <c>int</c> all map here.
    /// </summary>
    Float,

    /// <summary>
    /// Emits <c>USING rtree_i32(...)</c>. Coordinates are stored as 32-bit signed integers.
    /// Only <c>int</c> property types are allowed.
    /// </summary>
    Int32,
}
