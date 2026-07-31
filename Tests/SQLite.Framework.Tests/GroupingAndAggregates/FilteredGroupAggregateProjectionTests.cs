using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H26zFilteredGroupRows")]
public class H26zFilteredGroupRow
{
    [Key]
    public int Id { get; set; }

    public int Bucket { get; set; }

    public int Amount { get; set; }

    public bool Flag { get; set; }
}

public class FilteredGroupAggregateProjectionTests
{
    [Fact]
    public void AFilteredGroupSumReadsBesideTheKey()
    {
        using TestDatabase db = Setup(nameof(AFilteredGroupSumReadsBesideTheKey));

        List<string> expected = Rows()
            .GroupBy(r => r.Bucket)
            .Select(g => new { g.Key, S = g.Where(x => x.Flag).Sum(x => x.Amount) })
            .OrderBy(x => x.Key)
            .Select(x => x.Key + ":" + x.S)
            .ToList();

        Assert.Equal(new List<string> { "1:30", "2:5", "3:0" }, expected);

        List<string> actual = db.Table<H26zFilteredGroupRow>()
            .GroupBy(r => r.Bucket)
            .Select(g => new { g.Key, S = g.Where(x => x.Flag).Sum(x => x.Amount) })
            .OrderBy(x => x.Key)
            .Select(x => x.Key + ":" + x.S)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void AFilteredGroupCountReadsBesideTheKey()
    {
        using TestDatabase db = Setup(nameof(AFilteredGroupCountReadsBesideTheKey));

        List<string> expected = Rows()
            .GroupBy(r => r.Bucket)
            .Select(g => new { g.Key, N = g.Where(x => x.Flag).Count() })
            .OrderBy(x => x.Key)
            .Select(x => x.Key + ":" + x.N)
            .ToList();

        Assert.Equal(new List<string> { "1:2", "2:1", "3:0" }, expected);

        List<string> actual = db.Table<H26zFilteredGroupRow>()
            .GroupBy(r => r.Bucket)
            .Select(g => new { g.Key, N = g.Where(x => x.Flag).Count() })
            .OrderBy(x => x.Key)
            .Select(x => x.Key + ":" + x.N)
            .ToList();

        Assert.Equal(expected, actual);
    }

    private static List<H26zFilteredGroupRow> Rows()
    {
        return
        [
            new H26zFilteredGroupRow { Id = 1, Bucket = 1, Amount = 10, Flag = true },
            new H26zFilteredGroupRow { Id = 2, Bucket = 2, Amount = 30, Flag = false },
            new H26zFilteredGroupRow { Id = 3, Bucket = 1, Amount = 20, Flag = true },
            new H26zFilteredGroupRow { Id = 4, Bucket = 3, Amount = 30, Flag = false },
            new H26zFilteredGroupRow { Id = 5, Bucket = 2, Amount = 5, Flag = true }
        ];
    }

    private static TestDatabase Setup(string methodName)
    {
        TestDatabase db = new(null, methodName);
        db.Table<H26zFilteredGroupRow>().Schema.CreateTable();
        db.Table<H26zFilteredGroupRow>().AddRange(Rows());
        return db;
    }
}
