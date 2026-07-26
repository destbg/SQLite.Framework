using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H22fGroupedSingleRows")]
public class H22fGroupedSingleRow
{
    [Key]
    public int Id { get; set; }

    public List<int> Nums { get; set; } = [];
}

public class JsonGroupedProjectionSingleTests
{
    [Fact]
    public void SingleOverTheOnlyGroupKeyMatchesLinq()
    {
        using TestDatabase db = Setup(nameof(SingleOverTheOnlyGroupKeyMatchesLinq));

        int expected = Nums().GroupBy(n => n).Select(g => g.Key).Single();
        int actual = db.Table<H22fGroupedSingleRow>()
            .Select(r => r.Nums.GroupBy(n => n).Select(g => g.Key).Single())
            .First();

        Assert.Equal(4, expected);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void SingleOrDefaultOverTheOnlyGroupKeyMatchesLinq()
    {
        using TestDatabase db = Setup(nameof(SingleOrDefaultOverTheOnlyGroupKeyMatchesLinq));

        int expected = Nums().GroupBy(n => n).Select(g => g.Key).SingleOrDefault();
        int actual = db.Table<H22fGroupedSingleRow>()
            .Select(r => r.Nums.GroupBy(n => n).Select(g => g.Key).SingleOrDefault())
            .First();

        Assert.Equal(4, expected);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void SingleOverTheOnlyGroupKeyAfterAGroupFilterMatchesLinq()
    {
        using TestDatabase db = Setup(nameof(SingleOverTheOnlyGroupKeyAfterAGroupFilterMatchesLinq));

        int expected = Nums().GroupBy(n => n).Where(g => g.Count() > 1).Select(g => g.Key).Single();
        int actual = db.Table<H22fGroupedSingleRow>()
            .Select(r => r.Nums.GroupBy(n => n).Where(g => g.Count() > 1).Select(g => g.Key).Single())
            .First();

        Assert.Equal(4, expected);
        Assert.Equal(expected, actual);
    }

    private static List<int> Nums()
    {
        return [4, 4, 4];
    }

    private static TestDatabase Setup(string methodName)
    {
        TestDatabase db = new(b => b.TypeConverters[typeof(List<int>)] =
            new SQLiteJsonConverter<List<int>>(TestJsonContext.Default.ListInt32), methodName);
        db.Table<H22fGroupedSingleRow>().Schema.CreateTable();
        db.Table<H22fGroupedSingleRow>().Add(new H22fGroupedSingleRow { Id = 1, Nums = Nums() });
        return db;
    }
}
