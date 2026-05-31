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
}