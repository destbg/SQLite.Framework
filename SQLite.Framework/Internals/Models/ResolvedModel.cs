using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using SQLite.Framework.Internals.Visitors;

namespace SQLite.Framework.Internals.Models;

/// <summary>
/// Represents a resolved expression by <see cref="SQLVisitor.ResolveExpression"/>
/// </summary>
internal class ResolvedModel
{
    public required bool IsConstant { get; init; }
    public required object? Constant { get; init; }
    public required SQLExpression? SQLExpression { get; init; }
    public required Expression Expression { get; init; }

    [NotNullIfNotNull(nameof(SQLExpression))]
    public string? Sql => SQLExpression?.Sql;

    public SQLiteParameter[]? Parameters => SQLExpression?.Parameters;
}