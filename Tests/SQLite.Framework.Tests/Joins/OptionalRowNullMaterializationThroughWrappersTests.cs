using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using SQLite.Framework;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H24qWrapOwners")]
public class H24qWrapOwner
{
    [Key]
    public int Id { get; set; }

    public string Name { get; set; } = "";
}

[Table("H24qWrapNotes")]
public class H24qWrapNote
{
    [Key]
    public int Id { get; set; }

    public int OwnerId { get; set; }

    public string? Label { get; set; }
}

public class OptionalRowNullMaterializationThroughWrappersTests
{
    [Fact]
    public void MissingLeftJoinRowReadsBackAsNullThroughACommonTableExpression()
    {
        using TestDatabase db = Setup(nameof(MissingLeftJoinRowReadsBackAsNullThroughACommonTableExpression));

        int expected = Owners()
            .GroupJoin(Notes(), o => o.Id, n => n.OwnerId, (o, g) => new { o, g })
            .SelectMany(t => t.g.DefaultIfEmpty(), (t, n) => n)
            .Count(n => n == null);

        SQLiteCte<H24qWrapNote> cte = db.With(() =>
            from o in db.Table<H24qWrapOwner>()
            join n in db.Table<H24qWrapNote>() on o.Id equals n.OwnerId into g
            from n in g.DefaultIfEmpty()
            select n);

        int actual = cte.AsEnumerable().Count(n => n == null);

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void MissingLeftJoinRowReadsBackAsNullOnTheSecondSideOfConcat()
    {
        using TestDatabase db = Setup(nameof(MissingLeftJoinRowReadsBackAsNullOnTheSecondSideOfConcat));

        int expected = Notes()
            .Where(n => n.Id == 10)
            .Concat(
                Owners()
                    .GroupJoin(Notes(), o => o.Id, n => n.OwnerId, (o, g) => new { o, g })
                    .SelectMany(t => t.g.DefaultIfEmpty(), (t, n) => n))
            .Count(n => n == null);

        IQueryable<H24qWrapNote> optional =
            from o in db.Table<H24qWrapOwner>()
            join n in db.Table<H24qWrapNote>() on o.Id equals n.OwnerId into g
            from n in g.DefaultIfEmpty()
            select n;

        int actual = db.Table<H24qWrapNote>()
            .Where(n => n.Id == 10)
            .Concat(optional)
            .AsEnumerable()
            .Count(n => n == null);

        Assert.Equal(expected, actual);
    }

    private static List<H24qWrapOwner> Owners()
    {
        return
        [
            new H24qWrapOwner { Id = 1, Name = "Ann" },
            new H24qWrapOwner { Id = 2, Name = "Bob" },
            new H24qWrapOwner { Id = 3, Name = "Cid" }
        ];
    }

    private static List<H24qWrapNote> Notes()
    {
        return
        [
            new H24qWrapNote { Id = 10, OwnerId = 1, Label = "alpha" },
            new H24qWrapNote { Id = 11, OwnerId = 3, Label = "gamma" }
        ];
    }

    private static TestDatabase Setup(string methodName)
    {
        TestDatabase db = new(null, methodName);
        db.Table<H24qWrapOwner>().Schema.CreateTable();
        db.Table<H24qWrapNote>().Schema.CreateTable();
        db.Table<H24qWrapOwner>().AddRange(Owners());
        db.Table<H24qWrapNote>().AddRange(Notes());
        return db;
    }
}
