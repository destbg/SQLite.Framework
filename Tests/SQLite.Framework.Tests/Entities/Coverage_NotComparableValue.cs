namespace SQLite.Framework.Tests.Entities;

public sealed class Coverage_NotComparableValue
{
    public Coverage_NotComparableValue(int payload)
    {
        Payload = payload;
    }

    public int Payload { get; }
}
