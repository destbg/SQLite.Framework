using System.Runtime.CompilerServices;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class H23rRejectingConnectedHookDatabase : TestDatabase
{
    public H23rRejectingConnectedHookDatabase([CallerMemberName] string? methodName = null)
        : base(methodName)
    {
    }

    protected override void OnDatabaseConnected()
    {
        throw new InvalidOperationException("The connected hook rejected the open.");
    }
}

public class H23rOneTimeFailingConnectedHookDatabase : TestDatabase
{
    private int calls;

    public H23rOneTimeFailingConnectedHookDatabase([CallerMemberName] string? methodName = null)
        : base(methodName)
    {
    }

    public int CompletedCalls { get; private set; }

    protected override void OnDatabaseConnected()
    {
        calls++;
        if (calls == 1)
        {
            throw new InvalidOperationException("The connected hook rejected the first open.");
        }

        Execute("CREATE TABLE IF NOT EXISTS \"H23rConnectedMarks\" (\"Seq\" INTEGER)");
        CompletedCalls++;
    }
}

public class ConnectedHookFailureConnectionStateTests
{
    [Fact]
    public void AConnectedHookThatThrowsLeavesTheDatabaseNotConnected()
    {
        using H23rRejectingConnectedHookDatabase db = new();

        Assert.Throws<InvalidOperationException>(() => db.OpenConnection());

        Assert.False(db.IsConnected);
    }

    [Fact]
    public void ASecondOpenAfterAConnectedHookThrewRunsTheHookAgain()
    {
        using H23rOneTimeFailingConnectedHookDatabase db = new();
        Assert.Throws<InvalidOperationException>(() => db.OpenConnection());

        db.OpenConnection();

        Assert.Equal(1, db.CompletedCalls);
    }
}
