using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using SQLite.Framework;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class GroupJoinGroupPredicateMessageTests
{
    [Fact]
    public void WhereOverGroupJoinGroupNamesGroupJoinInMessage()
    {
        using TestDatabase db = new();
        db.Table<QjmLeft>().Schema.CreateTable();
        db.Table<QjmRight>().Schema.CreateTable();
        db.Table<QjmLeft>().Add(new QjmLeft { Id = 1, Key = 10 });
        db.Table<QjmRight>().Add(new QjmRight { Id = 1, Key = 10 });

        IQueryable<int> query =
            from l in db.Table<QjmLeft>()
            join r in db.Table<QjmRight>() on l.Key equals r.Key into g
            where g.Any()
            select l.Id;

        NotSupportedException ex = Assert.Throws<NotSupportedException>(() => query.ToList());
        Assert.Contains("GroupJoin", ex.Message);
        Assert.Contains("DefaultIfEmpty", ex.Message);
    }
}

[Table("QjmLefts")]
public class QjmLeft
{
    [Key]
    public int Id { get; set; }

    public int Key { get; set; }
}

[Table("QjmRights")]
public class QjmRight
{
    [Key]
    public int Id { get; set; }

    public int Key { get; set; }
}
