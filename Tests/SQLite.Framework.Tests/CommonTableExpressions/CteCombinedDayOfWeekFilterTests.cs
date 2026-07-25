using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Enums;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H21nDowMixRows")]
public class H21nDowMixRow
{
    [Key]
    public int Id { get; set; }

    public DateTime When { get; set; }

    public DayOfWeek Day { get; set; }
}

public class H21nDowMixPair
{
    public int N { get; set; }

    public DayOfWeek Dow { get; set; }
}

public class CteCombinedDayOfWeekFilterTests
{
    private static List<H21nDowMixRow> Rows()
    {
        return
        [
            new H21nDowMixRow { Id = 1, When = new DateTime(2024, 1, 1), Day = DayOfWeek.Monday },
            new H21nDowMixRow { Id = 2, When = new DateTime(2024, 1, 2), Day = DayOfWeek.Sunday },
            new H21nDowMixRow { Id = 3, When = new DateTime(2024, 1, 7), Day = DayOfWeek.Monday },
            new H21nDowMixRow { Id = 4, When = new DateTime(2024, 1, 8), Day = DayOfWeek.Tuesday },
        ];
    }

    private static TestDatabase Setup()
    {
        TestDatabase db = new(b => b.UseEnumStorage(EnumStorageMode.Text));
        db.Table<H21nDowMixRow>().Schema.CreateTable();
        db.Table<H21nDowMixRow>().AddRange(Rows());
        return db;
    }

    [Fact]
    public void ComputedArmFirstCombinedCteFilterMatchesLinq()
    {
        using TestDatabase db = Setup();
        List<H21nDowMixRow> local = Rows();

        List<int> expected = local
            .Select(r => new H21nDowMixPair { N = r.Id, Dow = r.When.DayOfWeek })
            .Concat(local.Select(r => new H21nDowMixPair { N = r.Id + 100, Dow = r.Day }))
            .Where(p => p.Dow == DayOfWeek.Monday)
            .Select(p => p.N)
            .OrderBy(n => n)
            .ToList();

        SQLiteCte<H21nDowMixPair> cte = db.With(() =>
            db.Table<H21nDowMixRow>()
                .Select(r => new H21nDowMixPair { N = r.Id, Dow = r.When.DayOfWeek })
                .Concat(db.Table<H21nDowMixRow>()
                    .Select(r => new H21nDowMixPair { N = r.Id + 100, Dow = r.Day })));

        List<int> actual = cte
            .Where(p => p.Dow == DayOfWeek.Monday)
            .Select(p => p.N)
            .ToList()
            .OrderBy(n => n)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void StoredArmFirstCombinedCteFilterMatchesLinq()
    {
        using TestDatabase db = Setup();
        List<H21nDowMixRow> local = Rows();

        List<int> expected = local
            .Select(r => new H21nDowMixPair { N = r.Id + 100, Dow = r.Day })
            .Concat(local.Select(r => new H21nDowMixPair { N = r.Id, Dow = r.When.DayOfWeek }))
            .Where(p => p.Dow == DayOfWeek.Monday)
            .Select(p => p.N)
            .OrderBy(n => n)
            .ToList();

        SQLiteCte<H21nDowMixPair> cte = db.With(() =>
            db.Table<H21nDowMixRow>()
                .Select(r => new H21nDowMixPair { N = r.Id + 100, Dow = r.Day })
                .Concat(db.Table<H21nDowMixRow>()
                    .Select(r => new H21nDowMixPair { N = r.Id, Dow = r.When.DayOfWeek })));

        List<int> actual = cte
            .Where(p => p.Dow == DayOfWeek.Monday)
            .Select(p => p.N)
            .ToList()
            .OrderBy(n => n)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ComputedArmFirstCombinedCteValuesMatchLinq()
    {
        using TestDatabase db = Setup();
        List<H21nDowMixRow> local = Rows();

        List<DayOfWeek> expected = local
            .Select(r => new H21nDowMixPair { N = r.Id, Dow = r.When.DayOfWeek })
            .Concat(local.Select(r => new H21nDowMixPair { N = r.Id + 100, Dow = r.Day }))
            .OrderBy(p => p.N)
            .Select(p => p.Dow)
            .ToList();

        SQLiteCte<H21nDowMixPair> cte = db.With(() =>
            db.Table<H21nDowMixRow>()
                .Select(r => new H21nDowMixPair { N = r.Id, Dow = r.When.DayOfWeek })
                .Concat(db.Table<H21nDowMixRow>()
                    .Select(r => new H21nDowMixPair { N = r.Id + 100, Dow = r.Day })));

        List<DayOfWeek> actual = cte
            .OrderBy(p => p.N)
            .Select(p => p.Dow)
            .ToList();

        Assert.Equal(expected, actual);
    }
}
