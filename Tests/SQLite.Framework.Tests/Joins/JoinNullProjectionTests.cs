using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("JoinOuters")]
file sealed class JoinOuter
{
    [Key]
    public int Id { get; set; }
}

[Table("JoinKeylessInners")]
public sealed class JoinKeylessInner
{
    public int? Value { get; set; }
}

[Table("JoinLefts")]
file sealed class JoinLeft
{
    [Key]
    public int Id { get; set; }

    public int Key { get; set; }

    public string Tag { get; set; } = "";
}

[Table("JoinRights")]
file sealed class JoinRight
{
    [Key]
    public int Id { get; set; }

    public int Key { get; set; }

    public string RightTag { get; set; } = "";
}

public class JoinNullProjectionTests
{
    [Fact]
    public void CrossJoinPresentRowOfAllNullColumnsMaterializesAsNull()
    {
        using TestDatabase db = new();
        db.Table<JoinOuter>().Schema.CreateTable();
        db.Table<JoinKeylessInner>().Schema.CreateTable();
        db.Table<JoinOuter>().Add(new JoinOuter { Id = 1 });
        db.Table<JoinKeylessInner>().Add(new JoinKeylessInner { Value = null });
        db.Table<JoinKeylessInner>().Add(new JoinKeylessInner { Value = 42 });

        int[] outers = [1];
        JoinKeylessInner[] inners = [new JoinKeylessInner { Value = null }, new JoinKeylessInner { Value = 42 }];
        int oracleNonNull = (from o in outers from k in inners select new { o, Inner = k }).Count(p => p.Inner != null);
        Assert.Equal(2, oracleNonNull);

        var projected = (from o in db.Table<JoinOuter>()
            from k in db.Table<JoinKeylessInner>()
            select new { o.Id, Inner = k }).ToList();
        int actualNonNull = projected.Count(p => p.Inner != null);

        Assert.Equal(1, actualNonNull);
    }

    [Fact]
    public void DefaultIfEmptyWithCustomDefaultThrows()
    {
        using TestDatabase db = new();
        db.Table<JoinLeft>().Schema.CreateTable();
        db.Table<JoinRight>().Schema.CreateTable();
        db.Table<JoinLeft>().Add(new JoinLeft { Id = 1, Key = 100, Tag = "L1" });

        JoinRight sentinel = new() { Id = -1, Key = -1, RightTag = "SENTINEL" };

        JoinLeft[] lefts = [new JoinLeft { Id = 1, Key = 100, Tag = "L1" }];
        JoinRight[] rights = [];
        var oracle = (from l in lefts
            join r in rights on l.Key equals r.Key into g
            from r in g.DefaultIfEmpty(sentinel)
            select new { l.Tag, RTag = r.RightTag, RId = r.Id }).Single();
        Assert.Equal("SENTINEL", oracle.RTag);
        Assert.Equal(-1, oracle.RId);

        Assert.Throws<NotSupportedException>(() => (from l in db.Table<JoinLeft>()
            join r in db.Table<JoinRight>() on l.Key equals r.Key into g
            from r in g.DefaultIfEmpty(sentinel)
            select new { l.Tag, RTag = r.RightTag, RId = r.Id }).Single());
    }
}
