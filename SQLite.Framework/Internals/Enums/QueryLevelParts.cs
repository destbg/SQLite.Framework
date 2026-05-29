namespace SQLite.Framework.Internals.Enums;

/// <summary>
/// The SQL parts that a single SELECT level can hold. Used to decide when a LINQ
/// method has to run on top of a subquery instead of folding into the current SELECT.
/// </summary>
[Flags]
internal enum QueryLevelParts
{
    None = 0,
    Where = 1 << 0,
    Projection = 1 << 1,
    GroupBy = 1 << 2,
    OrderBy = 1 << 3,
    Distinct = 1 << 4,
    Limit = 1 << 5,
    Join = 1 << 6,
    Reverse = 1 << 7,
}
