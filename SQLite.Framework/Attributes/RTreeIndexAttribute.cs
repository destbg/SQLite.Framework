namespace SQLite.Framework.Attributes;

/// <summary>
/// Marks a class as an R-Tree virtual table. The class becomes queryable through
/// <c>db.Table&lt;T&gt;()</c> and supports the same range-query patterns as a normal table.
/// </summary>
/// <remarks>
/// The mapped class must have:
/// <list type="bullet">
/// <item>Exactly one integer primary key property (the R-Tree rowid).</item>
/// <item>One or more matching <see cref="RTreeMinAttribute" /> / <see cref="RTreeMaxAttribute" />
/// pairs grouped by dimension name. SQLite allows 1 to 5 dimensions.</item>
/// <item>Zero or more properties marked with <see cref="RTreeAuxiliaryAttribute" />, which are
/// stored alongside the bounding box and emitted with the SQLite <c>+</c> prefix.</item>
/// </list>
/// </remarks>
#if SQLITE_FRAMEWORK_OS_BUNDLED_SQLITE
[UnsupportedOSPlatform("ios")]
[SupportedOSPlatform("ios10.0")]
#endif
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public sealed class RTreeIndexAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RTreeIndexAttribute" /> class with the default
    /// <see cref="SQLiteRTreeStorage.Float" /> storage.
    /// </summary>
    public RTreeIndexAttribute()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RTreeIndexAttribute" /> class with the given
    /// storage type.
    /// </summary>
    public RTreeIndexAttribute(SQLiteRTreeStorage storage)
    {
        Storage = storage;
    }

    /// <summary>
    /// Whether to emit <c>USING rtree(...)</c> or <c>USING rtree_i32(...)</c>. Defaults to
    /// <see cref="SQLiteRTreeStorage.Float" />.
    /// </summary>
    public SQLiteRTreeStorage Storage { get; init; }
}
