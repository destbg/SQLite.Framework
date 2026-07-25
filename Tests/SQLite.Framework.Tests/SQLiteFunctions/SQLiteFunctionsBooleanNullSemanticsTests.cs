using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H21gScoreRows")]
public class H21gScoreRow
{
    [Key]
    public int Id { get; set; }

    public int? Score { get; set; }

    public int Weight { get; set; }
}

public class SQLiteFunctionsBooleanNullSemanticsTests
{
    [Fact]
    public void NegatedBetweenOverNullableScoreProjectsTrueForNullRows()
    {
        using TestDatabase db = Setup();
        List<H21gScoreRow> local = Rows();

        List<bool> expected = local
            .OrderBy(x => x.Id)
            .Select(x => !(x.Score >= 2 && x.Score <= 4))
            .ToList();

        List<bool> actual = db.Table<H21gScoreRow>()
            .OrderBy(x => x.Id)
            .Select(x => !SQLiteFunctions.Between(x.Score, (int?)2, (int?)4))
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void NegatedBetweenOverNullableScoreKeepsNullRows()
    {
        using TestDatabase db = Setup();
        List<H21gScoreRow> local = Rows();

        List<int> expected = local
            .Where(x => !(x.Score >= 2 && x.Score <= 4))
            .Select(x => x.Id)
            .OrderBy(v => v)
            .ToList();

        List<int> actual = db.Table<H21gScoreRow>()
            .Where(x => !SQLiteFunctions.Between(x.Score, (int?)2, (int?)4))
            .Select(x => x.Id)
            .AsEnumerable()
            .OrderBy(v => v)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void BetweenComparedToFalseOverNullableScoreProjectsTrueForNullRows()
    {
        using TestDatabase db = Setup();
        List<H21gScoreRow> local = Rows();

        List<bool> expected = local
            .OrderBy(x => x.Id)
            .Select(x => (x.Score >= 2 && x.Score <= 4) == false)
            .ToList();

        List<bool> actual = db.Table<H21gScoreRow>()
            .OrderBy(x => x.Id)
            .Select(x => SQLiteFunctions.Between(x.Score, (int?)2, (int?)4) == false)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void NegatedInOverNullableScoreProjectsTrueForNullRows()
    {
        using TestDatabase db = Setup();
        List<H21gScoreRow> local = Rows();
        int?[] wanted = [2, 3];

        List<bool> expected = local
            .OrderBy(x => x.Id)
            .Select(x => !wanted.Contains(x.Score))
            .ToList();

        List<bool> actual = db.Table<H21gScoreRow>()
            .OrderBy(x => x.Id)
            .Select(x => !SQLiteFunctions.In(x.Score, (int?)2, (int?)3))
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void NegatedInOverNullableScoreKeepsNullRows()
    {
        using TestDatabase db = Setup();
        List<H21gScoreRow> local = Rows();
        int?[] wanted = [2, 3];

        List<int> expected = local
            .Where(x => !wanted.Contains(x.Score))
            .Select(x => x.Id)
            .OrderBy(v => v)
            .ToList();

        List<int> actual = db.Table<H21gScoreRow>()
            .Where(x => !SQLiteFunctions.In(x.Score, (int?)2, (int?)3))
            .Select(x => x.Id)
            .AsEnumerable()
            .OrderBy(v => v)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void NegatedInWithNullListElementProjectsTrueForNonMatchingRows()
    {
        using TestDatabase db = Setup();
        List<H21gScoreRow> local = Rows();
        int?[] wanted = [10, null];

        List<bool> expected = local
            .OrderBy(x => x.Id)
            .Select(x => !wanted.Contains(x.Weight))
            .ToList();

        List<bool> actual = db.Table<H21gScoreRow>()
            .OrderBy(x => x.Id)
            .Select(x => !SQLiteFunctions.In((int?)x.Weight, (int?)10, null))
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void WindowPartitionByBetweenGroupsNullRowsWithFalse()
    {
        using TestDatabase db = Setup();
        List<H21gScoreRow> local = Rows();

        Dictionary<bool, long> sizes = local
            .GroupBy(x => x.Score >= 2 && x.Score <= 4)
            .ToDictionary(g => g.Key, g => (long)g.Count());

        List<long> expected = local
            .OrderBy(x => x.Id)
            .Select(x => sizes[x.Score >= 2 && x.Score <= 4])
            .ToList();

        List<long> actual = db.Table<H21gScoreRow>()
            .OrderBy(x => x.Id)
            .Select(x => SQLiteWindowFunctions.Count()
                .Over()
                .PartitionBy(SQLiteFunctions.Between(x.Score, (int?)2, (int?)4))
                .AsValue())
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void WindowOrderByBetweenSortsNullRowsWithFalse()
    {
        using TestDatabase db = Setup();
        List<H21gScoreRow> local = Rows();

        List<(int Id, long Rn)> expected = local
            .OrderBy(x => x.Score >= 2 && x.Score <= 4)
            .ThenBy(x => x.Id)
            .Select((x, i) => (Id: x.Id, Rn: (long)i + 1))
            .OrderBy(t => t.Id)
            .ToList();

        List<(int Id, long Rn)> actual = db.Table<H21gScoreRow>()
            .Select(x => new
            {
                x.Id,
                Rn = SQLiteWindowFunctions.RowNumber()
                    .Over()
                    .OrderBy(SQLiteFunctions.Between(x.Score, (int?)2, (int?)4))
                    .ThenOrderBy(x.Id)
                    .AsValue()
            })
            .AsEnumerable()
            .Select(a => (Id: a.Id, Rn: a.Rn))
            .OrderBy(t => t.Id)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void WindowFilterWithNegatedBetweenKeepsNullRows()
    {
        using TestDatabase db = Setup();
        List<H21gScoreRow> local = Rows();

        int total = local
            .Where(x => !(x.Score >= 2 && x.Score <= 4))
            .Sum(x => x.Weight);

        List<int> expected = local
            .OrderBy(x => x.Id)
            .Select(_ => total)
            .ToList();

        List<int> actual = db.Table<H21gScoreRow>()
            .OrderBy(x => x.Id)
            .Select(x => SQLiteWindowFunctions.Sum(x.Weight)
                .Filter(!SQLiteFunctions.Between(x.Score, (int?)2, (int?)4))
                .Over()
                .AsValue())
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void NegatedInlineRangeComparisonKeepsNullRows()
    {
        using TestDatabase db = Setup();
        List<H21gScoreRow> local = Rows();

        List<int> expected = local
            .Where(x => !(x.Score >= 2 && x.Score <= 4))
            .Select(x => x.Id)
            .OrderBy(v => v)
            .ToList();

        List<int> actual = db.Table<H21gScoreRow>()
            .Where(x => !(x.Score >= 2 && x.Score <= 4))
            .Select(x => x.Id)
            .AsEnumerable()
            .OrderBy(v => v)
            .ToList();

        Assert.Equal(expected, actual);
    }

    private static List<H21gScoreRow> Rows()
    {
        return
        [
            new H21gScoreRow { Id = 1, Score = 1, Weight = 10 },
            new H21gScoreRow { Id = 2, Score = 3, Weight = 20 },
            new H21gScoreRow { Id = 3, Score = null, Weight = 30 },
            new H21gScoreRow { Id = 4, Score = 5, Weight = 40 },
            new H21gScoreRow { Id = 5, Score = null, Weight = 50 }
        ];
    }

    private static TestDatabase Setup()
    {
        TestDatabase db = new();
        db.Table<H21gScoreRow>().Schema.CreateTable();
        db.Table<H21gScoreRow>().AddRange(Rows());
        return db;
    }
}
