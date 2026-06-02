using System.ComponentModel.DataAnnotations;
using SQLite.Framework;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

file sealed class JsonAggRow
{
    [Key]
    public int Id { get; set; }

    public List<int> Numbers { get; set; } = [];
}

public class JsonAggregateOverEmptyTests
{
    private static TestDatabase CreateDb()
    {
        TestDatabase db = new(b =>
            b.TypeConverters[typeof(List<int>)] =
                new SQLiteJsonConverter<List<int>>(TestJsonContext.Default.ListInt32));
        db.Table<JsonAggRow>().Schema.CreateTable();
        return db;
    }

    [Fact]
    public void SumOverEmptyArrayProjectionReturnsZero()
    {
        using TestDatabase db = CreateDb();
        db.Table<JsonAggRow>().Add(new JsonAggRow { Id = 1, Numbers = [] });

        int expected = new List<int>().Sum();
        int actual = db.Table<JsonAggRow>().Select(r => r.Numbers.Sum()).First();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void SumOverEmptyArrayToListReturnsZero()
    {
        using TestDatabase db = CreateDb();
        db.Table<JsonAggRow>().Add(new JsonAggRow { Id = 1, Numbers = [] });

        List<int> expected = new List<List<int>> { new() }.Select(n => n.Sum()).ToList();
        List<int> actual = db.Table<JsonAggRow>().Select(r => r.Numbers.Sum()).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void SumOverEmptyArrayInWhereMatchesDotNet()
    {
        using TestDatabase db = CreateDb();
        db.Table<JsonAggRow>().Add(new JsonAggRow { Id = 1, Numbers = [] });
        db.Table<JsonAggRow>().Add(new JsonAggRow { Id = 2, Numbers = [3, 4] });

        List<JsonAggRow> rows =
        [
            new() { Id = 1, Numbers = [] },
            new() { Id = 2, Numbers = [3, 4] },
        ];

        List<int> expected = rows.Where(r => r.Numbers.Sum() == 0).Select(r => r.Id).ToList();
        List<int> actual = db.Table<JsonAggRow>()
            .Where(r => r.Numbers.Sum() == 0)
            .Select(r => r.Id)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void SumOverEmptyArrayInArithmeticMatchesDotNet()
    {
        using TestDatabase db = CreateDb();
        db.Table<JsonAggRow>().Add(new JsonAggRow { Id = 1, Numbers = [] });

        int expected = new List<int>().Sum() + 100;
        int actual = db.Table<JsonAggRow>().Select(r => r.Numbers.Sum() + 100).First();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void SumOverNonEmptyArrayMatchesDotNet()
    {
        using TestDatabase db = CreateDb();
        db.Table<JsonAggRow>().Add(new JsonAggRow { Id = 1, Numbers = [3, 4, 5] });

        int expected = new List<int> { 3, 4, 5 }.Sum();
        int actual = db.Table<JsonAggRow>().Select(r => r.Numbers.Sum()).First();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void SumAfterWhereOverEmptyResultInArithmeticMatchesDotNet()
    {
        using TestDatabase db = CreateDb();
        db.Table<JsonAggRow>().Add(new JsonAggRow { Id = 1, Numbers = [1, 2, 3] });

        int expected = new List<int> { 1, 2, 3 }.Where(x => x > 100).Sum() + 100;
        int actual = db.Table<JsonAggRow>()
            .Select(r => r.Numbers.Where(x => x > 100).Sum() + 100)
            .First();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void FirstOrDefaultOverEmptyArrayProjectionReturnsDefault()
    {
        using TestDatabase db = CreateDb();
        db.Table<JsonAggRow>().Add(new JsonAggRow { Id = 1, Numbers = [] });

        int expected = new List<int>().FirstOrDefault();
        int actual = db.Table<JsonAggRow>().Select(r => r.Numbers.FirstOrDefault()).First();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void FirstOrDefaultOverNonEmptyArrayReturnsFirst()
    {
        using TestDatabase db = CreateDb();
        db.Table<JsonAggRow>().Add(new JsonAggRow { Id = 1, Numbers = [7, 8, 9] });

        int expected = new List<int> { 7, 8, 9 }.FirstOrDefault();
        int actual = db.Table<JsonAggRow>().Select(r => r.Numbers.FirstOrDefault()).First();

        Assert.Equal(expected, actual);
    }
}
