using System.ComponentModel.DataAnnotations;
using SQLite.Framework;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

internal sealed class JawRow
{
    [Key]
    public int Id { get; set; }
    public List<int> Numbers { get; set; } = [];
}

public class JsonArrayWindowOrderingTests
{
    private static readonly List<int> A = [5, 3, 8, 1, 9, 2];
    private static readonly List<int> B = [3, 1, 3, 2, 1, 3];

    private static TestDatabase CreateDb()
    {
        TestDatabase db = new(b => b.TypeConverters[typeof(List<int>)] = new SQLiteJsonConverter<List<int>>(TestJsonContext.Default.ListInt32));
        db.Table<JawRow>().Schema.CreateTable();
        db.Table<JawRow>().Add(new JawRow { Id = 1, Numbers = A });
        db.Table<JawRow>().Add(new JawRow { Id = 2, Numbers = B });
        return db;
    }

    [Fact]
    public void TakeThenReverse()
    {
        using TestDatabase db = CreateDb();
        List<int> oracle = A.Take(3).Reverse().ToList();
        List<int> actual = db.Table<JawRow>().Where(r => r.Id == 1).Select(r => r.Numbers.Take(3).Reverse()).First().ToList();
        Assert.Equal([8, 3, 5], oracle);
        Assert.Equal(oracle, actual);
    }

    [Fact]
    public void SkipThenReverse()
    {
        using TestDatabase db = CreateDb();
        List<int> oracle = A.Skip(4).Reverse().ToList();
        List<int> actual = db.Table<JawRow>().Where(r => r.Id == 1).Select(r => r.Numbers.Skip(4).Reverse()).First().ToList();
        Assert.Equal([2, 9], oracle);
        Assert.Equal(oracle, actual);
    }

    [Fact]
    public void TakeThenWhere()
    {
        using TestDatabase db = CreateDb();
        List<int> oracle = A.Take(4).Where(x => x > 2).ToList();
        List<int> actual = db.Table<JawRow>().Where(r => r.Id == 1).Select(r => r.Numbers.Take(4).Where(x => x > 2)).First().ToList();
        Assert.Equal([5, 3, 8], oracle);
        Assert.Equal(oracle, actual);
    }

    [Fact]
    public void SkipThenWhere()
    {
        using TestDatabase db = CreateDb();
        List<int> oracle = A.Skip(2).Where(x => x > 2).ToList();
        List<int> actual = db.Table<JawRow>().Where(r => r.Id == 1).Select(r => r.Numbers.Skip(2).Where(x => x > 2)).First().ToList();
        Assert.Equal([8, 9], oracle);
        Assert.Equal(oracle, actual);
    }

    [Fact]
    public void TakeThenLastPredicate()
    {
        using TestDatabase db = CreateDb();
        int oracle = A.Take(4).Last(x => x > 2);
        int actual = db.Table<JawRow>().Where(r => r.Id == 1).Select(r => r.Numbers.Take(4).Last(x => x > 2)).First();
        Assert.Equal(8, oracle);
        Assert.Equal(oracle, actual);
    }

    [Fact]
    public void TakeThenLastNoPredicate()
    {
        using TestDatabase db = CreateDb();
        int oracle = A.Take(3).Last();
        int actual = db.Table<JawRow>().Where(r => r.Id == 1).Select(r => r.Numbers.Take(3).Last()).First();
        Assert.Equal(8, oracle);
        Assert.Equal(oracle, actual);
    }

    [Fact]
    public void SkipThenLastPredicate()
    {
        using TestDatabase db = CreateDb();
        int oracle = A.Skip(1).Last(x => x > 2);
        int actual = db.Table<JawRow>().Where(r => r.Id == 1).Select(r => r.Numbers.Skip(1).Last(x => x > 2)).First();
        Assert.Equal(9, oracle);
        Assert.Equal(oracle, actual);
    }

    [Fact]
    public void TakeThenOrderByAscendingFirst_OrdersTheWindow()
    {
        using TestDatabase db = CreateDb();
        int oracle = A.Take(3).OrderBy(x => x).First();
        int actual = db.Table<JawRow>().Where(r => r.Id == 1).Select(r => r.Numbers.Take(3).OrderBy(x => x).First()).First();
        Assert.Equal(3, oracle);
        Assert.Equal(oracle, actual);
    }

