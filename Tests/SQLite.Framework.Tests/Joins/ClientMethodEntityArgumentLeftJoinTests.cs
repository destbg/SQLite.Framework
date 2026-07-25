using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("cme_parent")]
public class CmeParent
{
    [Key]
    public int Id { get; set; }

    public string Name { get; set; } = "";
}

[Table("cme_child")]
public class CmeChild
{
    [Key]
    public int Id { get; set; }

    public int ParentId { get; set; }

    public string? Title { get; set; }
}

[Table("cme_tchild")]
public class CmeTagChild
{
    [Key]
    [Column("k_id")]
    public int Id { get; set; }

    [Column("p_ref")]
    public int ParentId { get; set; }

    [Column("t_txt")]
    public string? Title { get; set; }
}

[Table("cme_solo")]
public class CmeSolo
{
    [Key]
    public int Id { get; set; }
}

[Table("cme_dchild")]
public class CmeDateChild
{
    [Key]
    public int Id { get; set; }

    public int ParentId { get; set; }

    public DateTime When { get; set; }
}

public class ClientMethodEntityArgumentLeftJoinTests
{
    public static string DescribeChild(CmeChild? c)
    {
        return c == null ? "none" : c.Id + ":" + (c.Title ?? "<null>");
    }

    public static string DescribeTag(CmeTagChild? c)
    {
        return c == null ? "none" : c.Id + ":" + (c.Title ?? "<null>");
    }

    public static string DescribeSolo(CmeSolo? s)
    {
        return s == null ? "none" : "solo" + s.Id;
    }

    public static string DescribeDate(CmeDateChild? c)
    {
        return c == null ? "none" : c.Id + "@" + c.When.Ticks;
    }

    private static List<CmeParent> Parents()
    {
        return
        [
            new CmeParent { Id = 1, Name = "Ann" },
            new CmeParent { Id = 2, Name = "Bob" },
            new CmeParent { Id = 3, Name = "Cid" },
        ];
    }

    private static List<CmeChild> Children()
    {
        return
        [
            new CmeChild { Id = 10, ParentId = 1, Title = "t1" },
            new CmeChild { Id = 11, ParentId = 3, Title = null },
        ];
    }

    private static TestDatabase Seed()
    {
        TestDatabase db = new();
        db.Table<CmeParent>().Schema.CreateTable();
        db.Table<CmeParent>().AddRange(Parents());
        db.Table<CmeChild>().Schema.CreateTable();
        db.Table<CmeChild>().AddRange(Children());
        return db;
    }

