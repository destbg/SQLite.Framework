namespace SQLite.Framework.Tests.Entities;

public sealed class NotComparableValueHolder
{
    public NotComparableValueHolder(int payload)
    {
        Payload = payload;
    }

    public int Payload { get; }
}
