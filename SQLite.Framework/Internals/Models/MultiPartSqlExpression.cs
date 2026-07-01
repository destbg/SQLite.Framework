namespace SQLite.Framework.Internals.Models;

/// <summary>
/// SQL expression that mixes a list of string parts with child expressions. The shape is
/// <c>{parts[0]}{children[0]}{parts[1]}{children[1]}{parts[2]}...{parts[N]}</c> where
/// <c>parts.Length == children.Length + 1</c>. Use it for shapes with four or more child slots
/// that don't fit <see cref="WrapSqlExpression"/>, <see cref="BinarySqlExpression"/> or
/// <see cref="TrinarySqlExpression"/>.
/// </summary>
internal sealed class MultiPartSqlExpression : SQLiteExpression
{
    private readonly string[] parts;
    private readonly SQLiteExpression[] children;

    public MultiPartSqlExpression(Type type, int identifier, string[] parts, SQLiteExpression[] children, SQLiteParameter[]? parameters)
        : base(type, identifier, parameters)
    {
        this.parts = parts;
        this.children = children;
    }

    public override void WriteSqlTo(StringBuilder sb)
    {
        sb.Append(parts[0]);
        for (int i = 0; i < children.Length; i++)
        {
            children[i].WriteSqlTo(sb);
            sb.Append(parts[i + 1]);
        }
    }
}
