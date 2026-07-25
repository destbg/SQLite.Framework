using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("RecreateSeedLog")]
public class RecreateSeedLogRow
{
    [Key]
    public int Id { get; set; }

    public string Note { get; set; } = "";
}

public class MigrationRecreatedTableSeedRowTests
{
    [Fact]
    public void StepwiseUpgradeLeavesTheRecreatedTableEmpty()
    {
        using TestDatabase db = new(useFile: true);
        db.Schema.Migrations()
            .Version(1, m => m.CreateTable<RecreateSeedLogRow>().Insert(new RecreateSeedLogRow { Id = 1, Note = "seed" }))
            .Migrate();
        db.Schema.Migrations()
            .Version(1, m => m.CreateTable<RecreateSeedLogRow>().Insert(new RecreateSeedLogRow { Id = 1, Note = "seed" }))
            .Version(2, m => m.DropTable<RecreateSeedLogRow>())
            .Migrate();
        db.Schema.Migrations()
            .Version(1, m => m.CreateTable<RecreateSeedLogRow>().Insert(new RecreateSeedLogRow { Id = 1, Note = "seed" }))
            .Version(2, m => m.DropTable<RecreateSeedLogRow>())
            .Version(3, m => m.CreateTable<RecreateSeedLogRow>())
            .Migrate();

        Assert.Equal(0, db.Table<RecreateSeedLogRow>().Count());
    }

    [Fact]
    public void FreshDatabaseLeavesTheRecreatedTableEmpty()
    {
        using TestDatabase db = new(useFile: true);
        db.Schema.Migrations()
            .Version(1, m => m.CreateTable<RecreateSeedLogRow>().Insert(new RecreateSeedLogRow { Id = 1, Note = "seed" }))
            .Version(2, m => m.DropTable<RecreateSeedLogRow>())
            .Version(3, m => m.CreateTable<RecreateSeedLogRow>())
            .Migrate();

        Assert.Equal(0, db.Table<RecreateSeedLogRow>().Count());
    }
}
