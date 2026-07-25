using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class MultiStatementExecuteRowCountParityTests
{
    [Fact]
    public void Execute_DeleteThenSelect_ReportsActualRowsChanged()
    {
        using TestDatabase db = new();
        db.Execute("CREATE TABLE t (x INTEGER)");
        db.Execute("INSERT INTO t VALUES (1)");
        db.Execute("INSERT INTO t VALUES (2)");

        int before = db.ExecuteScalar<int>("SELECT COUNT(*) FROM t");
        int affected = db.Execute("DELETE FROM t; SELECT 1");
        int after = db.ExecuteScalar<int>("SELECT COUNT(*) FROM t");

        Assert.Equal(before - after, affected);
    }

    [Fact]
    public void Execute_InsertThenSelect_ReportsActualRowsChanged()
    {
        using TestDatabase db = new();
        db.Execute("CREATE TABLE t (x INTEGER)");

        int before = db.ExecuteScalar<int>("SELECT COUNT(*) FROM t");
        int affected = db.Execute("INSERT INTO t VALUES (1); SELECT 1");
        int after = db.ExecuteScalar<int>("SELECT COUNT(*) FROM t");

        Assert.Equal(after - before, affected);
    }
}
