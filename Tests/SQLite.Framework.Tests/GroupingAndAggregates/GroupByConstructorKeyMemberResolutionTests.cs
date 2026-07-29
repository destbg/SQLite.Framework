using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H24kPairRows")]
public class H24kPairRow
{
    [Key]
    public int Id { get; set; }

    public int Region { get; set; }

    public int Channel { get; set; }

    public int Units { get; set; }
}

public record H24kPairKey(int Region, int Channel);

public class GroupByConstructorKeyMemberResolutionTests
{
    [Fact]
    public void TupleKeySecondMemberReadsTheSecondKeyColumn()
    {
        using TestDatabase db = Setup(nameof(TupleKeySecondMemberReadsTheSecondKeyColumn));

        List<(int Channel, int Count)> expected = Rows()
            .GroupBy(r => ValueTuple.Create(r.Region, r.Channel))
            .Select(g => (Channel: g.Key.Item2, Count: g.Count()))
            .OrderBy(x => x.Channel)
            .ThenBy(x => x.Count)
            .ToList();

        List<(int Channel, int Count)> actual = db.Table<H24kPairRow>()
            .GroupBy(r => ValueTuple.Create(r.Region, r.Channel))
            .Select(g => new { Channel = g.Key.Item2, Count = g.Count() })
            .ToList()
            .Select(x => (Channel: x.Channel, Count: x.Count))
            .OrderBy(x => x.Channel)
            .ThenBy(x => x.Count)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void TupleKeyMembersReadTheirOwnKeyColumns()
    {
        using TestDatabase db = Setup(nameof(TupleKeyMembersReadTheirOwnKeyColumns));

        List<(int Region, int Channel, int Count)> expected = Rows()
            .GroupBy(r => ValueTuple.Create(r.Region, r.Channel))
            .Select(g => (Region: g.Key.Item1, Channel: g.Key.Item2, Count: g.Count()))
            .OrderBy(x => x.Region)
            .ThenBy(x => x.Channel)
            .ToList();

        List<(int Region, int Channel, int Count)> actual = db.Table<H24kPairRow>()
            .GroupBy(r => ValueTuple.Create(r.Region, r.Channel))
            .Select(g => new { Region = g.Key.Item1, Channel = g.Key.Item2, Count = g.Count() })
            .ToList()
            .Select(x => (Region: x.Region, Channel: x.Channel, Count: x.Count))
            .OrderBy(x => x.Region)
            .ThenBy(x => x.Channel)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ConstructedKeyMemberReadsItsOwnConstructorArgument()
    {
        using TestDatabase db = Setup(nameof(ConstructedKeyMemberReadsItsOwnConstructorArgument));

        List<(int Region, int Count)> expected = Rows()
            .GroupBy(r => new H24kPairKey(r.Channel, r.Region))
            .Select(g => (Region: g.Key.Region, Count: g.Count()))
            .OrderBy(x => x.Region)
            .ThenBy(x => x.Count)
            .ToList();

        List<(int Region, int Count)> actual = db.Table<H24kPairRow>()
            .GroupBy(r => new H24kPairKey(r.Channel, r.Region))
            .Select(g => new { Region = g.Key.Region, Count = g.Count() })
            .ToList()
            .Select(x => (Region: x.Region, Count: x.Count))
            .OrderBy(x => x.Region)
            .ThenBy(x => x.Count)
            .ToList();

        Assert.Equal(expected, actual);
    }

    private static List<H24kPairRow> Rows()
    {
        return
        [
            new H24kPairRow { Id = 1, Region = 1, Channel = 10, Units = 5 },
            new H24kPairRow { Id = 2, Region = 1, Channel = 10, Units = 7 },
            new H24kPairRow { Id = 3, Region = 1, Channel = 20, Units = 9 },
            new H24kPairRow { Id = 4, Region = 2, Channel = 10, Units = 4 }
        ];
    }

    private static TestDatabase Setup(string methodName)
    {
        TestDatabase db = new(null, methodName);
        db.Table<H24kPairRow>().Schema.CreateTable();
        db.Table<H24kPairRow>().AddRange(Rows());
        return db;
    }
}
