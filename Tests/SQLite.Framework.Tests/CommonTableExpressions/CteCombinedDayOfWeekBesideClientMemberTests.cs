using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Enums;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H22eDowMixRows")]
public class H22eDowMixRow
{
    [Key]
    public int Id { get; set; }

    public DateTime When { get; set; }

    public DayOfWeek Day { get; set; }

    public int Extra { get; set; }
}

public class H22eDowMixPair
{
    public int N { get; set; }

    public DayOfWeek Dow { get; set; }

    public int[] Tags { get; set; } = [];
}

public class CteCombinedDayOfWeekBesideClientMemberTests
{
    [Fact]
    public void ComputedArmFirstBesideArrayMemberFiltersOnDayOfWeek()
    {
        using TestDatabase db = Setup();
        List<H22eDowMixRow> local = Rows();

        List<int> expected = local
            .Select(r => new H22eDowMixPair { N = r.Id, Dow = r.When.DayOfWeek, Tags = new[] { r.Extra } })
            .Concat(local.Select(r => new H22eDowMixPair { N = r.Id + 100, Dow = r.Day, Tags = new[] { r.Extra } }))
            .Where(p => p.Dow == DayOfWeek.Monday)
            .Select(p => p.N)
            .OrderBy(n => n)
            .ToList();

        List<int> actual = db.With(() => db.Table<H22eDowMixRow>()
                .Select(r => new H22eDowMixPair { N = r.Id, Dow = r.When.DayOfWeek, Tags = new[] { r.Extra } })
                .Concat(db.Table<H22eDowMixRow>()
                    .Select(r => new H22eDowMixPair { N = r.Id + 100, Dow = r.Day, Tags = new[] { r.Extra } })))
            .Where(p => p.Dow == DayOfWeek.Monday)
            .Select(p => p.N)
            .ToList()
            .OrderBy(n => n)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void StoredArmFirstBesideArrayMemberFiltersOnDayOfWeek()
    {
        using TestDatabase db = Setup();
        List<H22eDowMixRow> local = Rows();

        List<int> expected = local
            .Select(r => new H22eDowMixPair { N = r.Id + 100, Dow = r.Day, Tags = new[] { r.Extra } })
            .Concat(local.Select(r => new H22eDowMixPair { N = r.Id, Dow = r.When.DayOfWeek, Tags = new[] { r.Extra } }))
            .Where(p => p.Dow == DayOfWeek.Monday)
            .Select(p => p.N)
            .OrderBy(n => n)
            .ToList();

        List<int> actual = db.With(() => db.Table<H22eDowMixRow>()
                .Select(r => new H22eDowMixPair { N = r.Id + 100, Dow = r.Day, Tags = new[] { r.Extra } })
                .Concat(db.Table<H22eDowMixRow>()
                    .Select(r => new H22eDowMixPair { N = r.Id, Dow = r.When.DayOfWeek, Tags = new[] { r.Extra } })))
            .Where(p => p.Dow == DayOfWeek.Monday)
            .Select(p => p.N)
            .ToList()
            .OrderBy(n => n)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ComputedArmFirstBesideArrayMemberReadsDayOfWeekValues()
    {
        using TestDatabase db = Setup();
        List<H22eDowMixRow> local = Rows();

        List<DayOfWeek> expected = local
            .Select(r => new H22eDowMixPair { N = r.Id, Dow = r.When.DayOfWeek, Tags = new[] { r.Extra } })
            .Concat(local.Select(r => new H22eDowMixPair { N = r.Id + 100, Dow = r.Day, Tags = new[] { r.Extra } }))
            .OrderBy(p => p.N)
            .Select(p => p.Dow)
            .ToList();

        List<DayOfWeek> actual = db.With(() => db.Table<H22eDowMixRow>()
                .Select(r => new H22eDowMixPair { N = r.Id, Dow = r.When.DayOfWeek, Tags = new[] { r.Extra } })
                .Concat(db.Table<H22eDowMixRow>()
                    .Select(r => new H22eDowMixPair { N = r.Id + 100, Dow = r.Day, Tags = new[] { r.Extra } })))
            .OrderBy(p => p.N)
            .Select(p => p.Dow)
            .ToList();

        Assert.Equal(expected, actual);
    }

    private static List<H22eDowMixRow> Rows()
    {
        return
        [
            new H22eDowMixRow { Id = 1, When = new DateTime(2024, 1, 1), Day = DayOfWeek.Monday, Extra = 11 },
            new H22eDowMixRow { Id = 2, When = new DateTime(2024, 1, 2), Day = DayOfWeek.Sunday, Extra = 22 },
            new H22eDowMixRow { Id = 3, When = new DateTime(2024, 1, 7), Day = DayOfWeek.Monday, Extra = 33 },
            new H22eDowMixRow { Id = 4, When = new DateTime(2024, 1, 8), Day = DayOfWeek.Tuesday, Extra = 44 },
        ];
    }

    private static TestDatabase Setup()
    {
        TestDatabase db = new(b => b.UseEnumStorage(EnumStorageMode.Text));
        db.Table<H22eDowMixRow>().Schema.CreateTable();
        db.Table<H22eDowMixRow>().AddRange(Rows());
        return db;
    }
}
