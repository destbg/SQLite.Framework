using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("CapturedProjRow")]
public class CapturedProjRow
{
    [Key]
    public int Id { get; set; }

    public int Num { get; set; }
}

public class CapturedCollectionSelectProjectionTests
{
    private static List<CapturedProjRow> Rows() =>
    [
        new() { Id = 1, Num = 3 },
        new() { Id = 2, Num = 2 },
        new() { Id = 3, Num = 12 },
    ];

    private static TestDatabase Seed(string methodName)
    {
        TestDatabase db = new(methodName);
        db.Table<CapturedProjRow>().Schema.CreateTable();
        db.Table<CapturedProjRow>().AddRange(Rows());
        return db;
    }

    [Fact]
    public void CountWithPredicateComputesInMemory()
    {
        using TestDatabase db = Seed(nameof(CountWithPredicateComputesInMemory));
        List<int> values = [1, 2, 5, 11];
        List<int> expected = Rows().Select(r => values.Count(v => v > r.Num)).ToList();
        List<int> actual = db.Table<CapturedProjRow>().OrderBy(r => r.Id).Select(r => values.Count(v => v > r.Num)).ToList();
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void AnyWithPredicateComputesInMemory()
    {
        using TestDatabase db = Seed(nameof(AnyWithPredicateComputesInMemory));
        List<int> values = [1, 2, 5, 11];
        List<bool> expected = Rows().Select(r => values.Any(v => v > r.Num)).ToList();
        List<bool> actual = db.Table<CapturedProjRow>().OrderBy(r => r.Id).Select(r => values.Any(v => v > r.Num)).ToList();
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void SumWithSelectorComputesInMemory()
    {
        using TestDatabase db = Seed(nameof(SumWithSelectorComputesInMemory));
        List<int> values = [1, 2];
        List<int> expected = Rows().Select(r => values.Sum(v => v + r.Num)).ToList();
        List<int> actual = db.Table<CapturedProjRow>().OrderBy(r => r.Id).Select(r => values.Sum(v => v + r.Num)).ToList();
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void FirstOrDefaultWithPredicateComputesInMemory()
    {
        using TestDatabase db = Seed(nameof(FirstOrDefaultWithPredicateComputesInMemory));
        List<int> values = [1, 2, 5, 11];
        List<int> expected = Rows().Select(r => values.FirstOrDefault(v => v > r.Num)).ToList();
        List<int> actual = db.Table<CapturedProjRow>().OrderBy(r => r.Id).Select(r => values.FirstOrDefault(v => v > r.Num)).ToList();
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void WhereThenCountComputesInMemory()
    {
        using TestDatabase db = Seed(nameof(WhereThenCountComputesInMemory));
        List<int> values = [1, 2, 5, 11];
        List<int> expected = Rows().Select(r => values.Where(v => v > r.Num).Count()).ToList();
        List<int> actual = db.Table<CapturedProjRow>().OrderBy(r => r.Id).Select(r => values.Where(v => v > r.Num).Count()).ToList();
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void FindAllMaxComputesInMemory()
    {
        using TestDatabase db = Seed(nameof(FindAllMaxComputesInMemory));
        List<int> values = [1, 2, 5, 11];
        List<int> expected = Rows().Select(r => values.FindAll(v => v < r.Num).Max()).ToList();
        List<int> actual = db.Table<CapturedProjRow>().OrderBy(r => r.Id).Select(r => values.FindAll(v => v < r.Num).Max()).ToList();
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ConvertAllSumComputesInMemory()
    {
        using TestDatabase db = Seed(nameof(ConvertAllSumComputesInMemory));
        List<int> values = [1, 2];
        List<int> expected = Rows().Select(r => values.ConvertAll(v => v * r.Num).Sum()).ToList();
        List<int> actual = db.Table<CapturedProjRow>().OrderBy(r => r.Id).Select(r => values.ConvertAll(v => v * r.Num).Sum()).ToList();
        Assert.Equal(expected, actual);
    }
}
