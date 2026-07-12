using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("ctn_parent")]
public class CtnParent
{
    [Key]
    public int Id { get; set; }

    public string Name { get; set; } = "";
}

[Table("ctn_child")]
public class CtnChild
{
    [Key]
    public int Id { get; set; }

    public int ParentId { get; set; }

    public string? Title { get; set; }
}

public class CtnPair
{
    public string? Title { get; set; }

    public int? Amount { get; set; }
}

public class CtnWrap
{
    public int Id { get; set; }

    public CtnPair? W { get; set; }
}

public class CteNestedConstructedDtoNullMembersTests
{
    private static List<CtnParent> Parents()
    {
        return
        [
            new CtnParent { Id = 1, Name = "Ann" },
            new CtnParent { Id = 2, Name = "Bob" },
            new CtnParent { Id = 3, Name = "Cid" },
        ];
    }

    private static List<CtnChild> Children()
    {
        return
        [
            new CtnChild { Id = 10, ParentId = 1, Title = "t1" },
            new CtnChild { Id = 11, ParentId = 3, Title = null },
        ];
    }

    [Fact]
    public void ConstructedDtoWithAllNullMembersStaysBuiltThroughCte()
    {
        using TestDatabase db = new();
        db.Table<CtnParent>().Schema.CreateTable();
        db.Table<CtnParent>().AddRange(Parents());
        db.Table<CtnChild>().Schema.CreateTable();
        db.Table<CtnChild>().AddRange(Children());
        List<CtnParent> ps = Parents();
        List<CtnChild> cs = Children();

        List<string> expected = (from p in ps
            join c in cs on p.Id equals c.ParentId into g
            from c in g.DefaultIfEmpty()
            select new CtnWrap { Id = p.Id, W = new CtnPair { Title = c != null ? c.Title : null, Amount = c != null ? (int?)c.Id : null } })
            .OrderBy(x => x.Id)
            .Select(x => x.Id + "|" + (x.W == null ? "nullW" : (x.W.Title ?? "-") + ":" + (x.W.Amount?.ToString() ?? "-")))
            .ToList();

        SQLiteCte<CtnWrap> cte = db.With(() =>
            from p in db.Table<CtnParent>()
            join c in db.Table<CtnChild>() on p.Id equals c.ParentId into g
            from c in g.DefaultIfEmpty()
            select new CtnWrap { Id = p.Id, W = new CtnPair { Title = c != null ? c.Title : null, Amount = c != null ? (int?)c.Id : null } });

        List<string> actual = cte
            .AsEnumerable()
            .OrderBy(x => x.Id)
            .Select(x => x.Id + "|" + (x.W == null ? "nullW" : (x.W.Title ?? "-") + ":" + (x.W.Amount?.ToString() ?? "-")))
            .ToList();

        Assert.Equal(expected, actual);
    }
}
