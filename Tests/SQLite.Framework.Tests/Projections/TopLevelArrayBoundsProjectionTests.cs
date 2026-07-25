using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H20ArrBounds")]
public class H20ArrBoundsRow
{
    [Key]
    public int Id { get; set; }

    public int A { get; set; }
}

public class TopLevelArrayBoundsProjectionTests
{
    private static List<H20ArrBoundsRow> Rows() =>
    [
        new H20ArrBoundsRow { Id = 1, A = 3 },
        new H20ArrBoundsRow { Id = 2, A = 0 },
        new H20ArrBoundsRow { Id = 3, A = 5 },
    ];

    private static TestDatabase Setup()
    {
        TestDatabase db = new();
        db.Table<H20ArrBoundsRow>().Schema.CreateTable();
        db.Table<H20ArrBoundsRow>().AddRange(Rows());
        return db;
    }

    [Fact]
    public void ColumnBoundTopLevelArrayMatchesLinq()
    {
        using TestDatabase db = Setup();

        List<int[]> expected = Rows().OrderBy(r => r.Id)
            .Select(r => new int[r.A]).ToList();

        List<int[]> actual = db.Table<H20ArrBoundsRow>()
            .OrderBy(r => r.Id)
            .Select(r => new int[r.A])
            .ToList();

        Assert.Equal(expected.Count, actual.Count);
        for (int i = 0; i < expected.Count; i++)
        {
            Assert.Equal(expected[i], actual[i]);
        }
    }

    [Fact]
    public void ConstantBoundTopLevelArrayMatchesLinq()
    {
        using TestDatabase db = Setup();

        List<int[]> expected = Rows().OrderBy(r => r.Id)
            .Select(r => new int[2]).ToList();

        List<int[]> actual = db.Table<H20ArrBoundsRow>()
            .OrderBy(r => r.Id)
            .Select(r => new int[2])
            .ToList();

        Assert.Equal(expected.Count, actual.Count);
        for (int i = 0; i < expected.Count; i++)
        {
            Assert.Equal(expected[i], actual[i]);
        }
    }

    [Fact]
    public void ComputedBoundTopLevelArrayMatchesLinq()
    {
        using TestDatabase db = Setup();

        List<int[]> expected = Rows().OrderBy(r => r.Id)
            .Select(r => new int[r.A % 2 + 1]).ToList();

        List<int[]> actual = db.Table<H20ArrBoundsRow>()
            .OrderBy(r => r.Id)
            .Select(r => new int[r.A % 2 + 1])
            .ToList();

        Assert.Equal(expected.Count, actual.Count);
        for (int i = 0; i < expected.Count; i++)
        {
            Assert.Equal(expected[i], actual[i]);
        }
    }

    [Fact]
    public void MultiDimBoundTopLevelArrayMatchesLinq()
    {
        using TestDatabase db = Setup();

        List<int> expected = Rows().OrderBy(r => r.Id)
            .Select(r => new int[2, 3])
            .SelectMany(a => a.Cast<int>().Prepend(a.Length))
            .ToList();

        List<int> actual = db.Table<H20ArrBoundsRow>()
            .OrderBy(r => r.Id)
            .Select(r => new int[2, 3])
            .ToList()
            .SelectMany(a => a.Cast<int>().Prepend(a.Length))
            .ToList();

        Assert.Equal(expected, actual);
    }
}
