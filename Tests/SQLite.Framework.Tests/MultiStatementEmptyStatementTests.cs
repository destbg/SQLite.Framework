using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class MultiStatementEmptyStatementTests
{
    [Fact]
    public void ExecuteTrailingEmptyStatementRunsWithoutError()
    {
        using TestDatabase db = new();
        db.Execute("CREATE TABLE t (x INTEGER)");

        db.Execute("INSERT INTO t VALUES (1);;");

        Assert.Equal(1, db.ExecuteScalar<int>("SELECT COUNT(*) FROM t"));
    }

    [Fact]
    public void ScalarQueryTrailingEmptyStatementRuns()
    {
        using TestDatabase db = new();

        Assert.Equal(1L, db.ExecuteScalar<long>("SELECT 1;;"));
    }

    [Fact]
    public void ExecuteEmptyStatementBetweenStatementsRunsBoth()
    {
        using TestDatabase db = new();
        db.Execute("CREATE TABLE t (x INTEGER)");

        db.Execute("INSERT INTO t VALUES (1);; INSERT INTO t VALUES (2)");

        Assert.Equal(2, db.ExecuteScalar<int>("SELECT COUNT(*) FROM t"));
    }

    [Fact]
    public void ExecuteLeadingEmptyStatementRunsStatement()
    {
        using TestDatabase db = new();
        db.Execute("CREATE TABLE t (x INTEGER)");

        db.Execute("; INSERT INTO t VALUES (1)");

        Assert.Equal(1, db.ExecuteScalar<int>("SELECT COUNT(*) FROM t"));
    }
}
