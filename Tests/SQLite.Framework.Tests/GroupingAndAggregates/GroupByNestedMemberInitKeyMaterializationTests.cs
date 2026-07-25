using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("gbnk_rows")]
public class GbnkRow
{
    [Key]
    public int Id { get; set; }

    public string? Note { get; set; }

    public int? Amount { get; set; }
}

public class GbnkPart
{
    public string? Note { get; set; }

    public int? Amount { get; set; }
}

public class GroupByNestedMemberInitKeyMaterializationTests
{
    private static List<GbnkRow> RowData()
    {
        return
        [
            new GbnkRow { Id = 1, Note = null, Amount = null },
            new GbnkRow { Id = 2, Note = "x", Amount = 5 },
            new GbnkRow { Id = 3, Note = "x", Amount = 5 },
        ];
    }

    private static TestDatabase Seed()
    {
        TestDatabase db = new();
        db.Table<GbnkRow>().Schema.CreateTable();
        db.Table<GbnkRow>().AddRange(RowData());
        return db;
    }

    [Fact]
    public void WholeKeyWithNestedMemberInitPartMaterializes()
    {
        using TestDatabase db = Seed();

        List<(bool, string, int)> expected = RowData()
            .GroupBy(r => (r.Note, r.Amount))
            .Select(g => (true, g.Key.Note ?? "<n>", g.Count()))
            .OrderBy(x => x.Item2)
            .ToList();

        List<(bool, string, int)> actual = db.Table<GbnkRow>()
            .GroupBy(r => new { W = new GbnkPart { Note = r.Note, Amount = r.Amount } })
            .Select(g => new { g.Key, C = g.Count() })
            .AsEnumerable()
            .Select(x => (x.Key.W != null, x.Key.W == null ? "<n>" : x.Key.W.Note ?? "<n>", x.C))
            .OrderBy(x => x.Item2)
            .ToList();

        Assert.Equal(expected, actual);
    }
}
