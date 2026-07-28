using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Enums;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H23iConflictRows")]
public class H23iConflictRow
{
    [Key]
    public int Id { get; set; }

    public string Name { get; set; } = "";
}

public class ConflictRollbackTransactionCommitOutcomeTests
{
    [Fact]
    public void CommitAfterAConflictRollbackAgreesWithWhatSurvived()
    {
        using TestDatabase db = Seeded();

        bool committed;
        using (SQLiteTransaction transaction = db.BeginTransaction())
        {
            db.Table<H23iConflictRow>().Add(new H23iConflictRow { Id = 2, Name = "inside" });
            Record.Exception(() => db.Table<H23iConflictRow>().AddOrUpdate(
                new H23iConflictRow { Id = 1, Name = "duplicate" }, SQLiteConflict.Rollback));

            committed = Record.Exception(transaction.Commit) == null;
        }

        bool survived = db.Table<H23iConflictRow>().Any(r => r.Id == 2);

        Assert.Equal(committed, survived);
    }

    [Fact]
    public void OuterCommitAfterANestedConflictRollbackAgreesWithWhatSurvived()
    {
        using TestDatabase db = Seeded();

        bool committed;
        using (SQLiteTransaction outer = db.BeginTransaction())
        {
            using (SQLiteTransaction inner = db.BeginTransaction())
            {
                db.Table<H23iConflictRow>().Add(new H23iConflictRow { Id = 2, Name = "inside" });
                Record.Exception(() => db.Table<H23iConflictRow>().AddOrUpdate(
                    new H23iConflictRow { Id = 1, Name = "duplicate" }, SQLiteConflict.Rollback));

                Record.Exception(inner.Commit);
            }

            committed = Record.Exception(outer.Commit) == null;
        }

        bool survived = db.Table<H23iConflictRow>().Any(r => r.Id == 2);

        Assert.Equal(committed, survived);
    }

    private static TestDatabase Seeded()
    {
        TestDatabase db = new();
        db.Table<H23iConflictRow>().Schema.CreateTable();
        db.Table<H23iConflictRow>().Add(new H23iConflictRow { Id = 1, Name = "seed" });
        return db;
    }
}
