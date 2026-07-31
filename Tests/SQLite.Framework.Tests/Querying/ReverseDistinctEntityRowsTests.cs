using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("ReverseDistinctEntityRows")]
public class ReverseDistinctEntityRow
{
    [Key]
    public int Id { get; set; }

    public int A { get; set; }
}

[Table("ReverseDistinctSingleColumnRows")]
public class ReverseDistinctSingleColumnRow
{
    [Key]
    public int Id { get; set; }
}

public class ReverseDistinctEntityRowsTests
{
    [Fact]
    public void ReverseThenDistinctOverWholeEntitiesReturnsRowsInDescendingOrder()
    {
        using TestDatabase db = Setup(nameof(ReverseThenDistinctOverWholeEntitiesReturnsRowsInDescendingOrder));

        List<int> actual = db.Table<ReverseDistinctEntityRow>()
            .OrderBy(r => r.Id)
            .Reverse()
            .Distinct()
            .AsEnumerable()
            .Select(r => r.Id)
            .ToList();

        Assert.Equal(new List<int> { 3, 2, 1 }, actual);
    }

    [Fact]
    public void ReverseThenDistinctOverAStoredCteScalarKeepsTheLatestValues()
    {
        using TestDatabase db = Setup(nameof(ReverseThenDistinctOverAStoredCteScalarKeepsTheLatestValues));

        SQLiteCte<ReverseDistinctEntityRow> cte = db.With(() => db.Table<ReverseDistinctEntityRow>()
            .Where(r => r.Id > 0));

        List<int> expected = Rows()
            .Where(r => r.Id > 0)
            .OrderBy(r => r.A)
            .Reverse()
            .Select(r => r.A)
            .Distinct()
            .ToList();

        List<int> actual = cte
            .OrderBy(r => r.A)
            .Reverse()
            .Select(r => r.A)
            .Distinct()
            .AsEnumerable()
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ReverseThenDistinctOverAScalarCteKeepsTheLatestValues()
    {
        using TestDatabase db = Setup(nameof(ReverseThenDistinctOverAScalarCteKeepsTheLatestValues));

        SQLiteCte<int> cte = db.With(() => db.Table<ReverseDistinctEntityRow>()
            .Select(r => r.A));

        List<int> expected = Rows()
            .Select(r => r.A)
            .OrderBy(v => v)
            .Reverse()
            .Distinct()
            .ToList();

        List<int> actual = cte
            .OrderBy(v => v)
            .Reverse()
            .Distinct()
            .AsEnumerable()
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ReverseThenDistinctOverASingleColumnEntityReturnsRowsInDescendingOrder()
    {
        using TestDatabase db = new(null, nameof(ReverseThenDistinctOverASingleColumnEntityReturnsRowsInDescendingOrder));
        db.Table<ReverseDistinctSingleColumnRow>().Schema.CreateTable();
        db.Table<ReverseDistinctSingleColumnRow>().AddRange(
        [
            new ReverseDistinctSingleColumnRow { Id = 1 },
            new ReverseDistinctSingleColumnRow { Id = 2 }
        ]);

        List<int> actual = db.Table<ReverseDistinctSingleColumnRow>()
            .OrderBy(r => r.Id)
            .Reverse()
            .Distinct()
            .AsEnumerable()
            .Select(r => r.Id)
            .ToList();

        Assert.Equal(new List<int> { 2, 1 }, actual);
    }

    private static List<ReverseDistinctEntityRow> Rows()
    {
        return
        [
            new ReverseDistinctEntityRow { Id = 1, A = 10 },
            new ReverseDistinctEntityRow { Id = 2, A = 20 },
            new ReverseDistinctEntityRow { Id = 3, A = 10 }
        ];
    }

    private static TestDatabase Setup(string methodName)
    {
        TestDatabase db = new(null, methodName);
        db.Table<ReverseDistinctEntityRow>().Schema.CreateTable();
        db.Table<ReverseDistinctEntityRow>().AddRange(Rows());
        return db;
    }
}
