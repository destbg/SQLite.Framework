using System.Runtime.CompilerServices;
using SQLite.Framework.Exceptions;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class H22aPragmaWatchingHookDatabase : TestDatabase
{
    private int foreignKeysWhenConnected = -1;

    public H22aPragmaWatchingHookDatabase([CallerMemberName] string? methodName = null)
        : base(methodName)
    {
    }

    public int ForeignKeysWhenConnected => foreignKeysWhenConnected;

    protected override void OnDatabaseConnected()
    {
        foreignKeysWhenConnected = ExecuteScalar<int>("PRAGMA foreign_keys");
    }
}

public class H22aForeignKeyHookDatabase : TestDatabase
{
    private bool hookInsertSucceeded;

    public H22aForeignKeyHookDatabase([CallerMemberName] string? methodName = null)
        : base(methodName)
    {
    }

    public bool HookInsertSucceeded => hookInsertSucceeded;

    protected override void OnDatabaseConnected()
    {
        Execute("CREATE TABLE \"H22aHookParents\" (\"Id\" INTEGER PRIMARY KEY)");
        Execute("CREATE TABLE \"H22aHookChildren\" (\"Id\" INTEGER PRIMARY KEY, \"ParentId\" INTEGER REFERENCES \"H22aHookParents\"(\"Id\"))");

        try
        {
            Execute("INSERT INTO \"H22aHookChildren\" (\"Id\", \"ParentId\") VALUES (1, 999)");
            hookInsertSucceeded = true;
        }
        catch (SQLiteException)
        {
            hookInsertSucceeded = false;
        }
    }
}

public class ConnectionHookPragmaStateTests
{
    [Fact]
    public void TheConnectedHookSeesTheForeignKeySettingTheOpenEndsWith()
    {
        using H22aPragmaWatchingHookDatabase db = new();

        db.OpenConnection();

        Assert.Equal(db.ExecuteScalar<int>("PRAGMA foreign_keys"), db.ForeignKeysWhenConnected);
    }

    [Fact]
    public void TheConnectedHookCannotWriteARowThatBreaksAForeignKey()
    {
        using H22aForeignKeyHookDatabase db = new();

        db.OpenConnection();

        Assert.True(db.Options.IsForeignKeysEnabled);
        Assert.False(db.HookInsertSucceeded);
    }
}
