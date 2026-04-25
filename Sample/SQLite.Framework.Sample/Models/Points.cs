namespace SQLite.Framework.Sample.Models;

/// <summary>
/// A custom value type with operator + defined but without implementing IAdditionOperators.
/// Used to test whether QueryCompilerVisitor falls back correctly for binary expressions
/// where node.Method is set by the compiler.
/// </summary>
public readonly struct Points
{
    public int Value { get; }

    public Points(int value)
    {
        Value = value;
    }

    public static Points operator +(Points a, Points b) => new(a.Value + b.Value);

    public override string ToString() => Value.ToString();
}
