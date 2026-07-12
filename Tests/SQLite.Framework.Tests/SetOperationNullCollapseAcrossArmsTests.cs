using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("sonc_rows")]
public class SoncRow
{
    [Key]
    public int Id { get; set; }

    public string? Note { get; set; }
}

[Table("sonc_opt")]
public class SoncOpt
{
    [Key]
    public int Id { get; set; }

    public string? Name { get; set; }
}

public class SoncMix
{
    public int Tag { get; set; }

    public SoncOpt? Entity { get; set; }
}

public class SetOperationNullCollapseAcrossArmsTests
{
    private static List<SoncRow> RowData()
    {
        return
        [
            new SoncRow { Id = 1, Note = null },
            new SoncRow { Id = 2, Note = "x" },
            new SoncRow { Id = 3, Note = "y" },
        ];
    }

    private static List<SoncOpt> OptData()
    {
        return [new SoncOpt { Id = 2, Name = "n2" }];
    }

    private static TestDatabase Seed()
    {
        TestDatabase db = new();
        db.Table<SoncRow>().Schema.CreateTable();
        db.Table<SoncRow>().AddRange(RowData());
        db.Table<SoncOpt>().Schema.CreateTable();
        db.Table<SoncOpt>().AddRange(OptData());
        return db;
    }

    [Fact]
    public void EntityArmOrphanStaysNullAfterConstructedFirstArm()
    {
        using TestDatabase db = Seed();

        List<SoncRow> rows = RowData();
        List<SoncOpt> opts = OptData();

        List<(int, string)> expected = rows
            .Select(r => new SoncMix { Tag = r.Id, Entity = new SoncOpt { Id = r.Id, Name = r.Note } })
            .Concat(from r in rows
                    join o in opts on r.Id equals o.Id into g
                    from o in g.DefaultIfEmpty()
                    select new SoncMix { Tag = r.Id + 10, Entity = o })
            .OrderBy(m => m.Tag)
            .Select(m => (m.Tag, m.Entity == null ? "null" : m.Entity.Id + ":" + (m.Entity.Name ?? "<n>")))
            .ToList();

        Assert.Equal(
        [
            (1, "1:<n>"), (2, "2:x"), (3, "3:y"),
            (11, "null"), (12, "2:n2"), (13, "null"),
        ], expected);

        List<(int, string)> actual = db.Table<SoncRow>()
            .Select(r => new SoncMix { Tag = r.Id, Entity = new SoncOpt { Id = r.Id, Name = r.Note } })
            .Concat(from r in db.Table<SoncRow>()
                    join o in db.Table<SoncOpt>() on r.Id equals o.Id into g
                    from o in g.DefaultIfEmpty()
                    select new SoncMix { Tag = r.Id + 10, Entity = o })
            .AsEnumerable()
            .OrderBy(m => m.Tag)
            .Select(m => (m.Tag, m.Entity == null ? "null" : m.Entity.Id + ":" + (m.Entity.Name ?? "<n>")))
            .ToList();

        Assert.Equal(expected, actual);
    }
}
