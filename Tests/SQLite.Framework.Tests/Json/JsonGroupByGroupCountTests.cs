using System.ComponentModel.DataAnnotations;
using SQLite.Framework;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

internal sealed class JsonGroupCountRow
{
    [Key]
    public int Id { get; set; }

    public List<int> Numbers { get; set; } = [];
}

public class JsonGroupByGroupCountTests
{
    private static readonly List<int> Seed = [1, 1, 2, 3];

    private static TestDatabase CreateDb()
    {
        TestDatabase db = new(b =>
            b.TypeConverters[typeof(List<int>)] = new SQLiteJsonConverter<List<int>>(TestJsonContext.Default.ListInt32));
        db.Table<JsonGroupCountRow>().Schema.CreateTable();
        db.Table<JsonGroupCountRow>().Add(new JsonGroupCountRow { Id = 1, Numbers = Seed });
        return db;
    }

    [Fact]
    public void GroupByThenCountReturnsNumberOfGroups()
    {
        using TestDatabase db = CreateDb();

        int expected = Seed.GroupBy(x => x).Count();

        Assert.Equal(3, expected);

        int actual = db.Table<JsonGroupCountRow>()
            .Select(r => r.Numbers.GroupBy(x => x).Count())
            .First();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void GroupByComputedKeyThenCountReturnsNumberOfGroups()
    {
        using TestDatabase db = CreateDb();

        int expected = Seed.GroupBy(x => x % 2).Count();

        Assert.Equal(2, expected);

        int actual = db.Table<JsonGroupCountRow>()
            .Select(r => r.Numbers.GroupBy(x => x % 2).Count())
            .First();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void WhereThenGroupByThenCountReturnsNumberOfGroups()
    {
        using TestDatabase db = CreateDb();

        int expected = Seed.Where(x => x > 1).GroupBy(x => x).Count();

        Assert.Equal(2, expected);

        int actual = db.Table<JsonGroupCountRow>()
            .Select(r => r.Numbers.Where(x => x > 1).GroupBy(x => x).Count())
            .First();

        Assert.Equal(expected, actual);
    }
}
