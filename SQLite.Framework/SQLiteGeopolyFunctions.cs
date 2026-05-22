namespace SQLite.Framework;

/// <summary>
/// Marker methods for SQLite Geopoly geospatial functions. These methods throw at runtime and
/// are only valid inside a LINQ query, where the framework swaps them for the right SQL.
/// Requires SQLite 3.27.0 or newer and a build that includes the Geopoly extension.
/// </summary>
/// <remarks>
/// The shape argument can be either GeoJSON text (a <see cref="string" />) or a binary polygon
/// blob (a <see cref="byte" />[] produced by <see cref="Blob" />).
/// </remarks>
#if SQLITE_FRAMEWORK_OS_BUNDLED_SQLITE
[UnsupportedOSPlatform("android")]
[SupportedOSPlatform("android30.0")]
[UnsupportedOSPlatform("ios")]
[SupportedOSPlatform("ios13.0")]
#endif
public static class SQLiteGeopolyFunctions
{
    private const string OutsideQuery = "SQLiteGeopolyFunctions methods can only be used inside a LINQ query.";

    /// <summary>
    /// Returns <see langword="true" /> when the two polygons overlap. Translates to SQLite's
    /// <c>geopoly_overlap(p1, p2)</c>.
    /// </summary>
    public static bool Overlap(object shape1, object shape2)
    {
        throw new InvalidOperationException(OutsideQuery);
    }

    /// <summary>
    /// Returns 2 when <paramref name="inner" /> is fully inside <paramref name="outer" />, 1 when
    /// they overlap but <paramref name="inner" /> is not fully inside, and 0 when they are
    /// disjoint. Translates to SQLite's <c>geopoly_within(p1, p2)</c>.
    /// </summary>
    public static int Within(object inner, object outer)
    {
        throw new InvalidOperationException(OutsideQuery);
    }

    /// <summary>
    /// Returns the area of the polygon. Translates to SQLite's <c>geopoly_area(p)</c>.
    /// </summary>
    public static double Area(object shape)
    {
        throw new InvalidOperationException(OutsideQuery);
    }

    /// <summary>
    /// Returns <see langword="true" /> when the polygon contains the point (<paramref name="x" />,
    /// <paramref name="y" />). Translates to SQLite's <c>geopoly_contains_point(p, x, y)</c>.
    /// </summary>
    public static bool ContainsPoint(object shape, double x, double y)
    {
        throw new InvalidOperationException(OutsideQuery);
    }

    /// <summary>
    /// Returns the polygon's axis-aligned bounding box as a 4-vertex polygon. Translates to
    /// SQLite's <c>geopoly_bbox(p)</c>.
    /// </summary>
    public static byte[] BoundingBox(object shape)
    {
        throw new InvalidOperationException(OutsideQuery);
    }

    /// <summary>
    /// Returns the polygon in compact binary form. Translates to SQLite's <c>geopoly_blob(p)</c>.
    /// </summary>
    public static byte[] Blob(object shape)
    {
        throw new InvalidOperationException(OutsideQuery);
    }

    /// <summary>
    /// Returns the polygon as GeoJSON text. Translates to SQLite's <c>geopoly_json(p)</c>.
    /// </summary>
    public static string Json(object shape)
    {
        throw new InvalidOperationException(OutsideQuery);
    }

    /// <summary>
    /// Returns the polygon as an SVG path snippet. Translates to SQLite's
    /// <c>geopoly_svg(p, attributes)</c>.
    /// </summary>
    public static string Svg(object shape, string attributes)
    {
        throw new InvalidOperationException(OutsideQuery);
    }

    /// <summary>
    /// Returns the polygon with its vertices reordered counter-clockwise. Translates to SQLite's
    /// <c>geopoly_ccw(p)</c>.
    /// </summary>
    public static byte[] CounterClockwise(object shape)
    {
        throw new InvalidOperationException(OutsideQuery);
    }

    /// <summary>
    /// Returns a regular n-gon centered at (<paramref name="cx" />, <paramref name="cy" />) with
    /// the given <paramref name="radius" /> and <paramref name="vertices" />. Translates to
    /// SQLite's <c>geopoly_regular(cx, cy, r, n)</c>.
    /// </summary>
    public static byte[] Regular(double cx, double cy, double radius, int vertices)
    {
        throw new InvalidOperationException(OutsideQuery);
    }

    /// <summary>
    /// Applies an affine 2D transform to every vertex of the polygon. Translates to SQLite's
    /// <c>geopoly_xform(p, A, B, C, D, E, F)</c>, which computes
    /// <c>x' = A*x + B*y + E</c> and <c>y' = C*x + D*y + F</c>.
    /// </summary>
    public static byte[] Transform(object shape, double a, double b, double c, double d, double e, double f)
    {
        throw new InvalidOperationException(OutsideQuery);
    }
}
