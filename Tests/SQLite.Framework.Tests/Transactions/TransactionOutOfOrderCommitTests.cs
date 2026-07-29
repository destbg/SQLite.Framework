using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H24hOrderRows")]
public class H24hOrderRow
{
    [Key]
    public int Id { get; set; }

    public string Name { get; set; } = "";
}

public class TransactionOutOfOrderCommitTests
{
    [Fact]
    public void InnerCommitAfterOuterCommitAgreesWithWhatSurvived()
    {
        using TestDatabase db = Seeded();

        SQLiteTransaction outer = db.BeginTransaction();
        SQLiteTransaction inner = db.BeginTransaction();
        db.Table<H24hOrderRow>().Add(new H24hOrderRow { Id = 2, Name = "inner" });
        outer.Commit();

        bool committed = Record.Exception(inner.Commit) == null;
        bool survived = db.Table<H24hOrderRow>().Any(r => r.Id == 2);

        Assert.Equal(committed, survived);
    }

    [Fact]
    public void InnerCommitAfterOuterCommitDoesNotThrow()
    {
        using TestDatabase db = Seeded();

        SQLiteTransaction outer = db.BeginTransaction();
        SQLiteTransaction inner = db.BeginTransaction();
        outer.Commit();

        Exception? ex = Record.Exception(inner.Commit);

        Assert.Null(ex);
    }

    private static TestDatabase Seeded()
    {
        TestDatabase db = new();
        db.Table<H24hOrderRow>().Schema.CreateTable();
        db.Table<H24hOrderRow>().Add(new H24hOrderRow { Id = 1, Name = "seed" });
        return db;
    }
}
