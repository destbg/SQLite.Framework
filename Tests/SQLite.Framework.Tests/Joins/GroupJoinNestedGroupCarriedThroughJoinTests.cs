using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using SQLite.Framework;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H22jNestedOuters")]
public class H22jNestedOuter
{
    [Key]
    public int Id { get; set; }

    public int Key { get; set; }

    public string Name { get; set; } = "";
}

[Table("H22jNestedItems")]
public class H22jNestedItem
{
    [Key]
    public int Id { get; set; }

    public int Key { get; set; }
}

[Table("H22jNestedSides")]
public class H22jNestedSide
{
    [Key]
    public int Id { get; set; }

    public int Key { get; set; }
}

public class GroupJoinNestedGroupCarriedThroughJoinTests
{
    [Fact]
    public void NestedGroupCarriedThroughAnInnerJoinStillLeftFlattens()
    {
        using TestDatabase db = new();
        (List<H22jNestedOuter> os, List<H22jNestedItem> items, List<H22jNestedSide> sides) = Seed(db);

        var expected = os
            .GroupJoin(items, o => o.Key, i => i.Key, (o, g) => new { o, Box = new { Group = g } })
            .Join(sides, x => x.o.Key, s => s.Key, (x, s) => new { x.o, x.Box, s })
            .SelectMany(y => y.Box.Group.DefaultIfEmpty(), (y, i) => new
            {
                OId = y.o.Id,
                SId = y.s.Id,
                IId = i == null ? -1 : i.Id
            })
            .OrderBy(t => t.OId)
            .ThenBy(t => t.SId)
            .ThenBy(t => t.IId)
            .ToList();

        var actual = db.Table<H22jNestedOuter>()
            .GroupJoin(db.Table<H22jNestedItem>(), o => o.Key, i => i.Key, (o, g) => new { o, Box = new { Group = g } })
            .Join(db.Table<H22jNestedSide>(), x => x.o.Key, s => s.Key, (x, s) => new { x.o, x.Box, s })
            .SelectMany(y => y.Box.Group.DefaultIfEmpty(), (y, i) => new
            {
                OId = y.o.Id,
                SId = y.s.Id,
                IId = i == null ? -1 : i.Id
            })
            .ToList()
            .OrderBy(t => t.OId)
            .ThenBy(t => t.SId)
            .ThenBy(t => t.IId)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void NestedGroupCarriedThroughASecondGroupJoinStillLeftFlattens()
    {
        using TestDatabase db = new();
        (List<H22jNestedOuter> os, List<H22jNestedItem> items, List<H22jNestedSide> sides) = Seed(db);

        var expected = os
            .GroupJoin(items, o => o.Key, i => i.Key, (o, g) => new { o, Box = new { Group = g } })
            .GroupJoin(sides, x => x.o.Key, s => s.Key, (x, sg) => new { x.o, x.Box, sg })
            .SelectMany(y => y.sg.DefaultIfEmpty(), (y, s) => new { y.o, y.Box, s })
            .SelectMany(y => y.Box.Group.DefaultIfEmpty(), (y, i) => new
            {
                OId = y.o.Id,
                SId = y.s == null ? -1 : y.s.Id,
                IId = i == null ? -1 : i.Id
            })
            .OrderBy(t => t.OId)
            .ThenBy(t => t.SId)
            .ThenBy(t => t.IId)
            .ToList();

        var actual = db.Table<H22jNestedOuter>()
            .GroupJoin(db.Table<H22jNestedItem>(), o => o.Key, i => i.Key, (o, g) => new { o, Box = new { Group = g } })
            .GroupJoin(db.Table<H22jNestedSide>(), x => x.o.Key, s => s.Key, (x, sg) => new { x.o, x.Box, sg })
            .SelectMany(y => y.sg.DefaultIfEmpty(), (y, s) => new { y.o, y.Box, s })
            .SelectMany(y => y.Box.Group.DefaultIfEmpty(), (y, i) => new
            {
                OId = y.o.Id,
                SId = y.s == null ? -1 : y.s.Id,
                IId = i == null ? -1 : i.Id
            })
            .ToList()
            .OrderBy(t => t.OId)
            .ThenBy(t => t.SId)
            .ThenBy(t => t.IId)
            .ToList();

        Assert.Equal(expected, actual);
    }

    private static (List<H22jNestedOuter> Outers, List<H22jNestedItem> Items, List<H22jNestedSide> Sides) Seed(TestDatabase db)
    {
        db.Table<H22jNestedOuter>().Schema.CreateTable();
        db.Table<H22jNestedItem>().Schema.CreateTable();
        db.Table<H22jNestedSide>().Schema.CreateTable();

        List<H22jNestedOuter> os =
        [
            new H22jNestedOuter { Id = 1, Key = 10, Name = "a" },
            new H22jNestedOuter { Id = 2, Key = 20, Name = "b" },
            new H22jNestedOuter { Id = 3, Key = 30, Name = "c" }
        ];
        List<H22jNestedItem> items =
        [
            new H22jNestedItem { Id = 1, Key = 10 },
            new H22jNestedItem { Id = 2, Key = 10 },
            new H22jNestedItem { Id = 3, Key = 30 }
        ];
        List<H22jNestedSide> sides =
        [
            new H22jNestedSide { Id = 1, Key = 10 },
            new H22jNestedSide { Id = 2, Key = 20 },
            new H22jNestedSide { Id = 3, Key = 30 }
        ];

        db.Table<H22jNestedOuter>().AddRange(os);
        db.Table<H22jNestedItem>().AddRange(items);
        db.Table<H22jNestedSide>().AddRange(sides);
        return (os, items, sides);
    }
}
