using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Enums;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H26nLedgerRows")]
public class H26nLedgerRow
{
    [Key]
    public int Id { get; set; }

    public string Name { get; set; } = "";
}

public class TransactionRollbackAfterInterveningCommitTests
{
    [Fact]
    public void InnerRollbackAfterOuterRollbackAgreesWithWhatSurvivedWhenASiblingCommitted()
    {
        using TestDatabase db = Seeded();

        SQLiteTransaction outer = db.BeginTransaction();
        SQLiteTransaction inner = db.BeginTransaction();
        db.Table<H26nLedgerRow>().Add(new H26nLedgerRow { Id = 2, Name = "inner" });

        using (SQLiteTransaction sibling = db.BeginTransaction())
        {
            sibling.Commit();
        }

        outer.Rollback();

        bool rolledBack = Record.Exception(inner.Rollback) == null;
        bool survived = db.Table<H26nLedgerRow>().Any(r => r.Id == 2);

        Assert.Equal(rolledBack, !survived);
    }

    [Fact]
    public void InnerRollbackAfterOuterRollbackAgreesWithWhatSurvivedWhenARangeWriteRanInBetween()
    {
        using TestDatabase db = Seeded();

        SQLiteTransaction outer = db.BeginTransaction();
        SQLiteTransaction inner = db.BeginTransaction();
        db.Table<H26nLedgerRow>().AddRange([new H26nLedgerRow { Id = 2, Name = "inner" }]);

        outer.Rollback();

        bool rolledBack = Record.Exception(inner.Rollback) == null;
        bool survived = db.Table<H26nLedgerRow>().Any(r => r.Id == 2);

        Assert.Equal(rolledBack, !survived);
    }

    [Fact]
    public void OuterRollbackAfterAConflictRollbackAgreesWithWhatSurvivedWhenAnInnerCommitted()
    {
        using TestDatabase db = Seeded();

        SQLiteTransaction outer = db.BeginTransaction();

        using (SQLiteTransaction inner = db.BeginTransaction())
        {
            inner.Commit();
        }

        db.Table<H26nLedgerRow>().Add(new H26nLedgerRow { Id = 2, Name = "inner" });
        Record.Exception(() => db.Table<H26nLedgerRow>().AddOrUpdate(
            new H26nLedgerRow { Id = 1, Name = "duplicate" }, SQLiteConflict.Rollback));

        bool rolledBack = Record.Exception(outer.Rollback) == null;
        bool survived = db.Table<H26nLedgerRow>().Any(r => r.Id == 2);

        Assert.Equal(rolledBack, !survived);
    }

    private static TestDatabase Seeded()
    {
        TestDatabase db = new();
        db.Table<H26nLedgerRow>().Schema.CreateTable();
        db.Table<H26nLedgerRow>().Add(new H26nLedgerRow { Id = 1, Name = "seed" });
        return db;
    }
}
