namespace SQLite.Framework.Internals.Models;

/// <summary>
/// Describes the self reference of a recursive common table expression while its body is being
/// translated. <see cref="Columns"/> is the template a lambda parameter resolves through, and the
/// remaining members let every place that puts the self reference in a FROM clause build its columns
/// the same way the outer query builds them.
/// </summary>
internal class CteSelfReference
{
    public required string Placeholder { get; init; }
    public required Type ElementType { get; init; }
    public required Dictionary<string, Expression> Columns { get; init; }
    public string[]? ColumnNames { get; init; }
    public HashSet<string>? DayOfWeekColumns { get; init; }
    public HashSet<string>? ConstructedPaths { get; init; }
    public Dictionary<string, Expression>? BodyColumns { get; init; }
    public IReadOnlyList<SQLiteExpression>? BodySelects { get; init; }
}
