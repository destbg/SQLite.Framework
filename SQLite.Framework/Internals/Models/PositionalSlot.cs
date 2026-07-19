namespace SQLite.Framework.Internals.Models;

/// <summary>
/// Pre-computed column binding for one positional-constructor parameter. Built once per query
/// by <see cref="BuildQueryObject" /> so the row loop only does the column read and any enum
/// conversion, with no per-row reflection.
/// </summary>
internal struct PositionalSlot
{
    public int ColumnIndex { get; set; }
    public Type DeclaredType { get; set; }
    public Type TargetType { get; set; }
    public Type ReadType { get; set; }
    public bool IsEnum { get; set; }
    public Type? EnumUnderlyingType { get; set; }
    public Func<SQLiteQueryContext, object?>? NestedMaterializer { get; set; }
}
