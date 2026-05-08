namespace SQLite.Framework.Internals.Models;

/// <summary>
/// SQL expression with the shape <c>{before}{a}{mid1}{b}{mid2}{c}{after}</c>. Use it for
/// function calls with three arguments and for ternary expressions.
/// </summary>
internal sealed class TrinarySqlExpression : SQLiteExpression
{
    private readonly string before;
    private readonly SQLiteExpression a;
    private readonly string mid1;
    private readonly SQLiteExpression b;
    private readonly string mid2;
    private readonly SQLiteExpression c;
    private readonly string after;

    public TrinarySqlExpression(Type type, int identifier, string before, SQLiteExpression a, string mid1, SQLiteExpression b, string mid2, SQLiteExpression c, string after, SQLiteParameter[]? parameters)
        : base(type, identifier, parameters)
    {
        this.before = before;
        this.a = a;
        this.mid1 = mid1;
        this.b = b;
        this.mid2 = mid2;
        this.c = c;
        this.after = after;
    }

    public override void WriteSqlTo(StringBuilder sb)
    {
        sb.Append(before);
        a.WriteSqlTo(sb);
        sb.Append(mid1);
        b.WriteSqlTo(sb);
        sb.Append(mid2);
        c.WriteSqlTo(sb);
        sb.Append(after);
    }
}
