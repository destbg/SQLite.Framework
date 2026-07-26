using System.Runtime.CompilerServices;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class H22aRejectingConnectHookDatabase : TestDatabase
{
    public H22aRejectingConnectHookDatabase([CallerMemberName] string? methodName = null)
        : base(methodName)
    {
    }

    protected override void OnDatabaseConnecting()
    {
        throw new InvalidOperationException("The connecting hook rejected the open.");
    }
}

public class H22aRejectingConnectHookFileDatabase : TestDatabase
{
    public H22aRejectingConnectHookFileDatabase([CallerMemberName] string? methodName = null)
        : base(true, methodName)
    {
    }

    protected override void OnDatabaseConnecting()
    {
        throw new InvalidOperationException("The connecting hook rejected the open.");
    }
}

public class ConnectingHookFailureConnectionStateTests
{
    [Fact]
    public void AConnectingHookThatThrowsLeavesNoOpenHandle()
    {
        using H22aRejectingConnectHookDatabase db = new();

        Assert.Throws<InvalidOperationException>(() => db.OpenConnection());

        Assert.False(db.IsConnected);
        Assert.Null(db.Handle);
    }

    [Fact]
    public void DisposeAfterAConnectingHookThrewClosesTheConnection()
    {
        H22aRejectingConnectHookDatabase db = new();
        Assert.Throws<InvalidOperationException>(() => db.OpenConnection());

        db.Dispose();

        Assert.Null(db.Handle);
    }

    [Fact]
    public void AConnectingHookThatThrowsOnAFileDatabaseLeavesNoOpenHandle()
    {
        using H22aRejectingConnectHookFileDatabase db = new();

        Assert.Throws<InvalidOperationException>(() => db.OpenConnection());

        Assert.Null(db.Handle);
    }
}
