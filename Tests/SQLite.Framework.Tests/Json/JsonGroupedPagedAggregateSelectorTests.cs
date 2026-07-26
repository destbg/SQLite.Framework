using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H22fGroupedPagedRows")]
public class H22fGroupedPagedRow
{
    [Key]
    public int Id { get; set; }

    public List<int> Nums { get; set; } = [];
}

public class JsonGroupedPagedAggregateSelectorTests
{
    [Fact]
    public void SumOfGroupKeysAfterTakeMatchesLinq()
    {
        using TestDatabase db = Setup(nameof(SumOfGroupKeysAfterTakeMatchesLinq));

        int expected = Nums().GroupBy(n => n).Take(2).Sum(g => g.Key);
        int actual = db.Table<H22fGroupedPagedRow>()
            .Select(r => r.Nums.GroupBy(n => n).Take(2).Sum(g => g.Key))
            .First();

        Assert.Equal(3, expected);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void AverageOfGroupKeysAfterSkipMatchesLinq()
    {
        using TestDatabase db = Setup(nameof(AverageOfGroupKeysAfterSkipMatchesLinq));

        double expected = Nums().GroupBy(n => n).Skip(1).Average(g => g.Key);
        double actual = db.Table<H22fGroupedPagedRow>()
            .Select(r => r.Nums.GroupBy(n => n).Skip(1).Average(g => g.Key))
            .First();

        Assert.Equal(2.5, expected);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void MaxOfGroupKeysAfterTakeMatchesLinq()
    {
        using TestDatabase db = Setup(nameof(MaxOfGroupKeysAfterTakeMatchesLinq));

        int expected = Nums().GroupBy(n => n).Take(2).Max(g => g.Key);
        int actual = db.Table<H22fGroupedPagedRow>()
            .Select(r => r.Nums.GroupBy(n => n).Take(2).Max(g => g.Key))
            .First();

        Assert.Equal(2, expected);
        Assert.Equal(expected, actual);
    }

    private static List<int> Nums()
    {
        return [1, 2, 3, 1];
    }

    private static TestDatabase Setup(string methodName)
    {
        TestDatabase db = new(b => b.TypeConverters[typeof(List<int>)] =
            new SQLiteJsonConverter<List<int>>(TestJsonContext.Default.ListInt32), methodName);
        db.Table<H22fGroupedPagedRow>().Schema.CreateTable();
        db.Table<H22fGroupedPagedRow>().Add(new H22fGroupedPagedRow { Id = 1, Nums = Nums() });
        return db;
    }
}
