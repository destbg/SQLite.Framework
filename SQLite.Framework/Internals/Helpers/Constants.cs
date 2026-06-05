namespace SQLite.Framework.Internals.Helpers;

internal static class Constants
{
    /// <summary>
    /// The number of ticks between the .NET epoch (1/1/0001) and the Unix epoch (1/1/1970).
    /// <code>
    /// new DateTime(1970, 1, 1).Ticks
    /// </code>
    /// </summary>
    public const long TicksToEpoch = 621355968000000000;

    /// <summary>
    /// Prefix used to stash the source row columns of a composite-key GroupBy in the grouping map.
    /// </summary>
    public const string GroupingElementPrefix = "$elem.";

    /// <summary>
    /// SQLite <c>CHAR(...)</c> list of the Unicode whitespace code points that .NET treats as whitespace.
    /// Passed as the second argument to TRIM, LTRIM and RTRIM so the result matches .NET Trim and the
    /// whitespace checks.
    /// </summary>
    public const string WhitespaceChars = "CHAR(9, 10, 11, 12, 13, 32, 133, 160, 5760, 8192, 8193, 8194, 8195, 8196, 8197, 8198, 8199, 8200, 8201, 8202, 8232, 8233, 8239, 8287, 12288)";
}
