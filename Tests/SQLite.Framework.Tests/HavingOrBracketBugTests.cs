using SQLite.Framework;
using SQLite.Framework.Attributes;
using SQLite.Framework.Enums;
using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Helpers;
using SQLite.Framework.Tests.Entities;
using System.Globalization;

namespace SQLite.Framework.Tests;

public class HavingOrBracketBugTests
{
    [Fact]
    public void HavingWithOrTermAndSecondPredicateBracketsCorrectly()
    {
        using TestDatabase db = new();
        db.Table<translatorHavingOrRow>().Schema.CreateTable();
        List<translatorHavingOrRow> data = new()
        {
            new translatorHavingOrRow { Id = 1, Value = 5, Grp = 1 },
            new translatorHavingOrRow { Id = 2, Value = 5, Grp = 2 },
            new translatorHavingOrRow { Id = 3, Value = 5, Grp = 2 },
            new translatorHavingOrRow { Id = 4, Value = 5, Grp = 7 },
        };
        db.Table<translatorHavingOrRow>().AddRange(data);
        List<int> actual = db.Table<translatorHavingOrRow>()
            .GroupBy(x => x.Grp)
            .Where(g => g.Count() == 1 || g.Sum(x => x.Value) > 50)
            .Where(g => g.Key >= 5)
            .Select(g => g.Key)
            .OrderBy(k => k)
            .ToList();
        List<int> expected = data
            .GroupBy(x => x.Grp)
            .Where(g => g.Count() == 1 || g.Sum(x => x.Value) > 50)
            .Where(g => g.Key >= 5)
            .Select(g => g.Key)
            .OrderBy(k => k)
            .ToList();
        Assert.Equal(expected, actual);
    }
}

public class translatorHavingOrRow
{
    [System.ComponentModel.DataAnnotations.Key]
    public int Id { get; set; }
    public int Value { get; set; }
    public int Grp { get; set; }
}
