using System.ComponentModel.DataAnnotations;
using SQLite.Framework;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

file sealed class TakeSkipRow
{
    [Key]
    public int Id { get; set; }

    public List<int> Numbers { get; set; } = [];
}

public class JsonCollectionTakeSkipTests
{
    private static readonly List<int> Seed = [10, 20, 30, 40];

    private static TestDatabase CreateDb()
    {
        TestDatabase db = new(b =>
            b.TypeConverters[typeof(List<int>)] =
                new SQLiteJsonConverter<List<int>>(TestJsonContext.Default.ListInt32));
        db.Table<TakeSkipRow>().Schema.CreateTable();
        db.Table<TakeSkipRow>().Add(new TakeSkipRow { Id = 1, Numbers = [.. Seed] });
        return db;
    }

    [Fact]
    public void TakeNegativeAfterWhereReturnsEmpty()
    {
        using TestDatabase db = CreateDb();

        List<int> expected = Seed.Where(x => x > 0).Take(-1).ToList();
        List<int> actual = db.Table<TakeSkipRow>()
            .Select(r => r.Numbers.Where(x => x > 0).Take(-1).ToList())
            .First();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void TakeNegativeReturnsEmpty()
    {
        using TestDatabase db = CreateDb();

        IEnumerable<int> expected = Seed.Take(-1);
        IEnumerable<int> actual = db.Table<TakeSkipRow>().Select(r => r.Numbers.Take(-1)).First();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void TakeZeroReturnsEmpty()
    {
        using TestDatabase db = CreateDb();

        IEnumerable<int> expected = Seed.Take(0);
        IEnumerable<int> actual = db.Table<TakeSkipRow>().Select(r => r.Numbers.Take(0)).First();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void TakePositiveReturnsPrefix()
    {
        using TestDatabase db = CreateDb();

        IEnumerable<int> expected = Seed.Take(2);
        IEnumerable<int> actual = db.Table<TakeSkipRow>().Select(r => r.Numbers.Take(2)).First();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void TakeMoreThanCountReturnsAll()
    {
        using TestDatabase db = CreateDb();

        IEnumerable<int> expected = Seed.Take(100);
        IEnumerable<int> actual = db.Table<TakeSkipRow>().Select(r => r.Numbers.Take(100)).First();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void SkipNegativeReturnsAll()
    {
        using TestDatabase db = CreateDb();

        IEnumerable<int> expected = Seed.Skip(-1);
        IEnumerable<int> actual = db.Table<TakeSkipRow>().Select(r => r.Numbers.Skip(-1)).First();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void SkipZeroReturnsAll()
    {
        using TestDatabase db = CreateDb();

        IEnumerable<int> expected = Seed.Skip(0);
        IEnumerable<int> actual = db.Table<TakeSkipRow>().Select(r => r.Numbers.Skip(0)).First();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void SkipPositiveReturnsRest()
    {
        using TestDatabase db = CreateDb();

        IEnumerable<int> expected = Seed.Skip(2);
        IEnumerable<int> actual = db.Table<TakeSkipRow>().Select(r => r.Numbers.Skip(2)).First();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void SkipMoreThanCountReturnsEmpty()
    {
        using TestDatabase db = CreateDb();

        IEnumerable<int> expected = Seed.Skip(100);
        IEnumerable<int> actual = db.Table<TakeSkipRow>().Select(r => r.Numbers.Skip(100)).First();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void SkipThenTakeNegativeReturnsEmpty()
    {
        using TestDatabase db = CreateDb();

        List<int> expected = Seed.Skip(1).Take(-1).ToList();
        List<int> actual = db.Table<TakeSkipRow>()
            .Select(r => r.Numbers.Skip(1).Take(-1).ToList())
            .First();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void SkipNegativeThenTakeReturnsPrefix()
    {
        using TestDatabase db = CreateDb();

        List<int> expected = Seed.Skip(-1).Take(2).ToList();
        List<int> actual = db.Table<TakeSkipRow>()
            .Select(r => r.Numbers.Skip(-1).Take(2).ToList())
            .First();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void TakeNegativeAfterOrderByReturnsEmpty()
    {
        using TestDatabase db = CreateDb();

        List<int> expected = Seed.OrderByDescending(x => x).Take(-1).ToList();
        List<int> actual = db.Table<TakeSkipRow>()
            .Select(r => r.Numbers.OrderByDescending(x => x).Take(-1).ToList())
            .First();

        Assert.Equal(expected, actual);
    }
}
