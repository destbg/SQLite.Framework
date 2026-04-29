namespace SQLite.Framework.Internals.Models;

/// <summary>
/// Represents a resolved expression by <see cref="SQLVisitor.ResolveExpression"/>
/// </summary>
[ExcludeFromCodeCoverage]
internal readonly struct ResolvedModel
{
    public required bool IsConstant { get; init; }
    public required object? Constant { get; init; }
    public required SQLiteExpression? SQLiteExpression { get; init; }
    public required Expression Expression { get; init; }

    [NotNullIfNotNull(nameof(SQLiteExpression))]
    public string? Sql => SQLiteExpression?.Sql;

    public SQLiteParameter[]? Parameters => SQLiteExpression?.Parameters;
}
