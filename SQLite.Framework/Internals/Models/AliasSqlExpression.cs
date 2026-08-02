namespace SQLite.Framework.Internals.Models;

/// <summary>
/// SQL expression that hands <see cref="SQLiteExpression.WriteSqlTo"/> off to an inner expression
/// without changing the SQL output.
/// </summary>
internal sealed class AliasSqlExpression : SQLiteExpression
{
    public AliasSqlExpression(Type type, int identifier, SQLiteExpression inner, SQLiteParameter[]? parameters)
        : base(type, identifier, parameters)
    {
        Inner = inner;
    }

    public SQLiteExpression Inner { get; }

    public override void WriteSqlTo(StringBuilder sb)
    {
        Inner.WriteSqlTo(sb);
    }
}
