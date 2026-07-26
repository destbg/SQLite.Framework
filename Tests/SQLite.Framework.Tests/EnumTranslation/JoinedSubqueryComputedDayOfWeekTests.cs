using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Enums;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H22qDowStamps")]
public class H22qDowStamp
{
    [Key]
    public int Id { get; set; }

    public DateTime When { get; set; }
}

[Table("H22qDowLinks")]
public class H22qDowLink
{
    [Key]
    public int Id { get; set; }

    public int StampId { get; set; }
}

public class H22qDowKeyed
{
    public int Key { get; set; }

    public DayOfWeek Dow { get; set; }
}

public class JoinedSubqueryComputedDayOfWeekTests
{
    [Fact]
    public void FilterOnTheJoinedSubqueryDayOfWeekMatchesLinq()
    {
        using TestDatabase db = Setup();

        List<int> expected = Links()
            .Join(
                Stamps().Select(s => new H22qDowKeyed { Key = s.Id, Dow = s.When.DayOfWeek }),
                l => l.StampId,
                k => k.Key,
                (l, k) => new { l.Id, k.Dow })
            .Where(t => t.Dow == DayOfWeek.Monday)
            .Select(t => t.Id)
            .OrderBy(v => v)
            .ToList();

        Assert.Equal([100, 300], expected);

        List<int> actual = db.Table<H22qDowLink>()
            .Join(
                db.Table<H22qDowStamp>().Select(s => new H22qDowKeyed { Key = s.Id, Dow = s.When.DayOfWeek }),
                l => l.StampId,
                k => k.Key,
                (l, k) => new { l.Id, k.Dow })
            .Where(t => t.Dow == DayOfWeek.Monday)
            .Select(t => t.Id)
            .OrderBy(v => v)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ProjectedComparisonOnTheJoinedSubqueryDayOfWeekMatchesLinq()
    {
        using TestDatabase db = Setup();

        List<bool> expected = Links()
            .Join(
                Stamps().Select(s => new H22qDowKeyed { Key = s.Id, Dow = s.When.DayOfWeek }),
                l => l.StampId,
                k => k.Key,
                (l, k) => new { l.Id, k.Dow })
            .OrderBy(t => t.Id)
            .Select(t => t.Dow == DayOfWeek.Monday)
            .ToList();

        Assert.Equal([true, false, true], expected);

        List<bool> actual = db.Table<H22qDowLink>()
            .Join(
                db.Table<H22qDowStamp>().Select(s => new H22qDowKeyed { Key = s.Id, Dow = s.When.DayOfWeek }),
                l => l.StampId,
                k => k.Key,
                (l, k) => new { l.Id, k.Dow })
            .OrderBy(t => t.Id)
            .Select(t => t.Dow == DayOfWeek.Monday)
            .ToList();

        Assert.Equal(expected, actual);
    }

    private static List<H22qDowStamp> Stamps()
    {
        return
        [
            new H22qDowStamp { Id = 1, When = new DateTime(2024, 1, 1, 9, 0, 0) },
            new H22qDowStamp { Id = 2, When = new DateTime(2024, 1, 2, 9, 0, 0) },
            new H22qDowStamp { Id = 3, When = new DateTime(2024, 1, 8, 9, 0, 0) }
        ];
    }

    private static List<H22qDowLink> Links()
    {
        return
        [
            new H22qDowLink { Id = 100, StampId = 1 },
            new H22qDowLink { Id = 200, StampId = 2 },
            new H22qDowLink { Id = 300, StampId = 3 }
        ];
    }

    private static TestDatabase Setup()
    {
        TestDatabase db = new(b => b.UseEnumStorage(EnumStorageMode.Text));
        db.Table<H22qDowStamp>().Schema.CreateTable();
        db.Table<H22qDowLink>().Schema.CreateTable();
        db.Table<H22qDowStamp>().AddRange(Stamps());
        db.Table<H22qDowLink>().AddRange(Links());
        return db;
    }
}
