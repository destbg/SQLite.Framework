#if !SQLITE_FRAMEWORK_REFLECTION_AOT_INCOMPATIBLE
using System;
using System.Collections.Generic;
using System.Linq;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("sg_gbk_src")]
public class SgGbkSrc
{
    [Key]
    public int Id { get; set; }

    public string Name { get; set; } = "";

    public int Value { get; set; }
}

public class SgGroupByKeyMaterializeTests
{
    private static List<SgGbkSrc> Rows()
    {
        return
        [
            new SgGbkSrc { Id = 1, Name = "apple", Value = 3 },
            new SgGbkSrc { Id = 2, Name = "avocado", Value = 5 },
            new SgGbkSrc { Id = 3, Name = "banana", Value = 7 },
        ];
    }

    private static TestDatabase Seed()
    {
        TestDatabase db = new();
        db.Table<SgGbkSrc>().Schema.CreateTable();
        db.Table<SgGbkSrc>().AddRange(Rows());
        return db;
    }

    [Fact]
    public void GroupByMemberKeyMaterializesGroupings()
    {
        using TestDatabase db = Seed();

        List<(int, int)> expected = Rows()
            .GroupBy(r => r.Name)
            .Select(g => (g.Count(), g.Sum(r => r.Value)))
            .OrderBy(t => t.Item1)
            .ThenBy(t => t.Item2)
            .ToList();

        List<(int, int)> actual = db.Table<SgGbkSrc>()
            .GroupBy(r => r.Name)
            .ToList()
            .Select(g => (g.Count(), g.Sum(r => r.Value)))
            .OrderBy(t => t.Item1)
            .ThenBy(t => t.Item2)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void GroupByMethodCallKeyMaterializesGroupings()
    {
        using TestDatabase db = Seed();

        List<(string, int)> expected = Rows()
            .GroupBy(r => r.Name.Substring(0, 1))
            .Select(g => (g.Key, g.Count()))
            .OrderBy(t => t.Item1, StringComparer.Ordinal)
            .ToList();

        List<(string, int)> actual = db.Table<SgGbkSrc>()
            .GroupBy(r => r.Name.Substring(0, 1))
            .ToList()
            .Select(g => (g.Key, g.Count()))
            .OrderBy(t => t.Item1, StringComparer.Ordinal)
            .ToList();

        Assert.Equal(expected, actual);
    }
}
#endif
