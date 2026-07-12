namespace SQLite.Framework.Internals.Models;

/// <summary>
/// Pre-computed column binding for one <see cref="PropertySlot" /> on an entity type. Built
/// once per query by <see cref="BuildQueryObject" /> and reused for every row, so the row
/// loop never re-resolves column names or recurses through nested-entity decision logic.
/// </summary>
internal struct SlotPlan
{
    public PropertySlot Slot { get; set; }
    public int ColumnIndex { get; set; }
    public Func<SQLiteQueryContext, object?>? NestedMaterializer { get; set; }
    public Type? ProjectedReadType { get; set; }
}
