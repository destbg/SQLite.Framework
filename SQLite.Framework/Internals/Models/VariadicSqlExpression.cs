namespace SQLite.Framework.Internals.Models;

/// <summary>
/// SQL expression with the shape <c>{before}{children[0]}{sep}{children[1]}{sep}...{after}</c>.
/// Use it for function calls that take any number of arguments, like <c>string.Concat</c>,
/// <c>IN</c> lists and <c>COALESCE</c> chains.
/// </summary>
internal sealed class VariadicSqlExpression : SQLiteExpression
{
    private readonly string before;
    private readonly SQLiteExpression[] children;
    private readonly string sep;
    private readonly string after;

    public VariadicSqlExpression(Type type, int identifier, string before, SQLiteExpression[] children, string sep, string after, SQLiteParameter[]? parameters)
        : base(type, identifier, parameters)
    {
        this.before = before;
        this.children = children;
        this.sep = sep;
        this.after = after;
    }

    public override void WriteSqlTo(StringBuilder sb)
    {
        sb.Append(before);
        for (int i = 0; i < children.Length; i++)
        {
            if (i > 0)
            {
                sb.Append(sep);
            }
            children[i].WriteSqlTo(sb);
        }
        sb.Append(after);
    }
}
