using SQLite.Framework;
using SQLite.Framework.Attributes;
using SQLite.Framework.Enums;
using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Helpers;
using SQLite.Framework.Tests.Entities;
using System.Globalization;

namespace SQLite.Framework.Tests;

public class LeftJoinEntityNullCheckProjectionBugTests
{
    [Fact]
    public void LeftJoinEntityNullCheckUsesNullableColumnProjection()
    {
        using TestDatabase db = new();
        db.Table<JoinOrderProjLeftRow>().Schema.CreateTable();
        db.Table<JoinOrderProjRightRow>().Schema.CreateTable();
        db.Table<JoinOrderProjLeftRow>().Add(new JoinOrderProjLeftRow { Id = 1, Fk = 100, Label = "matched" });
        db.Table<JoinOrderProjLeftRow>().Add(new JoinOrderProjLeftRow { Id = 2, Fk = 200, Label = "orphan" });
        db.Table<JoinOrderProjRightRow>().Add(new JoinOrderProjRightRow { Marker = 1, Fk = 100, X = null });
        List<bool> actual = (from l in db.Table<JoinOrderProjLeftRow>()
            join r in db.Table<JoinOrderProjRightRow>() on l.Fk equals r.Fk into g
            from r in g.DefaultIfEmpty()
            orderby l.Id
            select r != null).ToList();
        List<JoinOrderProjLeftRow> lefts =
        [
            new JoinOrderProjLeftRow { Id = 1, Fk = 100, Label = "matched" },
            new JoinOrderProjLeftRow { Id = 2, Fk = 200, Label = "orphan" }
        ];
        List<JoinOrderProjRightRow> rights = [new JoinOrderProjRightRow { Marker = 1, Fk = 100, X = null }];
        List<bool> oracle = (from l in lefts
            join r in rights on l.Fk equals r.Fk into g
            from r in g.DefaultIfEmpty()
            orderby l.Id
            select r != null).ToList();
        Assert.Equal(oracle, actual);
    }
}

public class JoinOrderProjLeftRow { [System.ComponentModel.DataAnnotations.Key] public int Id { get; set; } public int Fk { get; set; } public string Label { get; set; } = ""; }

public class JoinOrderProjRightRow { [System.ComponentModel.DataAnnotations.Key] public int Marker { get; set; } public int Fk { get; set; } public int? X { get; set; } }
