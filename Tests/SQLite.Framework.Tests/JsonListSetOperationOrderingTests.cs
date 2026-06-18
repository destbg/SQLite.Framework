using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using SQLite.Framework;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

file sealed class TwoIntListRow
{
    [Key]
    public int Id { get; set; }

    public List<int> Left { get; set; } = [];

    public List<int> Right { get; set; } = [];
}

public class JsonListSetOperationOrderingTests
{
    private static TestDatabase Seed(List<int> left, List<int> right)
    {
        TestDatabase db = new(b => b.TypeConverters[typeof(List<int>)] =
            new SQLiteJsonConverter<List<int>>(TestJsonContext.Default.ListInt32));
        db.Table<TwoIntListRow>().Schema.CreateTable();
        db.Table<TwoIntListRow>().Add(new TwoIntListRow { Id = 1, Left = left, Right = right });
        return db;
    }

    [Fact]
    public void Union_KeepsFirstAppearanceOrder()
    {
        using TestDatabase db = Seed([3, 1, 2], [2, 5, 4]);
        List<int> expected = new List<int> { 3, 1, 2 }.Union(new List<int> { 2, 5, 4 }).ToList();
        List<int> actual = db.Table<TwoIntListRow>().Select(r => r.Left.Union(r.Right).ToList()).First();
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Intersect_KeepsFirstAppearanceOrder()
    {
        using TestDatabase db = Seed([3, 1, 2, 1], [1, 2, 9]);
        List<int> expected = new List<int> { 3, 1, 2, 1 }.Intersect(new List<int> { 1, 2, 9 }).ToList();
        List<int> actual = db.Table<TwoIntListRow>().Select(r => r.Left.Intersect(r.Right).ToList()).First();
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Except_KeepsFirstAppearanceOrder()
    {
        using TestDatabase db = Seed([3, 1, 2, 1], [2]);
        List<int> expected = new List<int> { 3, 1, 2, 1 }.Except(new List<int> { 2 }).ToList();
        List<int> actual = db.Table<TwoIntListRow>().Select(r => r.Left.Except(r.Right).ToList()).First();
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Distinct_KeepsFirstAppearanceOrder()
    {
        using TestDatabase db = Seed([5, 3, 5, 8, 3, 3], []);
        List<int> expected = new List<int> { 5, 3, 5, 8, 3, 3 }.Distinct().ToList();
        List<int> actual = db.Table<TwoIntListRow>().Select(r => r.Left.Distinct().ToList()).First();
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Concat_KeepsFirstAppearanceOrder()
    {
        using TestDatabase db = Seed([3, 1], [8, 1]);
        List<int> expected = new List<int> { 3, 1 }.Concat(new List<int> { 8, 1 }).ToList();
        List<int> actual = db.Table<TwoIntListRow>().Select(r => r.Left.Concat(r.Right).ToList()).First();
        Assert.Equal(expected, actual);
    }
}
