using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H25cRegionSales")]
public class H25cRegionSale
{
    [Key]
    public int Id { get; set; }

    public int Region { get; set; }

    public int Channel { get; set; }

    public int Units { get; set; }
}

public class H25cSwappedKey
{
    public H25cSwappedKey(int region, int channel)
    {
        Region = region;
        Channel = channel;
    }

    public int Region { get; set; }

    public int Channel { get; set; }

    public override bool Equals(object? obj)
    {
        return obj is H25cSwappedKey other && other.Region == Region && other.Channel == Channel;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Region, Channel);
    }
}

public class ConstructedGroupKeyMemberValueTests
{
    [Fact]
    public void ConstructedKeyMemberReadsTheConstructorArgumentItWasBuiltFrom()
    {
        using TestDatabase db = Setup(nameof(ConstructedKeyMemberReadsTheConstructorArgumentItWasBuiltFrom));

        List<(int Region, int Count)> expected = Rows()
            .GroupBy(r => new H25cSwappedKey(r.Channel, r.Region))
            .Select(g => (Region: g.Key.Region, Count: g.Count()))
            .OrderBy(x => x.Region)
            .ThenBy(x => x.Count)
            .ToList();

        List<(int Region, int Count)> actual = db.Table<H25cRegionSale>()
            .GroupBy(r => new H25cSwappedKey(r.Channel, r.Region))
            .Select(g => new { Region = g.Key.Region, Count = g.Count() })
            .ToList()
            .Select(x => (Region: x.Region, Count: x.Count))
            .OrderBy(x => x.Region)
            .ThenBy(x => x.Count)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void BothConstructedKeyMembersReadTheArgumentsTheyWereBuiltFrom()
    {
        using TestDatabase db = Setup(nameof(BothConstructedKeyMembersReadTheArgumentsTheyWereBuiltFrom));

        List<(int Region, int Channel)> expected = Rows()
            .GroupBy(r => new H25cSwappedKey(r.Channel, r.Region))
            .Select(g => (Region: g.Key.Region, Channel: g.Key.Channel))
            .OrderBy(x => x.Region)
            .ThenBy(x => x.Channel)
            .ToList();

        List<(int Region, int Channel)> actual = db.Table<H25cRegionSale>()
            .GroupBy(r => new H25cSwappedKey(r.Channel, r.Region))
            .Select(g => new { Region = g.Key.Region, Channel = g.Key.Channel })
            .ToList()
            .Select(x => (Region: x.Region, Channel: x.Channel))
            .OrderBy(x => x.Region)
            .ThenBy(x => x.Channel)
            .ToList();

        Assert.Equal(expected, actual);
    }

    private static List<H25cRegionSale> Rows()
    {
        return
        [
            new H25cRegionSale { Id = 1, Region = 1, Channel = 10, Units = 5 },
            new H25cRegionSale { Id = 2, Region = 1, Channel = 10, Units = 7 },
            new H25cRegionSale { Id = 3, Region = 1, Channel = 20, Units = 9 },
            new H25cRegionSale { Id = 4, Region = 2, Channel = 10, Units = 4 }
        ];
    }

    private static TestDatabase Setup(string methodName)
    {
        TestDatabase db = new(null, methodName);
        db.Table<H25cRegionSale>().Schema.CreateTable();
        db.Table<H25cRegionSale>().AddRange(Rows());
        return db;
    }
}
