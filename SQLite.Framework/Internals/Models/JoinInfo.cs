namespace SQLite.Framework.Internals.Models;

internal class JoinInfo
{
    public required Type EntityType { get; init; }
    public required string JoinType { get; set; }
    public required SQLExpression Sql { get; init; }
    public required SQLExpression OnClause { get; init; }
    public required bool IsGroupJoin { get; set; }
}