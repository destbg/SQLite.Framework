namespace SQLite.Framework;

/// <summary>
/// Represents a boundary in a window frame. Use the static factory methods to create boundaries.
/// These methods throw at runtime and are only valid inside a LINQ query where they are translated
/// to their SQL equivalents.
/// </summary>
[ExcludeFromCodeCoverage]
#if SQLITE_FRAMEWORK_OS_BUNDLED_SQLITE
[UnsupportedOSPlatform("android")]
[SupportedOSPlatform("android30.0")]
[UnsupportedOSPlatform("ios")]
[SupportedOSPlatform("ios13.0")]
#endif
public sealed class SQLiteFrameBoundary
{
    private SQLiteFrameBoundary() { }

    /// <summary>
    /// The start of the partition. Translates to <c>UNBOUNDED PRECEDING</c>.
    /// </summary>
    public static SQLiteFrameBoundary UnboundedPreceding()
    {
        throw new InvalidOperationException("SQLiteFrameBoundary methods can only be used inside a LINQ query.");
    }

    /// <summary>
    /// The current row in the window. Translates to <c>CURRENT ROW</c>.
    /// </summary>
    public static SQLiteFrameBoundary CurrentRow()
    {
        throw new InvalidOperationException("SQLiteFrameBoundary methods can only be used inside a LINQ query.");
    }

    /// <summary>
    /// The end of the partition. Translates to <c>UNBOUNDED FOLLOWING</c>.
    /// </summary>
    public static SQLiteFrameBoundary UnboundedFollowing()
    {
        throw new InvalidOperationException("SQLiteFrameBoundary methods can only be used inside a LINQ query.");
    }

    /// <summary>
    /// A row that is <paramref name="n" /> rows before the current row. Translates to <c>n PRECEDING</c>.
    /// </summary>
    public static SQLiteFrameBoundary Preceding(long n)
    {
        throw new InvalidOperationException("SQLiteFrameBoundary methods can only be used inside a LINQ query.");
    }

    /// <summary>
    /// A row that is <paramref name="n" /> rows after the current row. Translates to <c>n FOLLOWING</c>.
    /// </summary>
    public static SQLiteFrameBoundary Following(long n)
    {
        throw new InvalidOperationException("SQLiteFrameBoundary methods can only be used inside a LINQ query.");
    }
}
