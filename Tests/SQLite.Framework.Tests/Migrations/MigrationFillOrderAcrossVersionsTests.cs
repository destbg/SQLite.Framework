using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("PricedBook")]
public class PricedBookRow
{
    [Key]
    public int Id { get; set; }

    public int Price { get; set; }
}

public class MigrationFillOrderAcrossVersionsTests
{
    [Fact]
    public void FillRunsAfterEarlierVersionInsertsOnAnUpgradedDatabase()
    {
        using TestDatabase stepwise = new(useFile: true);
        stepwise.Schema.Migrations()
            .Version(1, m => m.CreateTable<PricedBookRow>().Insert(new PricedBookRow { Id = 1, Price = 10 }))
            .Migrate();
        stepwise.Schema.Migrations()
            .Version(1, m => m.CreateTable<PricedBookRow>().Insert(new PricedBookRow { Id = 1, Price = 10 }))
            .Version(2, m => m.Insert(new PricedBookRow { Id = 2, Price = 10 }))
            .Migrate();
        stepwise.Schema.Migrations()
            .Version(1, m => m.CreateTable<PricedBookRow>().Insert(new PricedBookRow { Id = 1, Price = 10 }))
            .Version(2, m => m.Insert(new PricedBookRow { Id = 2, Price = 10 }))
            .Version(3, m => m.TableChanged<PricedBookRow>(s => s.Set(x => x.Price, 99)))
            .Migrate();

        using TestDatabase upgraded = new(useFile: true);
        upgraded.Schema.Migrations()
            .Version(1, m => m.CreateTable<PricedBookRow>().Insert(new PricedBookRow { Id = 1, Price = 10 }))
            .Migrate();
        upgraded.Schema.Migrations()
            .Version(1, m => m.CreateTable<PricedBookRow>().Insert(new PricedBookRow { Id = 1, Price = 10 }))
            .Version(2, m => m.Insert(new PricedBookRow { Id = 2, Price = 10 }))
            .Version(3, m => m.TableChanged<PricedBookRow>(s => s.Set(x => x.Price, 99)))
            .Migrate();

        List<int> expected = stepwise.Table<PricedBookRow>().OrderBy(x => x.Id).Select(x => x.Price).ToList();
        List<int> actual = upgraded.Table<PricedBookRow>().OrderBy(x => x.Id).Select(x => x.Price).ToList();
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ChainedFillsOnTheSameColumnMatchStepwiseRuns()
    {
        using TestDatabase stepwise = new(useFile: true);
        stepwise.Schema.Migrations()
            .Version(1, m => m.CreateTable<PricedBookRow>().Insert(new PricedBookRow { Id = 1, Price = 10 }))
            .Migrate();
        stepwise.Schema.Migrations()
            .Version(1, m => m.CreateTable<PricedBookRow>().Insert(new PricedBookRow { Id = 1, Price = 10 }))
            .Version(2, m => m.TableChanged<PricedBookRow>(s => s.Set(x => x.Price, 99)))
            .Migrate();
        stepwise.Schema.Migrations()
            .Version(1, m => m.CreateTable<PricedBookRow>().Insert(new PricedBookRow { Id = 1, Price = 10 }))
            .Version(2, m => m.TableChanged<PricedBookRow>(s => s.Set(x => x.Price, 99)))
            .Version(3, m => m.TableChanged<PricedBookRow>(s => s.Set(x => x.Price, x => x.Price * 2)))
            .Migrate();

        using TestDatabase upgraded = new(useFile: true);
        upgraded.Schema.Migrations()
            .Version(1, m => m.CreateTable<PricedBookRow>().Insert(new PricedBookRow { Id = 1, Price = 10 }))
            .Migrate();
        upgraded.Schema.Migrations()
            .Version(1, m => m.CreateTable<PricedBookRow>().Insert(new PricedBookRow { Id = 1, Price = 10 }))
            .Version(2, m => m.TableChanged<PricedBookRow>(s => s.Set(x => x.Price, 99)))
            .Version(3, m => m.TableChanged<PricedBookRow>(s => s.Set(x => x.Price, x => x.Price * 2)))
            .Migrate();

        Assert.Equal(
            stepwise.Table<PricedBookRow>().Single().Price,
            upgraded.Table<PricedBookRow>().Single().Price);
    }
}
