using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class ForcedSavepointCleanupCompletionTests
{
    [Fact]
    public void ACommitThatCannotReleaseTheSavepointLeavesTheConnectionOutOfAnyTransaction()
    {
        using TestDatabase db = SeededBlobRow();

        using (SQLiteTransaction transaction = db.BeginTransaction())
        using (db.OpenBlobStream("H22aForcedCleanupRows", "Data", 1, writable: true))
        {
            Record.Exception(() => transaction.Commit());
        }

        Exception? afterCleanup = Record.Exception(() => db.Pragmas.ForeignKeys = false);

        Assert.Null(afterCleanup);
    }

    [Fact]
    public void ARollbackThatCannotReleaseTheSavepointLeavesTheConnectionOutOfAnyTransaction()
    {
        using TestDatabase db = SeededBlobRow();

        using (SQLiteTransaction transaction = db.BeginTransaction())
        using (db.OpenBlobStream("H22aForcedCleanupRows", "Data", 1, writable: true))
        {
            Record.Exception(() => transaction.Rollback());
        }

        Exception? afterCleanup = Record.Exception(() => db.Pragmas.ForeignKeys = false);

        Assert.Null(afterCleanup);
    }

    [Fact]
    public void ADisposeThatCannotReleaseTheSavepointLeavesTheConnectionOutOfAnyTransaction()
    {
        using TestDatabase db = SeededBlobRow();

        SQLiteTransaction transaction = db.BeginTransaction();
        using (db.OpenBlobStream("H22aForcedCleanupRows", "Data", 1, writable: true))
        {
            Record.Exception(transaction.Dispose);
        }

        Exception? afterCleanup = Record.Exception(() => db.Pragmas.ForeignKeys = false);

        Assert.Null(afterCleanup);
    }

    private static TestDatabase SeededBlobRow()
    {
        TestDatabase db = new();
        db.Execute("CREATE TABLE \"H22aForcedCleanupRows\" (\"Id\" INTEGER PRIMARY KEY, \"Data\" BLOB)");
        db.Execute("INSERT INTO \"H22aForcedCleanupRows\" (\"Id\", \"Data\") VALUES (1, zeroblob(8))");
        return db;
    }
}