    [Fact]
    public void EntityArgumentIsNullForMissingRow()
    {
        using TestDatabase db = Seed();
        List<CmeParent> ps = Parents();
        List<CmeChild> cs = Children();

        List<string> expected = (from p in ps
            join c in cs on p.Id equals c.ParentId into g
            from c in g.DefaultIfEmpty()
            select new { p.Id, D = DescribeChild(c) })
            .OrderBy(x => x.Id)
            .Select(x => x.Id + "|" + x.D)
            .ToList();

        List<string> actual = (from p in db.Table<CmeParent>()
            join c in db.Table<CmeChild>() on p.Id equals c.ParentId into g
            from c in g.DefaultIfEmpty()
            select new { p.Id, D = DescribeChild(c) })
            .AsEnumerable()
            .OrderBy(x => x.Id)
            .Select(x => x.Id + "|" + x.D)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void EntityArgumentUsedTwiceIsNullForMissingRow()
    {
        using TestDatabase db = Seed();
        List<CmeParent> ps = Parents();
        List<CmeChild> cs = Children();

        List<string> expected = (from p in ps
            join c in cs on p.Id equals c.ParentId into g
            from c in g.DefaultIfEmpty()
            select new { p.Id, D = DescribeChild(c) + "/" + DescribeChild(c) })
            .OrderBy(x => x.Id)
            .Select(x => x.Id + "|" + x.D)
            .ToList();

        List<string> actual = (from p in db.Table<CmeParent>()
            join c in db.Table<CmeChild>() on p.Id equals c.ParentId into g
            from c in g.DefaultIfEmpty()
            select new { p.Id, D = DescribeChild(c) + "/" + DescribeChild(c) })
            .AsEnumerable()
            .OrderBy(x => x.Id)
            .Select(x => x.Id + "|" + x.D)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void EntityArgumentWithRenamedColumnsIsNullForMissingRow()
    {
        using TestDatabase db = Seed();
        db.Table<CmeTagChild>().Schema.CreateTable();
        db.Table<CmeTagChild>().Add(new CmeTagChild { Id = 20, ParentId = 1, Title = "r1" });
        db.Table<CmeTagChild>().Add(new CmeTagChild { Id = 21, ParentId = 3, Title = null });
        List<CmeParent> ps = Parents();
        List<CmeTagChild> ts =
        [
            new CmeTagChild { Id = 20, ParentId = 1, Title = "r1" },
            new CmeTagChild { Id = 21, ParentId = 3, Title = null },
        ];

        List<string> expected = (from p in ps
            join c in ts on p.Id equals c.ParentId into g
            from c in g.DefaultIfEmpty()
            select new { p.Id, D = DescribeTag(c) })
            .OrderBy(x => x.Id)
            .Select(x => x.Id + "|" + x.D)
            .ToList();

        List<string> actual = (from p in db.Table<CmeParent>()
            join c in db.Table<CmeTagChild>() on p.Id equals c.ParentId into g
            from c in g.DefaultIfEmpty()
            select new { p.Id, D = DescribeTag(c) })
            .AsEnumerable()
            .OrderBy(x => x.Id)
            .Select(x => x.Id + "|" + x.D)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void SingleColumnEntityArgumentIsNullForMissingRow()
    {
        using TestDatabase db = Seed();
        db.Table<CmeSolo>().Schema.CreateTable();
        db.Table<CmeSolo>().Add(new CmeSolo { Id = 1 });
        List<CmeParent> ps = Parents();
        List<CmeSolo> ss = [new CmeSolo { Id = 1 }];

        List<string> expected = (from p in ps
            join s in ss on p.Id equals s.Id into g
            from s in g.DefaultIfEmpty()
            select new { p.Id, D = DescribeSolo(s) })
            .OrderBy(x => x.Id)
            .Select(x => x.Id + "|" + x.D)
            .ToList();

        List<string> actual = (from p in db.Table<CmeParent>()
            join s in db.Table<CmeSolo>() on p.Id equals s.Id into g
            from s in g.DefaultIfEmpty()
            select new { p.Id, D = DescribeSolo(s) })
            .AsEnumerable()
            .OrderBy(x => x.Id)
            .Select(x => x.Id + "|" + x.D)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void EntityArgumentWithDateColumnIsNullForMissingRow()
    {
        using TestDatabase db = Seed();
        db.Table<CmeDateChild>().Schema.CreateTable();
        CmeDateChild d1 = new() { Id = 30, ParentId = 1, When = new DateTime(2024, 3, 5, 7, 9, 11) };
        db.Table<CmeDateChild>().Add(d1);
        List<CmeParent> ps = Parents();
        List<CmeDateChild> ds = [d1];

        List<string> expected = (from p in ps
            join c in ds on p.Id equals c.ParentId into g
            from c in g.DefaultIfEmpty()
            select new { p.Id, D = DescribeDate(c) })
            .OrderBy(x => x.Id)
            .Select(x => x.Id + "|" + x.D)
            .ToList();

        List<string> actual = (from p in db.Table<CmeParent>()
            join c in db.Table<CmeDateChild>() on p.Id equals c.ParentId into g
            from c in g.DefaultIfEmpty()
            select new { p.Id, D = DescribeDate(c) })
            .AsEnumerable()
            .OrderBy(x => x.Id)
            .Select(x => x.Id + "|" + x.D)
            .ToList();

        Assert.Equal(expected, actual);
    }
}
