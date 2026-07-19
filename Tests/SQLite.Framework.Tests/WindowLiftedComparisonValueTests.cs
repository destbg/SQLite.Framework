using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using SQLite.Framework;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H20WinMetrics")]
public class H20WinMetric
{
    [Key]
    public int Id { get; set; }

    public int Grp { get; set; }

    public int? A { get; set; }

    public int V { get; set; }
}

public class WindowLiftedComparisonValueTests
{
    private static List<H20WinMetric> Metrics()
    {
        return
        [
            new H20WinMetric { Id = 1, Grp = 1, A = 9, V = 10 },
            new H20WinMetric { Id = 2, Grp = 1, A = 3, V = 20 },
            new H20WinMetric { Id = 3, Grp = 2, A = null, V = 10 },
            new H20WinMetric { Id = 4, Grp = 2, A = 8, V = 30 },
            new H20WinMetric { Id = 5, Grp = 1, A = 1, V = 5 }
        ];
    }

    private static TestDatabase Setup()
    {
        TestDatabase db = new();
        db.Table<H20WinMetric>().Schema.CreateTable();
        db.Table<H20WinMetric>().AddRange(Metrics());
        return db;
    }

    [Fact]
    public void PlainProjectionOfNullableCastLiftedComparison()
    {
        using TestDatabase db = Setup();
        List<H20WinMetric> local = Metrics();

        List<(int Id, bool? F)> expected = local
            .OrderBy(x => x.Id)
            .Select(x => (x.Id, F: (bool?)(x.A > 5)))
            .ToList();

        List<(int Id, bool? F)> actual = db.Table<H20WinMetric>()
            .OrderBy(x => x.Id)
            .Select(x => new { x.Id, F = (bool?)(x.A > 5) })
            .AsEnumerable()
            .Select(t => (t.Id, t.F))
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void LagOverLiftedComparisonReadsFalseForNullOperand()
    {
        using TestDatabase db = Setup();
        List<H20WinMetric> local = Metrics();

        List<H20WinMetric> ordered = local.OrderBy(x => x.Id).ToList();
        List<(int Id, bool? F)> expected = ordered
            .Select((x, i) => (x.Id, F: i == 0 ? (bool?)null : (bool?)(ordered[i - 1].A > 5)))
            .ToList();

        List<(int Id, bool? F)> actual = db.Table<H20WinMetric>()
            .Select(x => new
            {
                x.Id,
                F = SQLiteWindowFunctions.Lag((bool?)(x.A > 5), 1L).Over().OrderBy(x.Id).AsValue()
            })
            .AsEnumerable()
            .OrderBy(t => t.Id)
            .Select(t => (t.Id, t.F))
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void LeadOverLiftedComparisonReadsFalseForNullOperand()
    {
        using TestDatabase db = Setup();
        List<H20WinMetric> local = Metrics();

        List<H20WinMetric> ordered = local.OrderBy(x => x.Id).ToList();
        List<(int Id, bool? F)> expected = ordered
            .Select((x, i) => (x.Id, F: i == ordered.Count - 1 ? (bool?)null : (bool?)(ordered[i + 1].A > 5)))
            .ToList();

        List<(int Id, bool? F)> actual = db.Table<H20WinMetric>()
            .Select(x => new
            {
                x.Id,
                F = SQLiteWindowFunctions.Lead((bool?)(x.A > 5), 1L).Over().OrderBy(x.Id).AsValue()
            })
            .AsEnumerable()
            .OrderBy(t => t.Id)
            .Select(t => (t.Id, t.F))
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void FirstValueOverLiftedComparisonReadsFalseForNullOperand()
    {
        using TestDatabase db = Setup();
        List<H20WinMetric> local = Metrics();

        List<(int Id, bool? F)> expected = local
            .OrderBy(x => x.Id)
            .Select(x => (x.Id, F: (bool?)(local.Where(o => o.Grp == x.Grp).OrderBy(o => o.Id).First().A > 5)))
            .ToList();

        List<(int Id, bool? F)> actual = db.Table<H20WinMetric>()
            .Select(x => new
            {
                x.Id,
                F = SQLiteWindowFunctions.FirstValue((bool?)(x.A > 5)).Over().PartitionBy(x.Grp).OrderBy(x.Id).AsValue()
            })
            .AsEnumerable()
            .OrderBy(t => t.Id)
            .Select(t => (t.Id, t.F))
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void WindowMinOverLiftedComparisonSeesFalseForNullOperand()
    {
        using TestDatabase db = Setup();
        List<H20WinMetric> local = Metrics();

        List<(int Id, bool? F)> expected = local
            .OrderBy(x => x.Id)
            .Select(x => (x.Id, F: local.Where(o => o.Grp == x.Grp).Min(o => (bool?)(o.A > 5))))
            .ToList();

        List<(int Id, bool? F)> actual = db.Table<H20WinMetric>()
            .Select(x => new
            {
                x.Id,
                F = SQLiteWindowFunctions.Min((bool?)(x.A > 5)).Over().PartitionBy(x.Grp).AsValue()
            })
            .AsEnumerable()
            .OrderBy(t => t.Id)
            .Select(t => (t.Id, t.F))
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void WindowCountOverLiftedComparisonCountsNullOperandRows()
    {
        using TestDatabase db = Setup();
        List<H20WinMetric> local = Metrics();

        List<(int Id, long C)> expected = local
            .OrderBy(x => x.Id)
            .Select(x => (x.Id, C: (long)local.Count(o => o.Grp == x.Grp)))
            .ToList();

        List<(int Id, long C)> actual = db.Table<H20WinMetric>()
            .Select(x => new
            {
                x.Id,
                C = SQLiteWindowFunctions.Count((bool?)(x.A > 5)).Over().PartitionBy(x.Grp).AsValue()
            })
            .AsEnumerable()
            .OrderBy(t => t.Id)
            .Select(t => (t.Id, t.C))
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void WindowValueArgumentWithoutSqlThrowsCleanError()
    {
        using TestDatabase db = Setup();

        NotSupportedException ex = Assert.Throws<NotSupportedException>(() => db.Table<H20WinMetric>()
            .Select(x => new
            {
                x.Id,
                F = SQLiteWindowFunctions.FirstValue(new { Y = x.V }).Over().PartitionBy(x.Grp).AsValue()
            })
            .ToList());

        Assert.Equal("The value argument of FirstValue cannot be translated to SQL. Use a column or a translatable expression.", ex.Message);
    }
}
