using SQLite.Framework;
using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class RowIdCommandChangeCountTests
{
    [Fact]
    public void StatementlessSqlReportsNoChangesAfterAnEarlierInsert()
    {
        using TestDatabase db = Seeded();

        (int Changes, long RowId) result = db.CreateCommand("-- nothing here", []).ExecuteWithLastRowId();

        Assert.Equal(0, result.Changes);
    }

    [Fact]
    public void SeparatorOnlySqlReportsNoChangesAfterAnEarlierInsert()
    {
        using TestDatabase db = Seeded();

        (int Changes, long RowId) result = db.CreateCommand(" ; ; ", []).ExecuteWithLastRowId();

        Assert.Equal(0, result.Changes);
    }

    [Fact]
    public void SchemaStatementReportsNoChangesAfterAnEarlierInsert()
    {
        using TestDatabase db = Seeded();

        (int Changes, long RowId) result = db
            .CreateCommand("CREATE TABLE \"H21iRowIdOther\" (\"A\" INTEGER)", [])
            .ExecuteWithLastRowId();

        Assert.Equal(0, result.Changes);
    }

    [Fact]
    public async Task StatementlessSqlReportsNoChangesOnTheAsyncPath()
    {
        using TestDatabase db = Seeded();

        (int Changes, long RowId) result = await db.CreateCommand("-- nothing here", []).ExecuteWithLastRowIdAsync();

        Assert.Equal(0, result.Changes);
    }

    [Fact]
    public void InsertReportsItsOwnChangeCount()
    {
        using TestDatabase db = Seeded();

        (int Changes, long RowId) result = db
            .CreateCommand("INSERT INTO \"H21iRowIdRows\" (\"Value\") VALUES (20)", [])
            .ExecuteWithLastRowId();

        Assert.Equal(1, result.Changes);
        Assert.Equal(2L, result.RowId);
    }

    [Fact]
    public void DeleteThatMatchesNothingReportsNoChanges()
    {
        using TestDatabase db = Seeded();

        (int Changes, long RowId) result = db
            .CreateCommand("DELETE FROM \"H21iRowIdRows\" WHERE \"Id\" = 999", [])
            .ExecuteWithLastRowId();

        Assert.Equal(0, result.Changes);
    }

    private static TestDatabase Seeded()
    {
        TestDatabase db = new();
        db.Execute("CREATE TABLE \"H21iRowIdRows\" (\"Id\" INTEGER PRIMARY KEY, \"Value\" INTEGER)");
        db.Execute("INSERT INTO \"H21iRowIdRows\" (\"Id\", \"Value\") VALUES (1, 10)");
        return db;
    }
}
