namespace SQLite.Framework.Internals.FTS5;

/// <summary>
/// One piece of a rendered FTS5 MATCH query. Either a literal text fragment of FTS5 syntax
/// (<see cref="LiteralText" /> set, <see cref="DynamicSql" /> null) or a runtime SQL expression
/// whose value is wrapped as <c>printf('"%w"', ...)</c> to produce a quoted FTS5 token.
/// </summary>
[ExcludeFromCodeCoverage]
internal sealed class FtsQueryPart
{
    public FtsQueryPart(string? literalText, SQLiteExpression? dynamicSql)
    {
        LiteralText = literalText;
        DynamicSql = dynamicSql;
    }

    public string? LiteralText { get; }
    public SQLiteExpression? DynamicSql { get; }
}
