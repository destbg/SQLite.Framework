using System.Runtime.CompilerServices;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class H21lConnectHookDatabase : TestDatabase
{
    private int connectingCalls;
    private int connectedCalls;

    public H21lConnectHookDatabase([CallerMemberName] string? methodName = null)
        : base(methodName)
    {
    }

    public int ConnectingCalls => connectingCalls;

    public int ConnectedCalls => connectedCalls;

    protected override void OnDatabaseConnecting()
    {
        connectingCalls++;
        if (connectingCalls == 1)
        {
            Execute("PRAGMA cache_size = -2048");
        }
    }

    protected override void OnDatabaseConnected()
    {
        connectedCalls++;
        Execute("CREATE TABLE IF NOT EXISTS \"H21lConnectMarks\" (\"Seq\" INTEGER)");
        Execute("INSERT INTO \"H21lConnectMarks\" (\"Seq\") VALUES (1)");
    }
}

public class H21lQuietConnectHookDatabase : TestDatabase
{
    private int connectingCalls;
    private int connectedCalls;

    public H21lQuietConnectHookDatabase([CallerMemberName] string? methodName = null)
        : base(methodName)
    {
    }

    public int ConnectingCalls => connectingCalls;

    public int ConnectedCalls => connectedCalls;

    protected override void OnDatabaseConnecting()
    {
        connectingCalls++;
    }

    protected override void OnDatabaseConnected()
    {
        connectedCalls++;
        Execute("CREATE TABLE IF NOT EXISTS \"H21lQuietMarks\" (\"Seq\" INTEGER)");
        Execute("INSERT INTO \"H21lQuietMarks\" (\"Seq\") VALUES (1)");
    }
}

public class ConnectionLifecycleHookTests
{
    [Fact]
    public void ConnectingHookRunningSqlFiresOncePerOpen()
    {
        using H21lConnectHookDatabase db = new();

        db.OpenConnection();

        Assert.Equal(1, db.ConnectingCalls);
    }

    [Fact]
    public void ConnectedHookFiresOncePerOpenWhenConnectingHookRunsSql()
    {
        using H21lConnectHookDatabase db = new();

        db.OpenConnection();

        Assert.Equal(1, db.ConnectedCalls);
    }

    [Fact]
    public void ConnectedHookWriteAppliesOncePerOpen()
    {
        using H21lConnectHookDatabase db = new();

        db.OpenConnection();

        Assert.Equal(1, db.ExecuteScalar<int>("SELECT COUNT(*) FROM \"H21lConnectMarks\""));
    }

    [Fact]
    public void HooksWithoutNestedSqlFireOncePerOpen()
    {
        using H21lQuietConnectHookDatabase db = new();

        db.OpenConnection();

        Assert.Equal(1, db.ConnectingCalls);
        Assert.Equal(1, db.ConnectedCalls);
        Assert.Equal(1, db.ExecuteScalar<int>("SELECT COUNT(*) FROM \"H21lQuietMarks\""));
    }
}
