using SQLite.Framework;
using SQLite.Framework.Attributes;
using SQLite.Framework.Enums;
using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Helpers;
using SQLite.Framework.Tests.Entities;
using System.Globalization;

namespace SQLite.Framework.Tests;

public class LeftJoinEntityNotNullFilterBugTests
{
    [Fact]
    public void LeftJoinWhereEntityNotNullDropsMatchedRowWithNullColumn()
    {
        using TestDatabase db = new();
        db.Table<JoinOrderWhereLeftRow>().Schema.CreateTable();
        db.Table<JoinOrderWhereRightRow>().Schema.CreateTable();
        db.Table<JoinOrderWhereLeftRow>().Add(new JoinOrderWhereLeftRow { Id = 1, Fk = 100 });
        db.Table<JoinOrderWhereLeftRow>().Add(new JoinOrderWhereLeftRow { Id = 2, Fk = 200 });
        db.Table<JoinOrderWhereRightRow>().Add(new JoinOrderWhereRightRow { Marker = 1, Fk = 100, X = null });
        List<int> actual = (from l in db.Table<JoinOrderWhereLeftRow>()
            join r in db.Table<JoinOrderWhereRightRow>() on l.Fk equals r.Fk into g
            from r in g.DefaultIfEmpty()
            where r != null
            orderby l.Id
            select l.Id).ToList();
        List<JoinOrderWhereLeftRow> lefts = [new JoinOrderWhereLeftRow { Id = 1, Fk = 100 }, new JoinOrderWhereLeftRow { Id = 2, Fk = 200 }];
        List<JoinOrderWhereRightRow> rights = [new JoinOrderWhereRightRow { Marker = 1, Fk = 100, X = null }];
        List<int> oracle = (from l in lefts
            join r in rights on l.Fk equals r.Fk into g
            from r in g.DefaultIfEmpty()
            where r != null
            orderby l.Id
            select l.Id).ToList();
        Assert.Equal(oracle, actual);
    }
}

public class JoinOrderWhereLeftRow { [System.ComponentModel.DataAnnotations.Key] public int Id { get; set; } public int Fk { get; set; } }

public class JoinOrderWhereRightRow { [System.ComponentModel.DataAnnotations.Key] public int Marker { get; set; } public int Fk { get; set; } public int? X { get; set; } }
