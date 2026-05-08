namespace SQLite.Framework.Internals.Models;

/// <summary>
/// SQL expression with the shape <c>{before}{child}{after}</c>. Use it for unary operators and
/// function calls with one argument. The dedicated class avoids the extra object that a lambda
/// would allocate.
/// </summary>
internal sealed class WrapSqlExpression : SQLiteExpression
{
    private readonly string before;
    private readonly SQLiteExpression child;
    private readonly string after;

    public WrapSqlExpression(Type type, int identifier, string before, SQLiteExpression child, string after, SQLiteParameter[]? parameters)
        : base(type, identifier, parameters)
    {
        this.before = before;
        this.child = child;
        this.after = after;
    }

    public override void WriteSqlTo(StringBuilder sb)
    {
        sb.Append(before);
        child.WriteSqlTo(sb);
        sb.Append(after);
    }
}
