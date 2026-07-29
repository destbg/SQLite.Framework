namespace SQLite.Framework.Internals.Models;

/// <summary>
/// The translated body of a recursive common table expression, together with the column traits the
/// body turned out to have. The registry needs all of them to describe the CTE to the outer query.
/// </summary>
internal class RecursiveCteBody
{
    public required SQLTranslator Translator { get; init; }
    public required SQLQuery Query { get; init; }
    public string[]? ColumnNames { get; init; }
    public HashSet<string>? DayOfWeekColumns { get; init; }
    public HashSet<string>? JsonSourceColumns { get; init; }
    public bool HasClientMember { get; init; }
}
