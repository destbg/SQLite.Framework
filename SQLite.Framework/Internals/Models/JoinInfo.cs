using System.Diagnostics.CodeAnalysis;

namespace SQLite.Framework.Internals.Models;

/// <summary>
/// Represents information about a join operation in a SQL query.
/// </summary>
[ExcludeFromCodeCoverage]
internal class JoinInfo
{
    public required Type EntityType { get; init; }
    public required string JoinType { get; set; }
    public required SQLExpression Sql { get; init; }
    public required SQLExpression? OnClause { get; init; }
    public required bool IsGroupJoin { get; set; }
}