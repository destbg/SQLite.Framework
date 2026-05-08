namespace SQLite.Framework.Internals.Models;

/// <summary>
/// SQL expression that hands <see cref="SQLiteExpression.WriteSqlTo"/> off to an inner expression
/// without changing the SQL output. Use this when you need a different <c>Type</c>,
/// <c>Identifier</c>, or <c>Parameters</c> on the outer wrapper, but the SQL should stay the same.
/// </summary>
internal sealed class AliasSqlExpression : SQLiteExpression
{
    private readonly SQLiteExpression inner;

    public AliasSqlExpression(Type type, int identifier, SQLiteExpression inner, SQLiteParameter[]? parameters)
        : base(type, identifier, parameters)
    {
        this.inner = inner;
    }

    public override void WriteSqlTo(StringBuilder sb)
    {
        inner.WriteSqlTo(sb);
    }
}
