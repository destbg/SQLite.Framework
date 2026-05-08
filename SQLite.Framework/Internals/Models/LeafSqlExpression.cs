namespace SQLite.Framework.Internals.Models;

/// <summary>
/// SQL expression that holds a single SQL string with no child expressions. Built through
/// <see cref="SQLiteExpression.Leaf(Type, int, string)" /> and its other overloads.
/// </summary>
internal sealed class LeafSqlExpression : SQLiteExpression
{
    private readonly string sql;

    public LeafSqlExpression(Type type, int identifier, string sql, SQLiteParameter[]? parameters)
        : base(type, identifier, parameters)
    {
        this.sql = sql;
    }

    public override void WriteSqlTo(StringBuilder sb)
    {
        sb.Append(sql);
    }
}
