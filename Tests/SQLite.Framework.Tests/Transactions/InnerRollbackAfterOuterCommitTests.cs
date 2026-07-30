using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H25jNestedOrderRows")]
public class H25jNestedOrderRow
{
    [Key]
    public int Id { get; set; }

    public string Name { get; set; } = "";
}

public class InnerRollbackAfterOuterCommitTests
{
    [Fact]
    public void InnerRollbackAfterOuterCommitAgreesWithWhatSurvived()
    {
        using TestDatabase db = Seeded();

        SQLiteTransaction outer = db.BeginTransaction();
        SQLiteTransaction inner = db.BeginTransaction();
        db.Table<H25jNestedOrderRow>().Add(new H25jNestedOrderRow { Id = 2, Name = "inner" });
        outer.Commit();

        bool rolledBack = Record.Exception(inner.Rollback) == null;
        bool survived = db.Table<H25jNestedOrderRow>().Any(r => r.Id == 2);

        Assert.Equal(rolledBack, !survived);
    }

    private static TestDatabase Seeded()
    {
        TestDatabase db = new();
        db.Table<H25jNestedOrderRow>().Schema.CreateTable();
        db.Table<H25jNestedOrderRow>().Add(new H25jNestedOrderRow { Id = 1, Name = "seed" });
        return db;
    }
}
