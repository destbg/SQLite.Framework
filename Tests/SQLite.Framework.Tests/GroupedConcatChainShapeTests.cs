using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("KindWordEntry")]
public class KindWordEntryRow
{
    [Key]
    public int Id { get; set; }

    public int Kind { get; set; }

    public string Name { get; set; } = "";
}

public class GroupedConcatChainShapeTests
{
    [Fact]
    public void JoinOverADistinctGroupChainBehavesLikeLinq()
    {
        using TestDatabase db = new();
        db.Table<KindWordEntryRow>().Schema.CreateTable();
        List<KindWordEntryRow> rows =
        [
            new KindWordEntryRow { Id = 1, Kind = 1, Name = "a" },
            new KindWordEntryRow { Id = 2, Kind = 1, Name = "a" },
            new KindWordEntryRow { Id = 3, Kind = 1, Name = "b" },
            new KindWordEntryRow { Id = 4, Kind = 2, Name = "c" },
        ];
        db.Table<KindWordEntryRow>().AddRange(rows);

        var expected = rows.GroupBy(x => x.Kind)
            .Select(g => new { K = g.Key, S = string.Join(",", g.Select(x => x.Name).Distinct()) })
            .OrderBy(x => x.K).ToList();
        var actual = db.Table<KindWordEntryRow>().GroupBy(x => x.Kind)
            .Select(g => new { K = g.Key, S = string.Join(",", g.Select(x => x.Name).Distinct()) })
            .ToList().OrderBy(x => x.K).ToList();

        Assert.Equal(expected, actual);
    }
}
