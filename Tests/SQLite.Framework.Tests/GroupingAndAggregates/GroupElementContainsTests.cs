using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H24kTicketRows")]
public class H24kTicketRow
{
    [Key]
    public int Id { get; set; }

    public string Kind { get; set; } = "";

    public int Score { get; set; }
}

public class GroupElementContainsTests
{
    [Fact]
    public void ContainsOverTheGroupElementsReadsBackPerGroup()
    {
        using TestDatabase db = Setup(nameof(ContainsOverTheGroupElementsReadsBackPerGroup));

        List<(string Kind, bool Has)> expected = Rows()
            .GroupBy(r => r.Kind, r => r.Score)
            .Select(g => (Kind: g.Key, Has: g.Contains(30)))
            .OrderBy(x => x.Kind, StringComparer.Ordinal)
            .ToList();

        List<(string Kind, bool Has)> actual = db.Table<H24kTicketRow>()
            .GroupBy(r => r.Kind, r => r.Score)
            .Select(g => new { Kind = g.Key, Has = g.Contains(30) })
            .ToList()
            .Select(x => (Kind: x.Kind, Has: x.Has))
            .OrderBy(x => x.Kind, StringComparer.Ordinal)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ContainsOverTheGroupElementsFiltersGroups()
    {
        using TestDatabase db = Setup(nameof(ContainsOverTheGroupElementsFiltersGroups));

        List<string> expected = Rows()
            .GroupBy(r => r.Kind, r => r.Score)
            .Where(g => g.Contains(30))
            .Select(g => g.Key)
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToList();

        List<string> actual = db.Table<H24kTicketRow>()
            .GroupBy(r => r.Kind, r => r.Score)
            .Where(g => g.Contains(30))
            .Select(g => g.Key)
            .ToList()
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToList();

        Assert.Equal(expected, actual);
    }

    private static List<H24kTicketRow> Rows()
    {
        return
        [
            new H24kTicketRow { Id = 1, Kind = "a", Score = 10 },
            new H24kTicketRow { Id = 2, Kind = "a", Score = 30 },
            new H24kTicketRow { Id = 3, Kind = "b", Score = 20 },
            new H24kTicketRow { Id = 4, Kind = "c", Score = 30 }
        ];
    }

    private static TestDatabase Setup(string methodName)
    {
        TestDatabase db = new(null, methodName);
        db.Table<H24kTicketRow>().Schema.CreateTable();
        db.Table<H24kTicketRow>().AddRange(Rows());
        return db;
    }
}
