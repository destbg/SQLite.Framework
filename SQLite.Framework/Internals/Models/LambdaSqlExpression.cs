namespace SQLite.Framework.Internals.Models;

/// <summary>
/// SQL expression that writes its SQL through an <see cref="Action{StringBuilder}"/>.
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

    /// <inheritdoc />
    public override void WriteSqlTo(StringBuilder sb)
    {
        writer(sb);
    }
}
