using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using SQLite.Framework;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class GroupJoinGroupUsageAfterFlattenMessageTests
{
    private static void Seed(TestDatabase db)
    {
        db.Table<H20GaLeft>().Schema.CreateTable();
        db.Table<H20GaRight>().Schema.CreateTable();
        db.Table<H20GaLeft>().Add(new H20GaLeft { Id = 1, Key = 10 });
        db.Table<H20GaRight>().Add(new H20GaRight { Id = 1, Key = 10, RTag = "A" });
        db.Table<H20GaRight>().Add(new H20GaRight { Id = 2, Key = 10, RTag = "B" });
    }

    [Fact]
    public void GroupAggregateInWhereAfterFlattenNamesGroupJoinInMessage()
    {
        using TestDatabase db = new();
        Seed(db);

        IQueryable<int> query =
            from l in db.Table<H20GaLeft>()
            join r in db.Table<H20GaRight>() on l.Key equals r.Key into g
            from r in g
            where g.Count() > 1
            select l.Id;

        NotSupportedException ex = Assert.Throws<NotSupportedException>(() => query.ToList());
        Assert.Contains("GroupJoin", ex.Message);
        Assert.Contains("DefaultIfEmpty", ex.Message);
    }

    [Fact]
    public void GroupAggregateInWhereAfterFlattenMethodSyntaxNamesGroupJoinInMessage()
    {
        using TestDatabase db = new();
        Seed(db);

        IQueryable<int> query = db.Table<H20GaLeft>()
            .GroupJoin(db.Table<H20GaRight>(), l => l.Key, r => r.Key, (l, g) => new { l, g })
            .SelectMany(x => x.g, (x, r) => new { x.l, x.g, r })
            .Where(t => t.g.Count() > 1)
            .Select(t => t.l.Id);

        NotSupportedException ex = Assert.Throws<NotSupportedException>(() => query.ToList());
        Assert.Contains("GroupJoin", ex.Message);
        Assert.Contains("DefaultIfEmpty", ex.Message);
    }

    [Fact]
    public void SecondFlattenOfSameGroupNamesGroupJoinInMessage()
    {
        using TestDatabase db = new();
        Seed(db);

        IQueryable<int> query =
            from l in db.Table<H20GaLeft>()
            join r in db.Table<H20GaRight>() on l.Key equals r.Key into g
            from x in g
            from y in g
            select x.Id + y.Id;

        NotSupportedException ex = Assert.Throws<NotSupportedException>(() => query.ToList());
        Assert.Contains("GroupJoin", ex.Message);
        Assert.Contains("DefaultIfEmpty", ex.Message);
    }
}

[Table("H20JoinGaLefts")]
public class H20GaLeft
{
    [Key]
    public int Id { get; set; }

    public int Key { get; set; }
}

[Table("H20JoinGaRights")]
public class H20GaRight
{
    [Key]
    public int Id { get; set; }

    public int Key { get; set; }

    public required string RTag { get; set; }
}