    [Fact]
    public void TakeThenOrderByDescendingFirst_OrdersTheWindow()
    {
        using TestDatabase db = CreateDb();
        int oracle = A.Take(3).OrderByDescending(x => x).First();
        int actual = db.Table<JawRow>().Where(r => r.Id == 1).Select(r => r.Numbers.Take(3).OrderByDescending(x => x).First()).First();
        Assert.Equal(8, oracle);
        Assert.Equal(oracle, actual);
    }

    [Fact]
    public void OrderByThenTakeThenReverse()
    {
        using TestDatabase db = CreateDb();
        List<int> oracle = A.OrderBy(x => x).Take(3).Reverse().ToList();
        List<int> actual = db.Table<JawRow>().Where(r => r.Id == 1).Select(r => r.Numbers.OrderBy(x => x).Take(3).Reverse()).First().ToList();
        Assert.Equal([3, 2, 1], oracle);
        Assert.Equal(oracle, actual);
    }

    [Fact]
    public void TakeThenSkip()
    {
        using TestDatabase db = CreateDb();
        List<int> oracle = A.Take(4).Skip(1).ToList();
        List<int> actual = db.Table<JawRow>().Where(r => r.Id == 1).Select(r => r.Numbers.Take(4).Skip(1)).First().ToList();
        Assert.Equal([3, 8, 1], oracle);
        Assert.Equal(oracle, actual);
    }

    [Fact]
    public void TakeThenTake()
    {
        using TestDatabase db = CreateDb();
        List<int> oracle = A.Take(4).Take(2).ToList();
        List<int> actual = db.Table<JawRow>().Where(r => r.Id == 1).Select(r => r.Numbers.Take(4).Take(2)).First().ToList();
        Assert.Equal([5, 3], oracle);
        Assert.Equal(oracle, actual);
    }

    [Fact]
    public void SkipThenTakeThenWhere()
    {
        using TestDatabase db = CreateDb();
        List<int> oracle = A.Skip(1).Take(3).Where(x => x > 2).ToList();
        List<int> actual = db.Table<JawRow>().Where(r => r.Id == 1).Select(r => r.Numbers.Skip(1).Take(3).Where(x => x > 2)).First().ToList();
        Assert.Equal([3, 8], oracle);
        Assert.Equal(oracle, actual);
    }

    [Fact]
    public void SkipThenTakeThenReverse()
    {
        using TestDatabase db = CreateDb();
        List<int> oracle = A.Skip(1).Take(2).Reverse().ToList();
        List<int> actual = db.Table<JawRow>().Where(r => r.Id == 1).Select(r => r.Numbers.Skip(1).Take(2).Reverse()).First().ToList();
        Assert.Equal([8, 3], oracle);
        Assert.Equal(oracle, actual);
    }

    [Fact]
    public void TakeThenDistinct()
    {
        using TestDatabase db = CreateDb();
        List<int> oracle = B.Take(4).Distinct().ToList();
        List<int> actual = db.Table<JawRow>().Where(r => r.Id == 2).Select(r => r.Numbers.Take(4).Distinct()).First().ToList();
        Assert.Equal([3, 1, 2], oracle);
        Assert.Equal(oracle, actual);
    }

    [Fact]
    public void PlainReverse_NoWindow_StillWorks()
    {
        using TestDatabase db = CreateDb();
        List<int> oracle = A.Select(x => x).Reverse().ToList();
        List<int> actual = db.Table<JawRow>().Where(r => r.Id == 1).Select(r => r.Numbers.Select(x => x).Reverse()).First().ToList();
        Assert.Equal([2, 9, 1, 8, 3, 5], oracle);
        Assert.Equal(oracle, actual);
    }

    [Fact]
    public void PlainWhere_NoWindow_StillWorks()
    {
        using TestDatabase db = CreateDb();
        List<int> oracle = A.Where(x => x > 2).ToList();
        List<int> actual = db.Table<JawRow>().Where(r => r.Id == 1).Select(r => r.Numbers.Where(x => x > 2)).First().ToList();
        Assert.Equal([5, 3, 8, 9], oracle);
        Assert.Equal(oracle, actual);
    }
}
