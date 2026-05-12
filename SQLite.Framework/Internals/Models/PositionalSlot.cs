namespace SQLite.Framework.Internals.Models;

/// <summary>
/// Pre-computed column binding for one positional-constructor parameter. Built once per query
/// by <see cref="BuildQueryObject" /> so the row loop only does the column read and any enum
/// conversion, with no per-row reflection.
/// </summary>
internal struct PositionalSlot
{
    public int ColumnIndex;
    public Type DeclaredType;
    public Type TargetType;
    public bool IsEnum;
    public Type? EnumUnderlyingType;
}
