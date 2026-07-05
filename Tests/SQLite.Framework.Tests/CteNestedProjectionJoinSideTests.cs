using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("CteJoinBook")]
public class CteJoinBookRow
{
    [Key]
    public int Id { get; set; }

    public int Price { get; set; }
}

[Table("CteJoinAuthor")]
public class CteJoinAuthorRow
{
    [Key]
    public int Id { get; set; }

    public string Name { get; set; } = "";
}

public class CteNestedProjectionJoinSideTests
{
    [Fact]
    public void CteWithNestedProjectionAsJoinSide()
    {
        using TestDatabase db = new();
        db.Table<CteJoinBookRow>().Schema.CreateTable();
        db.Table<CteJoinAuthorRow>().Schema.CreateTable();
        db.Table<CteJoinAuthorRow>().Add(new CteJoinAuthorRow { Id = 1, Name = "a1" });
        db.Table<CteJoinBookRow>().Add(new CteJoinBookRow { Id = 1, Price = 10 });

        var cte = db.With(() => db.Table<CteJoinBookRow>().Select(b => new { b.Id, Inner = new { b.Price } }));

        var actual = (from a in db.Table<CteJoinAuthorRow>()
                      join x in cte on a.Id equals x.Id
                      select new { a.Name, x.Inner.Price }).ToList();

        Assert.Equal([("a1", 10)], actual.Select(r => (r.Name, r.Price)).ToArray());
    }
}
