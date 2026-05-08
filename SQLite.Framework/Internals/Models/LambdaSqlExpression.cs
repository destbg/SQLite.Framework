namespace SQLite.Framework.Internals.Models;

/// <summary>
/// SQL expression that writes its SQL through an <see cref="Action{StringBuilder}"/>. Use this for
/// shapes that don't fit <see cref="WrapSqlExpression"/>, <see cref="BinarySqlExpression"/>,
/// <see cref="TrinarySqlExpression"/>, or <see cref="VariadicSqlExpression"/>. The lambda captures
/// its child expressions, so it allocates an extra object. Use the other classes when one of them
/// fits the shape.
/// </summary>
public sealed class LambdaSqlExpression : SQLiteExpression
{
    private readonly Action<StringBuilder> writer;

    /// <summary>
    /// Initializes a new instance of the <see cref="LambdaSqlExpression"/> class.
    /// </summary>
    public LambdaSqlExpression(Type type, int identifier, Action<StringBuilder> writer, SQLiteParameter[]? parameters)
        : base(type, identifier, parameters)
    {
        this.writer = writer;
    }

    public override void WriteSqlTo(StringBuilder sb)
    {
        writer(sb);
    }
}
