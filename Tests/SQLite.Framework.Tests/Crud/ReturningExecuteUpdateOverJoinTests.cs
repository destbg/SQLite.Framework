using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H26oJoinPriceItems")]
public class H26oJoinPriceItem
{
    [Key]
    public int Id { get; set; }

    public int OwnerId { get; set; }

    public int Price { get; set; }
}

[Table("H26oJoinPriceOwners")]
public class H26oJoinPriceOwner
{
    [Key]
    public int Id { get; set; }

    public int Bonus { get; set; }
}

public class ReturningExecuteUpdateOverJoinTests
{
    [Fact]
    public void ReturningExecuteUpdateOverAJoinAppliesASetterThatReadsTheTargetColumn()
    {
        using TestDatabase db = Setup(nameof(ReturningExecuteUpdateOverAJoinAppliesASetterThatReadsTheTargetColumn));

        List<int> expected = Items()
            .Join(Owners(), i => i.OwnerId, o => o.Id, (i, o) => i.Price + 1)
            .OrderBy(v => v)
            .ToList();

        List<int> returned = db.Table<H26oJoinPriceItem>()
            .Join(db.Table<H26oJoinPriceOwner>(), i => i.OwnerId, o => o.Id, (i, o) => new { i, o })
            .Returning(x => x.i.Price)
            .ExecuteUpdate(s => s.Set(x => x.i.Price, x => x.i.Price + 1));

        Assert.Equal(expected, returned.OrderBy(v => v).ToList());

        List<int> stored = db.Table<H26oJoinPriceItem>()
            .OrderBy(i => i.Id)
            .Select(i => i.Price)
            .ToList();

        Assert.Equal(expected, stored);
    }

    [Fact]
    public void ReturningExecuteUpdateOverAJoinAppliesASetterThatReadsTheJoinedColumn()
    {
        using TestDatabase db = Setup(nameof(ReturningExecuteUpdateOverAJoinAppliesASetterThatReadsTheJoinedColumn));

        List<int> expected = Items()
            .Join(Owners(), i => i.OwnerId, o => o.Id, (i, o) => i.Price + o.Bonus)
            .OrderBy(v => v)
            .ToList();

        List<int> returned = db.Table<H26oJoinPriceItem>()
            .Join(db.Table<H26oJoinPriceOwner>(), i => i.OwnerId, o => o.Id, (i, o) => new { i, o })
            .Returning(x => x.i.Price)
            .ExecuteUpdate(s => s.Set(x => x.i.Price, x => x.i.Price + x.o.Bonus));

        Assert.Equal(expected, returned.OrderBy(v => v).ToList());

        List<int> stored = db.Table<H26oJoinPriceItem>()
            .OrderBy(i => i.Id)
            .Select(i => i.Price)
            .ToList();

        Assert.Equal(expected, stored);
    }

    private static List<H26oJoinPriceItem> Items()
    {
        return
        [
            new H26oJoinPriceItem { Id = 1, OwnerId = 10, Price = 100 },
            new H26oJoinPriceItem { Id = 2, OwnerId = 20, Price = 200 }
        ];
    }

    private static List<H26oJoinPriceOwner> Owners()
    {
        return
        [
            new H26oJoinPriceOwner { Id = 10, Bonus = 5 },
            new H26oJoinPriceOwner { Id = 20, Bonus = 7 }
        ];
    }

    private static TestDatabase Setup(string methodName)
    {
        TestDatabase db = new(null, methodName);
        db.Table<H26oJoinPriceOwner>().Schema.CreateTable();
        db.Table<H26oJoinPriceItem>().Schema.CreateTable();
        db.Table<H26oJoinPriceOwner>().AddRange(Owners());
        db.Table<H26oJoinPriceItem>().AddRange(Items());
        return db;
    }
}
