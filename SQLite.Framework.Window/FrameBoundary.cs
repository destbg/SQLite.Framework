using System.Diagnostics.CodeAnalysis;

namespace SQLite.Framework.Window;

/// <summary>
/// Represents a boundary in a window frame. Use the static factory methods to create boundaries.
/// These methods throw at runtime and are only valid inside a LINQ query where they are translated
/// to their SQL equivalents. Register translations by calling
/// <see cref="SQLiteOptionsBuilderWindowExtensions.AddWindow" /> on your <see cref="SQLiteOptions" />.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class FrameBoundary
{
    private FrameBoundary() { }

    /// <summary>
    /// The start of the partition. Translates to <c>UNBOUNDED PRECEDING</c>.
    /// </summary>
    public static FrameBoundary UnboundedPreceding()
    {
        throw new InvalidOperationException("FrameBoundary methods can only be used inside a LINQ query.");
    }

    /// <summary>
    /// The current row in the window. Translates to <c>CURRENT ROW</c>.
    /// </summary>
    public static FrameBoundary CurrentRow()
    {
        throw new InvalidOperationException("FrameBoundary methods can only be used inside a LINQ query.");
    }

    /// <summary>
    /// The end of the partition. Translates to <c>UNBOUNDED FOLLOWING</c>.
    /// </summary>
    public static FrameBoundary UnboundedFollowing()
    {
        throw new InvalidOperationException("FrameBoundary methods can only be used inside a LINQ query.");
    }

    /// <summary>
    /// A row that is <paramref name="n" /> rows before the current row. Translates to <c>n PRECEDING</c>.
    /// </summary>
    public static FrameBoundary Preceding(long n)
    {
        throw new InvalidOperationException("FrameBoundary methods can only be used inside a LINQ query.");
    }

    /// <summary>
    /// A row that is <paramref name="n" /> rows after the current row. Translates to <c>n FOLLOWING</c>.
    /// </summary>
    public static FrameBoundary Following(long n)
    {
        throw new InvalidOperationException("FrameBoundary methods can only be used inside a LINQ query.");
    }
}
