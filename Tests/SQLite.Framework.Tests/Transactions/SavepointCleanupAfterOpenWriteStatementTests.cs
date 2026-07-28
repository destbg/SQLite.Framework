using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class SavepointCleanupAfterOpenWriteStatementTests
{
    [Fact]
    public void ACommitBlockedByAnOpenWriteReaderLeavesTheConnectionOutOfAnyTransaction()
    {
        using TestDatabase db = Seeded();

        using (SQLiteTransaction transaction = db.BeginTransaction())
        {
            using SQLiteDataReader reader = ReturningReader(db);
            Assert.True(reader.Read());

            Record.Exception(transaction.Commit);
        }

        Exception? afterCleanup = Record.Exception(() => db.Pragmas.ForeignKeys = false);

        Assert.Null(afterCleanup);
    }

    [Fact]
    public void ARollbackBlockedByAnOpenWriteReaderLeavesTheConnectionOutOfAnyTransaction()
    {
        using TestDatabase db = Seeded();

        using (SQLiteTransaction transaction = db.BeginTransaction())
        {
            using SQLiteDataReader reader = ReturningReader(db);
            Assert.True(reader.Read());

            Record.Exception(transaction.Rollback);
        }

        Exception? afterCleanup = Record.Exception(() => db.Pragmas.ForeignKeys = false);

        Assert.Null(afterCleanup);
    }

    private static SQLiteDataReader ReturningReader(TestDatabase db)
    {
        return db
            .CreateCommand("INSERT INTO \"H23iCleanupRows\" (\"Value\") VALUES (2) RETURNING \"Id\"", [])
            .ExecuteReader();
    }

    private static TestDatabase Seeded()
    {
        TestDatabase db = new();
        db.Execute("CREATE TABLE \"H23iCleanupRows\" (\"Id\" INTEGER PRIMARY KEY, \"Value\" INTEGER)");
        db.Execute("INSERT INTO \"H23iCleanupRows\" (\"Id\", \"Value\") VALUES (1, 1)");
        return db;
    }
}
