using System.ComponentModel.DataAnnotations;
using SQLite.Framework;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

file sealed class JsonWinRow
{
    [Key]
    public int Id { get; set; }

    public List<int> Numbers { get; set; } = [];
}

public class JsonCollectionWindowTerminalTests
{
    private static TestDatabase CreateDb(List<int> seed)
    {
        TestDatabase db = new(b =>
            b.TypeConverters[typeof(List<int>)] =
                new SQLiteJsonConverter<List<int>>(TestJsonContext.Default.ListInt32));
        db.Table<JsonWinRow>().Schema.CreateTable();
        db.Table<JsonWinRow>().Add(new JsonWinRow { Id = 1, Numbers = [.. seed] });
        return db;
    }

    [Fact]
    public void TakeThenSingleReturnsTheSingleElement()
    {
        List<int> seed = [10, 20, 30];
        using TestDatabase db = CreateDb(seed);

        int oracle = seed.Take(1).Single();
        Assert.Equal(10, oracle);

        int actual = db.Table<JsonWinRow>().Select(r => r.Numbers.Take(1).Single()).First();

        Assert.Equal(oracle, actual);
    }

    [Fact]
    public void TakeThenSingleOrDefaultReturnsTheSingleElement()
    {
        List<int> seed = [10, 20, 30];
        using TestDatabase db = CreateDb(seed);

        int oracle = seed.Take(1).SingleOrDefault();
        Assert.Equal(10, oracle);

        int actual = db.Table<JsonWinRow>().Select(r => r.Numbers.Take(1).SingleOrDefault()).First();

        Assert.Equal(oracle, actual);
    }

    [Fact]
    public void TakeThenFirstWithPredicateLimitsToTheWindow()
    {
        List<int> seed = [10, 20, 30, 40, 50];
        using TestDatabase db = CreateDb(seed);

        int oracle = seed.Take(2).FirstOrDefault(x => x > 25);
        Assert.Equal(0, oracle);

        int actual = db.Table<JsonWinRow>().Select(r => r.Numbers.Take(2).FirstOrDefault(x => x > 25)).First();

        Assert.Equal(oracle, actual);
    }

    [Fact]
    public void SkipThenFirstWithPredicateSkipsBeforeFiltering()
    {
        List<int> seed = [10, 20, 30, 40, 50];
        using TestDatabase db = CreateDb(seed);

        int oracle = seed.Skip(2).First(x => x > 15);
        Assert.Equal(30, oracle);

        int actual = db.Table<JsonWinRow>().Select(r => r.Numbers.Skip(2).First(x => x > 15)).First();

        Assert.Equal(oracle, actual);
    }

    [Fact]
    public void TakeZeroThenFirstOrDefaultReturnsDefault()
    {
        List<int> seed = [10, 20, 30];
        using TestDatabase db = CreateDb(seed);

        int oracle = seed.Take(0).FirstOrDefault();
        Assert.Equal(0, oracle);

        int actual = db.Table<JsonWinRow>().Select(r => r.Numbers.Take(0).FirstOrDefault()).First();

        Assert.Equal(oracle, actual);
    }

    [Fact]
    public void CompositeOrderTakeThenLastReturnsPositionalLast()
    {
        List<int> seed = [5, 3, 8, 1, 9, 2];
        using TestDatabase db = CreateDb(seed);

        int oracle = seed.OrderBy(x => x % 2).ThenByDescending(x => x).Take(4).Last();
        Assert.Equal(5, oracle);

        int actual = db.Table<JsonWinRow>()
            .Select(r => r.Numbers.OrderBy(x => x % 2).ThenByDescending(x => x).Take(4).Last())
            .First();

        Assert.Equal(oracle, actual);
    }

    [Fact]
    public void CompositeOrderTakeThenReverseInvertsWindowOrder()
    {
        List<int> seed = [5, 3, 8, 1, 9, 2];
        using TestDatabase db = CreateDb(seed);

        List<int> oracle = seed.OrderBy(x => x % 2).ThenByDescending(x => x).Take(4).Reverse().ToList();
        Assert.Equal([5, 9, 2, 8], oracle);

        List<int> actual = db.Table<JsonWinRow>()
            .Select(r => r.Numbers.OrderBy(x => x % 2).ThenByDescending(x => x).Take(4).Reverse().ToList())
            .First();

        Assert.Equal(oracle, actual);
    }
}
