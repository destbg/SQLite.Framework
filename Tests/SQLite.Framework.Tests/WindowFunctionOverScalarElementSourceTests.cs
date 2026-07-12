using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("wfse_events")]
public class WfseEvent
{
    [Key]
    public int Id { get; set; }

    public int Grp { get; set; }
}

public class WindowFunctionOverScalarElementSourceTests
{
    [Fact]
    public void WindowSumOverValuesRangeElement()
    {
        using TestDatabase db = new();
        int[] values = [1, 2, 2, 5];

        List<(int, int)> expected = values
            .Select(v => (v, values.Where(x => x == v).Sum()))
            .OrderBy(x => x.Item1).ThenBy(x => x.Item2)
            .ToList();

        List<(int, int)> actual = db.ValuesRange(values)
            .Select(v => new { V = v, S = SQLiteWindowFunctions.Sum(v).Over().PartitionBy(v).AsValue() })
            .AsEnumerable()
            .Select(x => (x.V, x.S))
            .OrderBy(x => x.Item1).ThenBy(x => x.Item2)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void WindowSumOverScalarCteElement()
    {
        using TestDatabase db = new();
        db.Table<WfseEvent>().Schema.CreateTable();
        List<WfseEvent> events =
        [
            new WfseEvent { Id = 1, Grp = 1 },
            new WfseEvent { Id = 2, Grp = 1 },
            new WfseEvent { Id = 3, Grp = 2 },
        ];
        db.Table<WfseEvent>().AddRange(events);

        List<int> merged = events.Select(e => e.Grp).ToList();

        List<(int, int)> expected = merged
            .Select(v => (v, merged.Where(x => x == v).Sum()))
            .OrderBy(x => x.Item1).ThenBy(x => x.Item2)
            .ToList();

        SQLiteCte<int> cte = db.With(() => db.Table<WfseEvent>().Select(e => e.Grp));

        List<(int, int)> actual = (from v in cte select new { V = v, S = SQLiteWindowFunctions.Sum(v).Over().PartitionBy(v).AsValue() })
            .AsEnumerable()
            .Select(x => (x.V, x.S))
            .OrderBy(x => x.Item1).ThenBy(x => x.Item2)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void WindowPartitionByScalarCteElementWithComputedAggregateArgument()
    {
        using TestDatabase db = new();
        db.Table<WfseEvent>().Schema.CreateTable();
        List<WfseEvent> events =
        [
            new WfseEvent { Id = 1, Grp = 1 },
            new WfseEvent { Id = 2, Grp = 1 },
            new WfseEvent { Id = 3, Grp = 2 },
        ];
        db.Table<WfseEvent>().AddRange(events);

        List<int> merged = events.Select(e => e.Grp).ToList();

        List<(int, int)> expected = merged
            .Select(v => (v, merged.Where(x => x == v).Sum(x => x + 0)))
            .OrderBy(x => x.Item1).ThenBy(x => x.Item2)
            .ToList();

        SQLiteCte<int> cte = db.With(() => db.Table<WfseEvent>().Select(e => e.Grp));

        List<(int, int)> actual = (from v in cte select new { V = v, S = SQLiteWindowFunctions.Sum(v + 0).Over().PartitionBy(v).AsValue() })
            .AsEnumerable()
            .Select(x => (x.V, x.S))
            .OrderBy(x => x.Item1).ThenBy(x => x.Item2)
            .ToList();

        Assert.Equal(expected, actual);
    }
}
