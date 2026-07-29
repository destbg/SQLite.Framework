using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class WriteAfterBlockedSavepointReleaseTests
{
    [Fact]
    public void AWriteMadeAfterACommitBlockedByAnOpenWriteReaderSurvives()
    {
        using TestDatabase db = Seeded();

        int changes;
        using (SQLiteTransaction transaction = db.BeginTransaction())
        {
            using SQLiteDataReader open = WriteReader(db);
            Assert.True(open.Read());

            Record.Exception(transaction.Commit);

            changes = InsertMarker(db);
        }

        Assert.Equal(changes, StoredMarkers(db));
    }

    [Fact]
    public void AWriteMadeAfterARollbackBlockedByAnOpenWriteReaderSurvives()
    {
        using TestDatabase db = Seeded();

        int changes;
        using (SQLiteTransaction transaction = db.BeginTransaction())
        {
            using SQLiteDataReader open = WriteReader(db);
            Assert.True(open.Read());

            Record.Exception(transaction.Rollback);

            changes = InsertMarker(db);
        }

        Assert.Equal(changes, StoredMarkers(db));
    }

    [Fact]
    public void AWriteMadeAfterACommitBlockedByAnOpenQueryReaderSurvives()
    {
        using TestDatabase db = Seeded();

        int changes;
        using (SQLiteTransaction transaction = db.BeginTransaction())
        {
            using SQLiteDataReader open = QueryReader(db);
            Assert.True(open.Read());

            Record.Exception(transaction.Commit);

            changes = InsertMarker(db);
        }

        Assert.Equal(changes, StoredMarkers(db));
    }

    [Fact]
    public void AWriteMadeAfterARollbackBlockedByAnOpenQueryReaderSurvives()
    {
        using TestDatabase db = Seeded();

        int changes;
        using (SQLiteTransaction transaction = db.BeginTransaction())
        {
            using SQLiteDataReader open = QueryReader(db);
            Assert.True(open.Read());

            Record.Exception(transaction.Rollback);

            changes = InsertMarker(db);
        }

        Assert.Equal(changes, StoredMarkers(db));
    }

    private static SQLiteDataReader WriteReader(TestDatabase db)
    {
        return db
            .CreateCommand("INSERT INTO \"H24hBlockedReleaseRows\" (\"Value\") VALUES (2) RETURNING \"Id\"", [])
            .ExecuteReader();
    }

    private static SQLiteDataReader QueryReader(TestDatabase db)
    {
        return db
            .CreateCommand("SELECT \"Id\" FROM \"H24hBlockedReleaseRows\" ORDER BY \"Id\"", [])
            .ExecuteReader();
    }

    private static int InsertMarker(TestDatabase db)
    {
        return db.Execute("INSERT INTO \"H24hBlockedReleaseRows\" (\"Id\", \"Value\") VALUES (9, 9)");
    }

    private static int StoredMarkers(TestDatabase db)
    {
        return db.ExecuteScalar<int>("SELECT COUNT(*) FROM \"H24hBlockedReleaseRows\" WHERE \"Id\" = 9");
    }

    private static TestDatabase Seeded()
    {
        TestDatabase db = new();
        db.Execute("CREATE TABLE \"H24hBlockedReleaseRows\" (\"Id\" INTEGER PRIMARY KEY, \"Value\" INTEGER)");
        db.Execute("INSERT INTO \"H24hBlockedReleaseRows\" (\"Id\", \"Value\") VALUES (1, 1)");
        db.Execute("INSERT INTO \"H24hBlockedReleaseRows\" (\"Id\", \"Value\") VALUES (2, 1)");
        return db;
    }
}
