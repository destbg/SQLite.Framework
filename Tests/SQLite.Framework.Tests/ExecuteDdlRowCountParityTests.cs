using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class ExecuteDdlRowCountParityTests
{
    [Fact]
    public void DdlAfterInsertInSameBatch_CountsOnlyTheInsert()
    {
        using TestDatabase db = new();
        db.Execute("CREATE TABLE t (x INTEGER)");

        int affected = db.Execute("INSERT INTO t VALUES (1); CREATE TABLE u (x INTEGER)");

        Assert.Equal(1, affected);
    }

    [Fact]
    public void StandaloneDdlAfterPriorInsert_CountsZero()
    {
        using TestDatabase db = new();
        db.Execute("CREATE TABLE t (x INTEGER)");
        int insertCount = db.Execute("INSERT INTO t VALUES (1)");
        Assert.Equal(1, insertCount);

        int affected = db.Execute("CREATE TABLE u (x INTEGER)");

        Assert.Equal(0, affected);
    }
}
