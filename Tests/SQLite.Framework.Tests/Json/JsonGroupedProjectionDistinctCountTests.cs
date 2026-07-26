using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H22fGroupedDistinctCountRows")]
public class H22fGroupedDistinctCountRow
{
    [Key]
    public int Id { get; set; }

    public List<int> Nums { get; set; } = [];
}

public class JsonGroupedProjectionDistinctCountTests
{
    [Fact]
    public void DistinctCountOverAGroupKeyProjectionMatchesLinq()
    {
        using TestDatabase db = Setup(nameof(DistinctCountOverAGroupKeyProjectionMatchesLinq));

        int expected = Nums().GroupBy(n => n).Select(g => g.Key % 2).Distinct().Count();
        int actual = db.Table<H22fGroupedDistinctCountRow>()
            .Select(r => r.Nums.GroupBy(n => n).Select(g => g.Key % 2).Distinct().Count())
            .First();

        Assert.Equal(2, expected);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void DistinctLongCountOverAGroupKeyProjectionMatchesLinq()
    {
        using TestDatabase db = Setup(nameof(DistinctLongCountOverAGroupKeyProjectionMatchesLinq));

        long expected = Nums().GroupBy(n => n).Select(g => g.Key % 2).Distinct().LongCount();
        long actual = db.Table<H22fGroupedDistinctCountRow>()
            .Select(r => r.Nums.GroupBy(n => n).Select(g => g.Key % 2).Distinct().LongCount())
            .First();

        Assert.Equal(2L, expected);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void DistinctCountOverAHalvedGroupKeyProjectionMatchesLinq()
    {
        using TestDatabase db = Setup(nameof(DistinctCountOverAHalvedGroupKeyProjectionMatchesLinq));

        int expected = Nums().GroupBy(n => n).Select(g => g.Key / 2).Distinct().Count();
        int actual = db.Table<H22fGroupedDistinctCountRow>()
            .Select(r => r.Nums.GroupBy(n => n).Select(g => g.Key / 2).Distinct().Count())
            .First();

        Assert.Equal(3, expected);
        Assert.Equal(expected, actual);
    }

    private static List<int> Nums()
    {
        return [1, 2, 3, 4];
    }

    private static TestDatabase Setup(string methodName)
    {
        TestDatabase db = new(b => b.TypeConverters[typeof(List<int>)] =
            new SQLiteJsonConverter<List<int>>(TestJsonContext.Default.ListInt32), methodName);
        db.Table<H22fGroupedDistinctCountRow>().Schema.CreateTable();
        db.Table<H22fGroupedDistinctCountRow>().Add(new H22fGroupedDistinctCountRow { Id = 1, Nums = Nums() });
        return db;
    }
}
