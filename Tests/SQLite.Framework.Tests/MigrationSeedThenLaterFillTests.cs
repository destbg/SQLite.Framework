using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("SeedPriceBook")]
public class SeedPriceBookRow
{
    [Key]
    public int Id { get; set; }

    public int Price { get; set; }
}

public class MigrationSeedThenLaterFillTests
{
    [Fact]
    public void StepwiseUpgradeAppliesTheVersionTwoFillToSeedRows()
    {
        using TestDatabase db = new(useFile: true);
        db.Schema.Migrations()
            .Version(1, m => m
                .CreateTable<SeedPriceBookRow>()
                .Insert(new SeedPriceBookRow { Id = 1, Price = 10 }))
            .Migrate();

        db.Schema.Migrations()
            .Version(1, m => m
                .CreateTable<SeedPriceBookRow>()
                .Insert(new SeedPriceBookRow { Id = 1, Price = 10 }))
            .Version(2, m => m.TableChanged<SeedPriceBookRow>(s => s.Set(r => r.Price, 99)))
            .Migrate();

        Assert.Equal(99, db.Table<SeedPriceBookRow>().Single().Price);
    }

    [Fact]
    public void FreshDatabaseAppliesTheVersionTwoFillToSeedRows()
    {
        using TestDatabase db = new(useFile: true);

        db.Schema.Migrations()
            .Version(1, m => m
                .CreateTable<SeedPriceBookRow>()
                .Insert(new SeedPriceBookRow { Id = 1, Price = 10 }))
            .Version(2, m => m.TableChanged<SeedPriceBookRow>(s => s.Set(r => r.Price, 99)))
            .Migrate();

        Assert.Equal(99, db.Table<SeedPriceBookRow>().Single().Price);
    }
}
