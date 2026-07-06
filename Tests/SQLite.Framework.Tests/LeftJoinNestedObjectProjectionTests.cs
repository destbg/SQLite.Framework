using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("LjNestedAuthors")]
public class LjNestedAuthor
{
    [Key]
    public int Id { get; set; }

    public string Name { get; set; } = "";
}

[Table("LjNestedBooks")]
public class LjNestedBook
{
    [Key]
    public int Id { get; set; }

    public int AuthorId { get; set; }

    public string Title { get; set; } = "";
}

public class LeftJoinNestedObjectProjectionTests
{
    private static List<LjNestedAuthor> Authors()
    {
        return
        [
            new LjNestedAuthor { Id = 1, Name = "Ann" },
            new LjNestedAuthor { Id = 2, Name = "Bob" },
        ];
    }

    private static List<LjNestedBook> Books()
    {
        return
        [
            new LjNestedBook { Id = 1, AuthorId = 1, Title = "T1" },
        ];
    }

    private static TestDatabase Seed()
    {
        TestDatabase db = new();
        db.Table<LjNestedAuthor>().Schema.CreateTable();
        db.Table<LjNestedAuthor>().AddRange(Authors());
        db.Table<LjNestedBook>().Schema.CreateTable();
        db.Table<LjNestedBook>().AddRange(Books());
        return db;
    }

    [Fact]
    public void NestedObjectAroundOuterColumns()
    {
        using TestDatabase db = Seed();
        List<LjNestedAuthor> authors = Authors();
        List<LjNestedBook> books = Books();

        var expected = (from a in authors
                        join b in books on a.Id equals b.AuthorId into grp
                        from b in grp.DefaultIfEmpty()
                        select new { Outer = new { a.Id, a.Name }, Title = b == null ? "none" : b.Title })
            .OrderBy(x => x.Outer.Id).ToList();
        var actual = (from a in db.Table<LjNestedAuthor>()
                      join b in db.Table<LjNestedBook>() on a.Id equals b.AuthorId into grp
                      from b in grp.DefaultIfEmpty()
                      select new { Outer = new { a.Id, a.Name }, Title = b == null ? "none" : b.Title })
            .ToList().OrderBy(x => x.Outer.Id).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void NestedObjectAroundInnerColumns()
    {
        using TestDatabase db = Seed();
        List<LjNestedAuthor> authors = Authors();
        List<LjNestedBook> books = Books();

        var expected = (from a in authors
                        join b in books on a.Id equals b.AuthorId into grp
                        from b in grp.DefaultIfEmpty()
                        select new { a.Id, Inner = new { Title = b == null ? "none" : b.Title } })
            .OrderBy(x => x.Id).ToList();
        var actual = (from a in db.Table<LjNestedAuthor>()
                      join b in db.Table<LjNestedBook>() on a.Id equals b.AuthorId into grp
                      from b in grp.DefaultIfEmpty()
                      select new { a.Id, Inner = new { Title = b == null ? "none" : b.Title } })
            .ToList().OrderBy(x => x.Id).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void NestedObjectsAroundBothSides()
    {
        using TestDatabase db = Seed();
        List<LjNestedAuthor> authors = Authors();
        List<LjNestedBook> books = Books();

        var expected = (from a in authors
                        join b in books on a.Id equals b.AuthorId into grp
                        from b in grp.DefaultIfEmpty()
                        select new
                        {
                            Outer = new { a.Id, a.Name },
                            Inner = new { Title = b == null ? "none" : b.Title },
                        })
            .OrderBy(x => x.Outer.Id).ToList();
        var actual = (from a in db.Table<LjNestedAuthor>()
                      join b in db.Table<LjNestedBook>() on a.Id equals b.AuthorId into grp
                      from b in grp.DefaultIfEmpty()
                      select new
                      {
                          Outer = new { a.Id, a.Name },
                          Inner = new { Title = b == null ? "none" : b.Title },
                      })
            .ToList().OrderBy(x => x.Outer.Id).ToList();

        Assert.Equal(expected, actual);
    }
}
