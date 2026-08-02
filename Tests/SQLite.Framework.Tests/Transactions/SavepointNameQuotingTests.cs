using SQLite.Framework;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class SavepointNameQuotingTests
{
    [Fact]
    public void CommitOfASavepointWhoseNameNeedsQuotingDoesNotFailWithARawEngineError()
    {
        using TestDatabase db = new();
        db.Execute("CREATE TABLE \"SecLSavepointRows\" (\"Id\" INTEGER PRIMARY KEY)");
        db.Execute("SAVEPOINT \"SecL outer sp\"");
        db.Execute("INSERT INTO \"SecLSavepointRows\" (\"Id\") VALUES (1)");

        Exception? exception = Record.Exception(() =>
            new SQLiteTransaction(db, "SecL outer sp", ownsLock: false).Commit());

        Assert.True(exception is ArgumentException
            || (exception == null && db.ExecuteScalar<int>("SELECT COUNT(*) FROM \"SecLSavepointRows\"") == 1));
    }

    [Fact]
    public void CommitDoesNotRunStatementsSmuggledInsideTheSavepointName()
    {
        using TestDatabase db = new();
        db.Execute("CREATE TABLE \"SecLSavepointMarks\" (\"Id\" INTEGER PRIMARY KEY)");
        db.Execute("SAVEPOINT base_sp");

        Exception? exception = Record.Exception(() =>
            new SQLiteTransaction(db, "base_sp; INSERT INTO \"SecLSavepointMarks\" (\"Id\") VALUES (1); --", ownsLock: false).Commit());

        Assert.True(exception is ArgumentException
            || db.ExecuteScalar<int>("SELECT COUNT(*) FROM \"SecLSavepointMarks\"") == 0);
    }
}
