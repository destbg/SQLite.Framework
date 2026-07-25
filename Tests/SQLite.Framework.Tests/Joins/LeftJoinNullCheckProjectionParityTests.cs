using System.Collections.Generic;
using System.Linq;
using System.ComponentModel.DataAnnotations;
using SQLite.Framework;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class LeftJoinNullCheckLeft
{
    [Key] public int Id { get; set; }
    public string Name { get; set; } = "";
    public int? BId { get; set; }
}

public class LeftJoinNullCheckRight
{
    [Key] public int Id { get; set; }
    public string Tag { get; set; } = "";
}

public class LeftJoinNullCheckProjectionParityTests
{
    private static string Munge(string s) => s.ToUpperInvariant();

    [Fact]
    public void LeftJoinNullChecks_InClientProjection_MatchLinqToObjects()
    {
        using TestDatabase db = new();
        db.Table<LeftJoinNullCheckLeft>().Schema.CreateTable();
        db.Table<LeftJoinNullCheckRight>().Schema.CreateTable();
        List<LeftJoinNullCheckLeft> lefts = new()
        {
            new() { Id = 1, Name = "a", BId = 10 },
            new() { Id = 2, Name = "b", BId = null },
            new() { Id = 3, Name = "c", BId = 99 },
        };
        List<LeftJoinNullCheckRight> rights = new() { new() { Id = 10, Tag = "t10" } };
        foreach (LeftJoinNullCheckLeft l in lefts)
        {
            db.Table<LeftJoinNullCheckLeft>().Add(l);
        }

        foreach (LeftJoinNullCheckRight r in rights)
        {
            db.Table<LeftJoinNullCheckRight>().Add(r);
        }

        var expected = (from l in lefts
                        join r in rights on l.BId equals r.Id into g
                        from r in g.DefaultIfEmpty()
                        orderby l.Id
                        select new { l.Id, IsNull = r == null, NotNull = r != null, Tag = Munge(r == null ? "miss" : r.Tag) }).ToList();
        var actual = (from l in db.Table<LeftJoinNullCheckLeft>()
                      join r in db.Table<LeftJoinNullCheckRight>() on l.BId equals r.Id into g
                      from r in g.DefaultIfEmpty()
                      orderby l.Id
                      select new { l.Id, IsNull = r == null, NotNull = r != null, Tag = Munge(r == null ? "miss" : r.Tag) }).ToList();

        Assert.Equal(expected, actual);
    }
}
