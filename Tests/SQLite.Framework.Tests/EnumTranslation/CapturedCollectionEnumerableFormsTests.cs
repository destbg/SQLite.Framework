using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("CapturedFormsRow")]
public class CapturedFormsRow
{
    [Key]
    public int Id { get; set; }

    public int Num { get; set; }
}

public class CapturedCollectionEnumerableFormsTests
{
    private static List<CapturedFormsRow> Rows() =>
    [
        new() { Id = 1, Num = 3 },
        new() { Id = 2, Num = 2 },
        new() { Id = 3, Num = 12 },
    ];

    private static TestDatabase Seed(string methodName)
    {
        TestDatabase db = new(methodName);
        db.Table<CapturedFormsRow>().Schema.CreateTable();
        db.Table<CapturedFormsRow>().AddRange(Rows());
        return db;
    }

    [Fact]
    public void EmptyConcatCapturedValuesComputesInMemory()
    {
        using TestDatabase db = Seed(nameof(EmptyConcatCapturedValuesComputesInMemory));
        List<int> values = [7, 8];
        List<List<int>> expected = Rows().OrderBy(r => r.Id).Select(_ => Enumerable.Empty<int>().Concat(values).ToList()).ToList();
        List<List<int>> actual = db.Table<CapturedFormsRow>().OrderBy(r => r.Id).Select(r => Enumerable.Empty<int>().Concat(values).ToList()).ToList();
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void EmptyRootedAppendColumnCountComputesInMemory()
    {
        using TestDatabase db = Seed(nameof(EmptyRootedAppendColumnCountComputesInMemory));
        List<int> expected = Rows().OrderBy(r => r.Id).Select(r => Enumerable.Empty<int>().Append(r.Num).Count()).ToList();
        List<int> actual = db.Table<CapturedFormsRow>().OrderBy(r => r.Id).Select(r => Enumerable.Empty<int>().Append(r.Num).Count()).ToList();
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void AllWithPredicateComputesInMemory()
    {
        using TestDatabase db = Seed(nameof(AllWithPredicateComputesInMemory));
        List<int> values = [1, 2, 5, 11];
        List<bool> expected = Rows().Select(r => values.All(v => v > r.Num)).ToList();
        List<bool> actual = db.Table<CapturedFormsRow>().OrderBy(r => r.Id).Select(r => values.All(v => v > r.Num)).ToList();
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void MinWithSelectorComputesInMemory()
    {
        using TestDatabase db = Seed(nameof(MinWithSelectorComputesInMemory));
        List<int> values = [1, 2, 5, 11];
        List<int> expected = Rows().Select(r => values.Min(v => v + r.Num)).ToList();
        List<int> actual = db.Table<CapturedFormsRow>().OrderBy(r => r.Id).Select(r => values.Min(v => v + r.Num)).ToList();
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void MaxWithSelectorComputesInMemory()
    {
        using TestDatabase db = Seed(nameof(MaxWithSelectorComputesInMemory));
        List<int> values = [1, 2, 5, 11];
        List<int> expected = Rows().Select(r => values.Max(v => v - r.Num)).ToList();
        List<int> actual = db.Table<CapturedFormsRow>().OrderBy(r => r.Id).Select(r => values.Max(v => v - r.Num)).ToList();
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void AverageWithSelectorComputesInMemory()
    {
        using TestDatabase db = Seed(nameof(AverageWithSelectorComputesInMemory));
        List<int> values = [1, 2, 5, 11];
        List<double> expected = Rows().Select(r => values.Average(v => v + r.Num)).ToList();
        List<double> actual = db.Table<CapturedFormsRow>().OrderBy(r => r.Id).Select(r => values.Average(v => v + r.Num)).ToList();
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void LastOrDefaultWithPredicateComputesInMemory()
    {
        using TestDatabase db = Seed(nameof(LastOrDefaultWithPredicateComputesInMemory));
        List<int> values = [1, 2, 5, 11];
        List<int> expected = Rows().Select(r => values.LastOrDefault(v => v < r.Num)).ToList();
        List<int> actual = db.Table<CapturedFormsRow>().OrderBy(r => r.Id).Select(r => values.LastOrDefault(v => v < r.Num)).ToList();
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void SelectThenSumComputesInMemory()
    {
        using TestDatabase db = Seed(nameof(SelectThenSumComputesInMemory));
        List<int> values = [1, 2];
        List<int> expected = Rows().Select(r => values.Select(v => v * r.Num).Sum()).ToList();
        List<int> actual = db.Table<CapturedFormsRow>().OrderBy(r => r.Id).Select(r => values.Select(v => v * r.Num).Sum()).ToList();
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ArrayCountWithPredicateComputesInMemory()
    {
        using TestDatabase db = Seed(nameof(ArrayCountWithPredicateComputesInMemory));
        int[] values = [1, 2, 5, 11];
        List<int> expected = Rows().Select(r => values.Count(v => v > r.Num)).ToList();
        List<int> actual = db.Table<CapturedFormsRow>().OrderBy(r => r.Id).Select(r => values.Count(v => v > r.Num)).ToList();
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void SelectThenCountWithPredicateComputesInMemory()
    {
        using TestDatabase db = Seed(nameof(SelectThenCountWithPredicateComputesInMemory));
        List<int> values = [1, 2, 5, 11];
        List<int> expected = Rows().Select(r => values.Select(v => v * 2).Count(v => v > r.Num)).ToList();
        List<int> actual = db.Table<CapturedFormsRow>().OrderBy(r => r.Id).Select(r => values.Select(v => v * 2).Count(v => v > r.Num)).ToList();
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void WhereThenAnyWithPredicateComputesInMemory()
    {
        using TestDatabase db = Seed(nameof(WhereThenAnyWithPredicateComputesInMemory));
        List<int> values = [1, 2, 5, 11];
        List<bool> expected = Rows().Select(r => values.Where(v => v > 1).Any(v => v > r.Num)).ToList();
        List<bool> actual = db.Table<CapturedFormsRow>().OrderBy(r => r.Id).Select(r => values.Where(v => v > 1).Any(v => v > r.Num)).ToList();
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void FindAllThenCountWithPredicateComputesInMemory()
    {
        using TestDatabase db = Seed(nameof(FindAllThenCountWithPredicateComputesInMemory));
        List<int> values = [1, 2, 5, 11];
        List<int> expected = Rows().Select(r => values.FindAll(v => v > 1).Count(v => v > r.Num)).ToList();
        List<int> actual = db.Table<CapturedFormsRow>().OrderBy(r => r.Id).Select(r => values.FindAll(v => v > 1).Count(v => v > r.Num)).ToList();
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void IndexedCapturedListCountComputesInMemory()
    {
        using TestDatabase db = Seed(nameof(IndexedCapturedListCountComputesInMemory));
        List<List<int>> lists = [[1, 2, 5, 11]];
        List<int> expected = Rows().Select(r => lists[0].Count(v => v > r.Num)).ToList();
        List<int> actual = db.Table<CapturedFormsRow>().OrderBy(r => r.Id).Select(r => lists[0].Count(v => v > r.Num)).ToList();
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void RepeatSourceWithPredicateThrows()
    {
        using TestDatabase db = Seed(nameof(RepeatSourceWithPredicateThrows));
        Assert.Throws<NotSupportedException>(() =>
            db.Table<CapturedFormsRow>().Where(r => Enumerable.Repeat(2, 3).Count(v => v > r.Num) == 1).ToList());
    }

    [Fact]
    public void CountWithPredicateInWhereThrowsCleanly()
    {
        using TestDatabase db = Seed(nameof(CountWithPredicateInWhereThrowsCleanly));
        List<int> values = [1, 2, 5, 11];
        NotSupportedException ex = Assert.Throws<NotSupportedException>(() =>
            db.Table<CapturedFormsRow>().Where(r => values.Count(v => v > r.Num) == 2).ToList());
        Assert.Contains("not translatable in a Where", ex.Message);
    }
}
