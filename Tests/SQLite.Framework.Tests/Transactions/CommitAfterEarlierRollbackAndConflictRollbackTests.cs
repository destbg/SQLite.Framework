using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Enums;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H24hGenerationRows")]
public class H24hGenerationRow
{
    [Key]
    public int Id { get; set; }

    public string Name { get; set; } = "";
}

public class CommitAfterEarlierRollbackAndConflictRollbackTests
{
    [Fact]
    public void OuterCommitAfterANestedRollbackThenAConflictRollbackAgreesWithWhatSurvived()
    {
        using TestDatabase db = Seeded();

        bool committed;
        using (SQLiteTransaction outer = db.BeginTransaction())
        {
            using (SQLiteTransaction inner = db.BeginTransaction())
            {
                inner.Rollback();
            }

            db.Table<H24hGenerationRow>().Add(new H24hGenerationRow { Id = 2, Name = "inside" });
            Record.Exception(() => db.Table<H24hGenerationRow>().AddOrUpdate(
                new H24hGenerationRow { Id = 1, Name = "duplicate" }, SQLiteConflict.Rollback));

            committed = Record.Exception(outer.Commit) == null;
        }

        bool survived = db.Table<H24hGenerationRow>().Any(r => r.Id == 2);

        Assert.Equal(committed, survived);
    }

    [Fact]
    public void OuterCommitAfterANestedDisposeThenAConflictRollbackAgreesWithWhatSurvived()
    {
        using TestDatabase db = Seeded();

        bool committed;
        using (SQLiteTransaction outer = db.BeginTransaction())
        {
            using (db.BeginTransaction())
            {
            }

            db.Table<H24hGenerationRow>().Add(new H24hGenerationRow { Id = 2, Name = "inside" });
            Record.Exception(() => db.Table<H24hGenerationRow>().AddOrUpdate(
                new H24hGenerationRow { Id = 1, Name = "duplicate" }, SQLiteConflict.Rollback));

            committed = Record.Exception(outer.Commit) == null;
        }

        bool survived = db.Table<H24hGenerationRow>().Any(r => r.Id == 2);

        Assert.Equal(committed, survived);
    }

    [Fact]
    public void OuterCommitAfterAFailedRangeWriteThenAConflictRollbackAgreesWithWhatSurvived()
    {
        using TestDatabase db = Seeded();

        bool committed;
        using (SQLiteTransaction outer = db.BeginTransaction())
        {
            Record.Exception(() => db.Table<H24hGenerationRow>().AddRange(
            [
                new H24hGenerationRow { Id = 3, Name = "first" },
                new H24hGenerationRow { Id = 1, Name = "duplicate" },
            ]));

            db.Table<H24hGenerationRow>().Add(new H24hGenerationRow { Id = 2, Name = "inside" });
            Record.Exception(() => db.Table<H24hGenerationRow>().AddOrUpdate(
                new H24hGenerationRow { Id = 1, Name = "duplicate" }, SQLiteConflict.Rollback));

            committed = Record.Exception(outer.Commit) == null;
        }

        bool survived = db.Table<H24hGenerationRow>().Any(r => r.Id == 2);

        Assert.Equal(committed, survived);
    }

    private static TestDatabase Seeded()
    {
        TestDatabase db = new();
        db.Table<H24hGenerationRow>().Schema.CreateTable();
        db.Table<H24hGenerationRow>().Add(new H24hGenerationRow { Id = 1, Name = "seed" });
        return db;
    }
}
