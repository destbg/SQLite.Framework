namespace SQLite.Framework.Internals.Models;

internal class JoinInfo
{
    public required string JoinType { get; set; }
    public required string TableName { get; init; }
    public required string Alias { get; init; }
    public required string OnClause { get; init; }
    public required bool IsGroupJoin { get; set; }
}