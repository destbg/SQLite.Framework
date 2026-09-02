using System.ComponentModel.DataAnnotations;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

internal sealed class ComputedOrderBySetRow
{
    [Key]
    public int Id { get; set; }

    public int A { get; set; }

    public int B { get; set; }

    public string Title { get; set; } = "";
}

public class ComputedOrderByAfterSetOperationTests
{
    private static readonly ComputedOrderBySetRow[] Data =
    [
        new ComputedOrderBySetRow { Id = 1, A = 3, B = 1, Title = "one" },
        new ComputedOrderBySetRow { Id = 2, A = 1, B = 2, Title = "zz" },
        new ComputedOrderBySetRow { Id = 3, A = 2, B = 4, Title = "four" },
        new ComputedOrderBySetRow { Id = 4, A = 1, B = 5, Title = "x" },
        new ComputedOrderBySetRow { Id = 5, A = 1, B = 2, Title = "five" },
    ];

    [Fact]
    public void OrderByComputedKeyAfterUnionMatchesObjects()
    {
        using TestDatabase db = new();
        db.Table<ComputedOrderBySetRow>().Schema.CreateTable();
        foreach (ComputedOrderBySetRow r in Data)
        {
            db.Table<ComputedOrderBySetRow>().Add(r);
        }

        List<int> expected = Data.Select(x => x.A)
            .Union(Data.Select(x => x.B))
            .OrderBy(x => -x)
            .ToList();
        List<int> actual = db.Table<ComputedOrderBySetRow>().Select(x => x.A)
            .Union(db.Table<ComputedOrderBySetRow>().Select(x => x.B))
            .OrderBy(x => -x)
            .ToList();

        Assert.Equal([5, 4, 3, 2, 1], expected);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void OrderByBareColumnAfterUnionWorks()
    {
        using TestDatabase db = new();
        db.Table<ComputedOrderBySetRow>().Schema.CreateTable();
        foreach (ComputedOrderBySetRow r in Data)
        {
            db.Table<ComputedOrderBySetRow>().Add(r);
        }

        List<int> expected = Data.Select(x => x.A)
            .Union(Data.Select(x => x.B))
            .OrderBy(x => x)
            .ToList();

        List<int> actual = db.Table<ComputedOrderBySetRow>().Select(x => x.A)
            .Union(db.Table<ComputedOrderBySetRow>().Select(x => x.B))
            .OrderBy(x => x)
            .ToList();

        Assert.Equal([1, 2, 3, 4, 5], expected);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void OrderByNestedMemberAfterUnionMatchesObjects()
    {
        using TestDatabase db = new();
        db.Table<ComputedOrderBySetRow>().Schema.CreateTable();
        db.Table<ComputedOrderBySetRow>().AddRange(Data);

        List<int> expected = Data.Where(x => x.Id <= 2)
            .Union(Data.Where(x => x.Id > 2))
            .OrderBy(x => x.Title.Length)
            .ThenBy(x => x.Id)
            .Select(x => x.Id)
            .ToList();
        List<int> actual = db.Table<ComputedOrderBySetRow>().Where(x => x.Id <= 2)
            .Union(db.Table<ComputedOrderBySetRow>().Where(x => x.Id > 2))
            .OrderBy(x => x.Title.Length)
            .ThenBy(x => x.Id)
            .Select(x => x.Id)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ThenByComputedKeyAfterUnionKeepsPrimaryOrder()
    {
        using TestDatabase db = new();
        db.Table<ComputedOrderBySetRow>().Schema.CreateTable();
        db.Table<ComputedOrderBySetRow>().AddRange(Data);

        List<int> expected = Data.Where(x => x.Id <= 2)
            .Union(Data.Where(x => x.Id > 2))
            .OrderBy(x => x.A)
            .ThenBy(x => -x.B)
            .Select(x => x.Id)
            .ToList();
        List<int> actual = db.Table<ComputedOrderBySetRow>().Where(x => x.Id <= 2)
            .Union(db.Table<ComputedOrderBySetRow>().Where(x => x.Id > 2))
            .OrderBy(x => x.A)
            .ThenBy(x => -x.B)
            .Select(x => x.Id)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ConvertedDirectKeysAfterUnionMatchObjects()
    {
        using TestDatabase db = new();
        db.Table<ComputedOrderBySetRow>().Schema.CreateTable();
        db.Table<ComputedOrderBySetRow>().AddRange(Data);

        List<int> expectedScalars = Data.Select(x => x.A)
            .Union(Data.Select(x => x.B))
            .OrderBy(x => (long)x)
            .ToList();
        List<int> actualScalars = db.Table<ComputedOrderBySetRow>().Select(x => x.A)
            .Union(db.Table<ComputedOrderBySetRow>().Select(x => x.B))
            .OrderBy(x => (long)x)
            .ToList();
        List<int> expectedRows = Data.Where(x => x.Id <= 2)
            .Union(Data.Where(x => x.Id > 2))
            .OrderBy(x => ((int?)x.A)!.Value)
            .ThenBy(x => x.Id)
            .Select(x => x.Id)
            .ToList();
        List<int> actualRows = db.Table<ComputedOrderBySetRow>().Where(x => x.Id <= 2)
            .Union(db.Table<ComputedOrderBySetRow>().Where(x => x.Id > 2))
            .OrderBy(x => ((int?)x.A)!.Value)
            .ThenBy(x => x.Id)
            .Select(x => x.Id)
            .ToList();

        Assert.Equal(expectedScalars, actualScalars);
        Assert.Equal(expectedRows, actualRows);
    }

    [Fact]
    public void ThirdComputedOrderKeyAfterUnionKeepsEarlierKeys()
    {
        using TestDatabase db = new();
        db.Table<ComputedOrderBySetRow>().Schema.CreateTable();
        db.Table<ComputedOrderBySetRow>().AddRange(Data);

        List<int> expected = Data.Where(x => x.Id <= 2)
            .Union(Data.Where(x => x.Id > 2))
            .OrderByDescending(x => x.A)
            .ThenByDescending(x => x.B)
            .ThenBy(x => -x.Id)
            .Select(x => x.Id)
            .ToList();
        List<int> actual = db.Table<ComputedOrderBySetRow>().Where(x => x.Id <= 2)
            .Union(db.Table<ComputedOrderBySetRow>().Where(x => x.Id > 2))
            .OrderByDescending(x => x.A)
            .ThenByDescending(x => x.B)
            .ThenBy(x => -x.Id)
            .Select(x => x.Id)
            .ToList();

        Assert.Equal([1, 3, 4, 5, 2], expected);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void DescendingThirdComputedOrderKeyAfterUnionKeepsEarlierKeys()
    {
        using TestDatabase db = new();
        db.Table<ComputedOrderBySetRow>().Schema.CreateTable();
        db.Table<ComputedOrderBySetRow>().AddRange(Data);

        List<int> expected = Data.Where(x => x.Id <= 2)
            .Union(Data.Where(x => x.Id > 2))
            .OrderByDescending(x => x.A)
            .ThenBy(x => x.B)
            .ThenByDescending(x => -x.Id)
            .Select(x => x.Id)
            .ToList();
        List<int> actual = db.Table<ComputedOrderBySetRow>().Where(x => x.Id <= 2)
            .Union(db.Table<ComputedOrderBySetRow>().Where(x => x.Id > 2))
            .OrderByDescending(x => x.A)
            .ThenBy(x => x.B)
            .ThenByDescending(x => -x.Id)
            .Select(x => x.Id)
            .ToList();

        Assert.Equal([1, 3, 2, 5, 4], expected);
        Assert.Equal(expected, actual);
    }
}
