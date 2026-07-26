using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H22kBasketRows")]
public class H22kBasketRow
{
    [Key]
    public int Id { get; set; }

    public int OwnerId { get; set; }
}

[Table("H22kOwnerRows")]
public class H22kOwnerRow
{
    [Key]
    public int Id { get; set; }

    public string Name { get; set; } = "";
}

public class JoinWithoutComparerArgumentTests
{
    [Fact]
    public void JoinWithANullComparerJoinsTheRows()
    {
        using TestDatabase db = Setup();
        List<H22kBasketRow> baskets = Baskets();
        List<H22kOwnerRow> owners = Owners();

        List<string> expected = baskets
            .Join(owners, b => b.OwnerId, o => o.Id, (b, o) => b.Id + "-" + o.Name, null)
            .OrderBy(s => s)
            .ToList();

        List<string> actual = db.Table<H22kBasketRow>()
            .Join(db.Table<H22kOwnerRow>(), b => b.OwnerId, o => o.Id, (b, o) => b.Id + "-" + o.Name, null)
            .AsEnumerable()
            .OrderBy(s => s)
            .ToList();

        Assert.Equal(expected, actual);
    }

    private static List<H22kBasketRow> Baskets()
    {
        return
        [
            new H22kBasketRow { Id = 1, OwnerId = 10 },
            new H22kBasketRow { Id = 2, OwnerId = 11 },
            new H22kBasketRow { Id = 3, OwnerId = 99 }
        ];
    }

    private static List<H22kOwnerRow> Owners()
    {
        return
        [
            new H22kOwnerRow { Id = 10, Name = "ann" },
            new H22kOwnerRow { Id = 11, Name = "bob" }
        ];
    }

    private static TestDatabase Setup()
    {
        TestDatabase db = new();
        db.Table<H22kBasketRow>().Schema.CreateTable();
        db.Table<H22kOwnerRow>().Schema.CreateTable();
        db.Table<H22kBasketRow>().AddRange(Baskets());
        db.Table<H22kOwnerRow>().AddRange(Owners());
        return db;
    }
}
