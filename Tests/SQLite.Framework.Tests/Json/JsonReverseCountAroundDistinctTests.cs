using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H22fReverseDistinctRows")]
public class H22fReverseDistinctRow
{
    [Key]
    public int Id { get; set; }

    public List<int> Nums { get; set; } = [];
}

public class JsonReverseCountAroundDistinctTests
{
    [Fact]
    public void ReverseThenDistinctThenReverseKeepsTheLastOccurrenceOrder()
    {
        using TestDatabase db = Setup(nameof(ReverseThenDistinctThenReverseKeepsTheLastOccurrenceOrder));

        List<int> expected = Enumerable.Reverse(Nums()).Distinct().Reverse().ToList();
        List<int> actual = db.Table<H22fReverseDistinctRow>()
            .Select(r => Enumerable.Reverse(r.Nums).Distinct().Reverse().ToList())
            .First();

        Assert.Equal([2, 1, 3], expected);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void DistinctAfterTwoReversesKeepsTheFirstOccurrenceOrder()
    {
        using TestDatabase db = Setup(nameof(DistinctAfterTwoReversesKeepsTheFirstOccurrenceOrder));

        List<int> expected = Enumerable.Reverse(Enumerable.Reverse(Nums())).Distinct().ToList();
        List<int> actual = db.Table<H22fReverseDistinctRow>()
            .Select(r => Enumerable.Reverse(Enumerable.Reverse(r.Nums)).Distinct().ToList())
            .First();

        Assert.Equal([1, 2, 3], expected);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void DistinctAfterAnOrderByAndTwoReversesKeepsTheSortedFirstOccurrenceOrder()
    {
        using TestDatabase db = Setup(nameof(DistinctAfterAnOrderByAndTwoReversesKeepsTheSortedFirstOccurrenceOrder));

        List<int> expected = Nums().OrderBy(n => n % 2).Reverse().Reverse().Distinct().ToList();
        List<int> actual = db.Table<H22fReverseDistinctRow>()
            .Select(r => r.Nums.OrderBy(n => n % 2).Reverse().Reverse().Distinct().ToList())
            .First();

        Assert.Equal([2, 1, 3], expected);
        Assert.Equal(expected, actual);
    }

    private static List<int> Nums()
    {
        return [1, 2, 1, 3];
    }

    private static TestDatabase Setup(string methodName)
    {
        TestDatabase db = new(b => b.TypeConverters[typeof(List<int>)] =
            new SQLiteJsonConverter<List<int>>(TestJsonContext.Default.ListInt32), methodName);
        db.Table<H22fReverseDistinctRow>().Schema.CreateTable();
        db.Table<H22fReverseDistinctRow>().Add(new H22fReverseDistinctRow { Id = 1, Nums = Nums() });
        return db;
    }
}
