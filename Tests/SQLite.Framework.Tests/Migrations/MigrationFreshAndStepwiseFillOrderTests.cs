using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework;
using SQLite.Framework.Attributes;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("FillOrderBook")]
public class FillOrderBookRow
{
    [Key]
    public int Id { get; set; }

    public int Price { get; set; }
}

public class MigrationFreshAndStepwiseFillOrderTests
{
    [Fact]
    public void RunCallbackWriteAgreesAcrossFreshAndStepwise()
    {
        int stepwise;
        using (TestDatabase db = new(useFile: true))
        {
            db.Schema.Migrations()
                .Version(1, m => m.CreateTable<FillOrderBookRow>().Insert(new FillOrderBookRow { Id = 1, Price = 10 }))
                .Migrate();
            db.Schema.Migrations()
                .Version(1, m => m.CreateTable<FillOrderBookRow>().Insert(new FillOrderBookRow { Id = 1, Price = 10 }))
                .Version(2, m => m
                    .TableChanged<FillOrderBookRow>(s => s.Set(r => r.Price, 99))
                    .Run(ctx => ctx.Database.Execute("UPDATE \"FillOrderBook\" SET \"Price\" = 55 WHERE \"Id\" = 1")))
                .Migrate();
            stepwise = db.Table<FillOrderBookRow>().Single().Price;
        }

        int fresh;
        using (TestDatabase db = new(useFile: true))
        {
            db.Schema.Migrations()
                .Version(1, m => m.CreateTable<FillOrderBookRow>().Insert(new FillOrderBookRow { Id = 1, Price = 10 }))
                .Version(2, m => m
                    .TableChanged<FillOrderBookRow>(s => s.Set(r => r.Price, 99))
                    .Run(ctx => ctx.Database.Execute("UPDATE \"FillOrderBook\" SET \"Price\" = 55 WHERE \"Id\" = 1")))
                .Migrate();
            fresh = db.Table<FillOrderBookRow>().Single().Price;
        }

        Assert.Equal(stepwise, fresh);
    }

    [Fact]
    public void SameVersionInsertAgreesAcrossFreshAndStepwise()
    {
        List<int> stepwise;
        using (TestDatabase db = new(useFile: true))
        {
            db.Schema.Migrations()
                .Version(1, m => m.CreateTable<FillOrderBookRow>().Insert(new FillOrderBookRow { Id = 1, Price = 10 }))
                .Migrate();
            db.Schema.Migrations()
                .Version(1, m => m.CreateTable<FillOrderBookRow>().Insert(new FillOrderBookRow { Id = 1, Price = 10 }))
                .Version(2, m => m
                    .TableChanged<FillOrderBookRow>(s => s.Set(r => r.Price, 99))
                    .Insert(new FillOrderBookRow { Id = 2, Price = 10 }))
                .Migrate();
            stepwise = db.Table<FillOrderBookRow>().OrderBy(r => r.Id).Select(r => r.Price).ToList();
        }

        List<int> fresh;
        using (TestDatabase db = new(useFile: true))
        {
            db.Schema.Migrations()
                .Version(1, m => m.CreateTable<FillOrderBookRow>().Insert(new FillOrderBookRow { Id = 1, Price = 10 }))
                .Version(2, m => m
                    .TableChanged<FillOrderBookRow>(s => s.Set(r => r.Price, 99))
                    .Insert(new FillOrderBookRow { Id = 2, Price = 10 }))
                .Migrate();
            fresh = db.Table<FillOrderBookRow>().OrderBy(r => r.Id).Select(r => r.Price).ToList();
        }

        Assert.Equal(stepwise, fresh);
    }
}
