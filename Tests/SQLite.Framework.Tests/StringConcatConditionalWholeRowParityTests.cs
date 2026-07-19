using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class StringConcatConditionalWholeRowParityTests
{
    private static List<H20SgLjParent> Parents() =>
    [
        new H20SgLjParent { Id = 1, Name = "p1" },
        new H20SgLjParent { Id = 2, Name = "p2" },
    ];

    private static List<H20SgLjChild> Children() =>
    [
        new H20SgLjChild { Id = 5, ParentId = 1, Tag = "c1" },
    ];

    private static TestDatabase Setup()
    {
        TestDatabase db = new();
        db.Table<H20SgLjParent>().Schema.CreateTable();
        db.Table<H20SgLjParent>().AddRange(Parents());
        db.Table<H20SgLjChild>().Schema.CreateTable();
        db.Table<H20SgLjChild>().AddRange(Children());
        return db;
    }

    [Fact]
    public void StringConcatWholeRowIfTrueMatchesLinq()
    {
        using TestDatabase db = Setup();
        List<H20SgLjParent> ps = Parents();
        List<H20SgLjChild> cs = Children();

        List<string> expected = (from p in ps
                join c in cs on p.Id equals c.ParentId into gc
                from c in gc.DefaultIfEmpty()
                orderby p.Id
                select string.Concat(p.Name, "|", c == null ? c : "null"))
            .ToList();

        List<string> actual = (from p in db.Table<H20SgLjParent>()
                join c in db.Table<H20SgLjChild>() on p.Id equals c.ParentId into gc
                from c in gc.DefaultIfEmpty()
                orderby p.Id
                select string.Concat(p.Name, "|", c == null ? c : "null"))
            .ToList();

        Assert.Equal(expected, actual);
    }
}
