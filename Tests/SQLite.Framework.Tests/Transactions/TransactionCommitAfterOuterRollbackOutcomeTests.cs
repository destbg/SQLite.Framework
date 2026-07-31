using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H26nJournalRows")]
public class H26nJournalRow
{
    [Key]
    public int Id { get; set; }

    public string Name { get; set; } = "";
}

public class TransactionCommitAfterOuterRollbackOutcomeTests
{
    [Fact]
    public void InnerCommitAfterOuterRollbackAgreesWithWhatSurvived()
    {
        using TestDatabase db = Seeded();

        SQLiteTransaction outer = db.BeginTransaction();
        SQLiteTransaction inner = db.BeginTransaction();
        db.Table<H26nJournalRow>().Add(new H26nJournalRow { Id = 2, Name = "inner" });
        outer.Rollback();

        bool committed = Record.Exception(inner.Commit) == null;
        bool survived = db.Table<H26nJournalRow>().Any(r => r.Id == 2);

        Assert.Equal(committed, survived);
    }

    [Fact]
    public void InnerCommitAfterOuterDisposeAgreesWithWhatSurvived()
    {
        using TestDatabase db = Seeded();

        SQLiteTransaction outer = db.BeginTransaction();
        SQLiteTransaction inner = db.BeginTransaction();
        db.Table<H26nJournalRow>().Add(new H26nJournalRow { Id = 2, Name = "inner" });
        outer.Dispose();

        bool committed = Record.Exception(inner.Commit) == null;
        bool survived = db.Table<H26nJournalRow>().Any(r => r.Id == 2);

        Assert.Equal(committed, survived);
    }

    private static TestDatabase Seeded()
    {
        TestDatabase db = new();
        db.Table<H26nJournalRow>().Schema.CreateTable();
        db.Table<H26nJournalRow>().Add(new H26nJournalRow { Id = 1, Name = "seed" });
        return db;
    }
}
