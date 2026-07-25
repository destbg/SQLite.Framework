using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("PosJoinParents")]
internal sealed class PosJoinParent
{
    public PosJoinParent(int a, int b)
    {
        A = a;
        B = b;
    }

    public int A { get; init; }

    public int B { get; init; }

    [Key]
    public int Id { get; init; }
}

[Table("PosJoinOwners")]
internal sealed class PosJoinOwner
{
    [Key]
    public int Id { get; set; }

    public int ParentId { get; set; }
}

public class PositionalLeftJoinNullTests
{
    private static TestDatabase Seeded()
    {
        TestDatabase db = new();
        db.Table<PosJoinParent>().Schema.CreateTable();
        db.Table<PosJoinOwner>().Schema.CreateTable();
        db.Table<PosJoinParent>().Add(new PosJoinParent(10, 20) { Id = 1 });
        db.Table<PosJoinOwner>().Add(new PosJoinOwner { Id = 1, ParentId = 1 });
        db.Table<PosJoinOwner>().Add(new PosJoinOwner { Id = 2, ParentId = 99 });
        return db;
    }

    [Fact]
    public void LeftJoinProjectingPositionalRecordNullsOrphanLikeDotNet()
    {
        using TestDatabase db = Seeded();

        List<bool> oracle = [false, true];
        List<bool> actual = (from o in db.Table<PosJoinOwner>()
                join p in db.Table<PosJoinParent>() on o.ParentId equals p.Id into g
                from p in g.DefaultIfEmpty()
                orderby o.Id
                select new { o.Id, Pos = p })
            .ToList()
            .Select(x => x.Pos == null)
            .ToList();

        Assert.Equal([false, true], oracle);
        Assert.Equal(oracle, actual);
    }

    [Fact]
    public void LeftJoinProjectingPositionalRecordMaterializesMatchedValuesAndExtraProperty()
    {
        using TestDatabase db = Seeded();

        var matched = (from o in db.Table<PosJoinOwner>()
                join p in db.Table<PosJoinParent>() on o.ParentId equals p.Id into g
                from p in g.DefaultIfEmpty()
                orderby o.Id
                select new { o.Id, Pos = p })
            .ToList()
            .First(x => x.Id == 1)
            .Pos;

        Assert.Equal(10, matched.A);
        Assert.Equal(20, matched.B);
        Assert.Equal(1, matched.Id);
    }
}
