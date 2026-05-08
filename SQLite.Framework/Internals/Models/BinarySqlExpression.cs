namespace SQLite.Framework.Internals.Models;

/// <summary>
/// SQL expression with the shape <c>{before}{a}{mid}{b}{after}</c>. Use it for binary operators
/// (with or without parentheses around them) and for function calls with two arguments.
/// </summary>
internal sealed class BinarySqlExpression : SQLiteExpression
{
    private readonly string before;
    private readonly SQLiteExpression a;
    private readonly string mid;
    private readonly SQLiteExpression b;
    private readonly string after;

    public BinarySqlExpression(Type type, int identifier, string before, SQLiteExpression a, string mid, SQLiteExpression b, string after, SQLiteParameter[]? parameters)
        : base(type, identifier, parameters)
    {
        this.before = before;
        this.a = a;
        this.mid = mid;
        this.b = b;
        this.after = after;
    }

    public override void WriteSqlTo(StringBuilder sb)
    {
        sb.Append(before);
        a.WriteSqlTo(sb);
        sb.Append(mid);
        b.WriteSqlTo(sb);
        sb.Append(after);
    }
}
