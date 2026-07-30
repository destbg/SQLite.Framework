using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H26gGroupedLiftedRows")]
public class H26gGroupedLiftedRow
{
    [Key]
    public int Id { get; set; }

    public List<int> Nums { get; set; } = [];
}

public class JsonGroupedLiftedKeyProjectionTests
{
    [Fact]
    public void FirstOverALiftedGroupKeyMatchesLinq()
    {
        using TestDatabase db = Setup(nameof(FirstOverALiftedGroupKeyMatchesLinq));

        int? expected = Nums().GroupBy(n => n).Select(g => (int?)g.Key).First();
        int? actual = db.Table<H26gGroupedLiftedRow>()
            .Select(r => r.Nums.GroupBy(n => n).Select(g => (int?)g.Key).First())
            .First();

        Assert.Equal(4, expected);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void LastOverALiftedGroupKeyMatchesLinq()
    {
        using TestDatabase db = Setup(nameof(LastOverALiftedGroupKeyMatchesLinq));

        int? expected = Nums().GroupBy(n => n).Select(g => (int?)g.Key).Last();
        int? actual = db.Table<H26gGroupedLiftedRow>()
            .Select(r => r.Nums.GroupBy(n => n).Select(g => (int?)g.Key).Last())
            .First();

        Assert.Equal(7, expected);
        Assert.Equal(expected, actual);
    }

    private static List<int> Nums()
    {
        return [4, 4, 7];
    }

    private static TestDatabase Setup(string methodName)
    {
        TestDatabase db = new(b => b.TypeConverters[typeof(List<int>)] =
            new SQLiteJsonConverter<List<int>>(TestJsonContext.Default.ListInt32), methodName);
        db.Table<H26gGroupedLiftedRow>().Schema.CreateTable();
        db.Table<H26gGroupedLiftedRow>().Add(new H26gGroupedLiftedRow { Id = 1, Nums = Nums() });
        return db;
    }
}
