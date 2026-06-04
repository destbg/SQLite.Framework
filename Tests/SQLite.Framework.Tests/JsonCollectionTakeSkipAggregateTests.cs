using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using SQLite.Framework;
using SQLite.Framework.Enums;
using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Enums;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class JsonCollectionTakeSkipAggregateTests
{
    [Fact]
    public void JsonCollectionTakeSkipAggregate_00()
    {
        using TestDatabase db = new(b => b.TypeConverters[typeof(List<int>)] = new SQLiteJsonConverter<List<int>>(TestJsonContext.Default.ListInt32));
                db.Table<QqIntRow>().Schema.CreateTable();
                db.Table<QqIntRow>().Add(new QqIntRow { Id = 1, Numbers = [1, 2, 3, 4] });
                int oracle = new List<int> { 1, 2, 3, 4 }.Take(2).Sum();
                int actual = db.Table<QqIntRow>().Select(r => r.Numbers.Take(2).Sum()).First();
                Assert.Equal(oracle, actual);
    }

    [Fact]
    public void JsonCollectionTakeSkipAggregate_01()
    {
        using TestDatabase db = new(b => b.TypeConverters[typeof(List<int>)] = new SQLiteJsonConverter<List<int>>(TestJsonContext.Default.ListInt32));
                db.Table<QqIntRow>().Schema.CreateTable();
                db.Table<QqIntRow>().Add(new QqIntRow { Id = 1, Numbers = [1, 2, 3, 4, 5] });
                int oracle = new List<int> { 1, 2, 3, 4, 5 }.Take(2).Count();
                int actual = db.Table<QqIntRow>().Select(r => r.Numbers.Take(2).Count()).First();
                Assert.Equal(oracle, actual);
    }

    [Fact]
    public void JsonCollectionTakeSkipAggregate_02()
    {
        using TestDatabase db = new(b => b.TypeConverters[typeof(List<int>)] = new SQLiteJsonConverter<List<int>>(TestJsonContext.Default.ListInt32));
                db.Table<QqIntRow>().Schema.CreateTable();
                db.Table<QqIntRow>().Add(new QqIntRow { Id = 1, Numbers = [5, 9, 1, 2] });
                int oracle = new List<int> { 5, 9, 1, 2 }.Take(2).Min();
                int actual = db.Table<QqIntRow>().Select(r => r.Numbers.Take(2).Min()).First();
                Assert.Equal(oracle, actual);
    }

    [Fact]
    public void JsonCollectionTakeSkipAggregate_03()
    {
        using TestDatabase db = new(b => b.TypeConverters[typeof(List<int>)] = new SQLiteJsonConverter<List<int>>(TestJsonContext.Default.ListInt32));
                db.Table<QqIntRow>().Schema.CreateTable();
                db.Table<QqIntRow>().Add(new QqIntRow { Id = 1, Numbers = [1, 2, 9, 5] });
                int oracle = new List<int> { 1, 2, 9, 5 }.Take(2).Max();
                int actual = db.Table<QqIntRow>().Select(r => r.Numbers.Take(2).Max()).First();
                Assert.Equal(oracle, actual);
    }

    [Fact]
    public void JsonCollectionTakeSkipAggregate_04()
    {
        using TestDatabase db = new(b => b.TypeConverters[typeof(List<int>)] = new SQLiteJsonConverter<List<int>>(TestJsonContext.Default.ListInt32));
                db.Table<QqIntRow>().Schema.CreateTable();
                db.Table<QqIntRow>().Add(new QqIntRow { Id = 1, Numbers = [2, 4, 100, 200] });
                double oracle = new List<int> { 2, 4, 100, 200 }.Take(2).Average();
                double actual = db.Table<QqIntRow>().Select(r => r.Numbers.Take(2).Average()).First();
                Assert.Equal(oracle, actual);
    }

    [Fact]
    public void JsonCollectionTakeSkipAggregate_05()
    {
        using TestDatabase db = new(b => b.TypeConverters[typeof(List<int>)] = new SQLiteJsonConverter<List<int>>(TestJsonContext.Default.ListInt32));
                db.Table<QqIntRow>().Schema.CreateTable();
                db.Table<QqIntRow>().Add(new QqIntRow { Id = 1, Numbers = [5, 6, 7] });
                bool oracle = new List<int> { 5, 6, 7 }.Take(1).Any(x => x == 7);
                bool actual = db.Table<QqIntRow>().Select(r => r.Numbers.Take(1).Any(x => x == 7)).First();
                Assert.Equal(oracle, actual);
    }

    [Fact]
    public void JsonCollectionTakeSkipAggregate_06()
    {
        using TestDatabase db = new(b => b.TypeConverters[typeof(List<int>)] = new SQLiteJsonConverter<List<int>>(TestJsonContext.Default.ListInt32));
                db.Table<QqIntRow>().Schema.CreateTable();
                db.Table<QqIntRow>().Add(new QqIntRow { Id = 1, Numbers = [2, 4, 5] });
                bool oracle = new List<int> { 2, 4, 5 }.Take(2).All(x => x % 2 == 0);
                bool actual = db.Table<QqIntRow>().Select(r => r.Numbers.Take(2).All(x => x % 2 == 0)).First();
                Assert.Equal(oracle, actual);
    }

    [Fact]
    public void JsonCollectionTakeSkipAggregate_07()
    {
        using TestDatabase db = new(b => b.TypeConverters[typeof(List<int>)] = new SQLiteJsonConverter<List<int>>(TestJsonContext.Default.ListInt32));
                db.Table<QqIntRow>().Schema.CreateTable();
                db.Table<QqIntRow>().Add(new QqIntRow { Id = 1, Numbers = [5, 6, 7] });
                bool oracle = new List<int> { 5, 6, 7 }.Take(1).Contains(7);
                bool actual = db.Table<QqIntRow>().Select(r => r.Numbers.Take(1).Contains(7)).First();
                Assert.Equal(oracle, actual);
    }

    [Fact]
    public void JsonCollectionTakeSkipAggregate_08()
    {
        using TestDatabase db = new(b => b.TypeConverters[typeof(List<int>)] = new SQLiteJsonConverter<List<int>>(TestJsonContext.Default.ListInt32));
                db.Table<QqIntRow>().Schema.CreateTable();
                db.Table<QqIntRow>().Add(new QqIntRow { Id = 1, Numbers = [1, 2, 3, 4] });
                int oracle = new List<int> { 1, 2, 3, 4 }.Skip(2).Sum();
                int actual = db.Table<QqIntRow>().Select(r => r.Numbers.Skip(2).Sum()).First();
                Assert.Equal(oracle, actual);
    }

    [Fact]
    public void JsonCollectionTakeSkipAggregate_09()
    {
        using TestDatabase db = new(b => b.TypeConverters[typeof(List<int>)] = new SQLiteJsonConverter<List<int>>(TestJsonContext.Default.ListInt32));
                db.Table<QqIntRow>().Schema.CreateTable();
                db.Table<QqIntRow>().Add(new QqIntRow { Id = 1, Numbers = [1, 2, 3, 4, 5] });
                int oracle = new List<int> { 1, 2, 3, 4, 5 }.Skip(1).Take(2).Count();
                int actual = db.Table<QqIntRow>().Select(r => r.Numbers.Skip(1).Take(2).Count()).First();
                Assert.Equal(oracle, actual);
    }

    [Fact]
    public void JsonCollectionTakeSkipAggregate_10()
    {
        using TestDatabase db = new(b => b.TypeConverters[typeof(List<int>)] = new SQLiteJsonConverter<List<int>>(TestJsonContext.Default.ListInt32));
                db.Table<QqIntRow>().Schema.CreateTable();
                db.Table<QqIntRow>().Add(new QqIntRow { Id = 1, Numbers = [5, 3, 1, 4, 2] });
                int oracle = new List<int> { 5, 3, 1, 4, 2 }.OrderBy(x => x).Take(3).Last();
                int actual = db.Table<QqIntRow>().Select(r => r.Numbers.OrderBy(x => x).Take(3).Last()).First();
                Assert.Equal(oracle, actual);
    }

    [Fact]
    public void JsonCollectionTakeSkipAggregate_11()
    {
        using TestDatabase db = new(b => b.TypeConverters[typeof(List<string>)] = new SQLiteJsonConverter<List<string>>(TestJsonContext.Default.ListString));
                db.Table<QqStrRow>().Schema.CreateTable();
                db.Table<QqStrRow>().Add(new QqStrRow { Id = 1, Tags = ["a", "b", "a", "c"] });
                List<string> oracle = Enumerable.Reverse(new List<string> { "a", "b", "a", "c" }).Distinct().ToList();
                List<string> actual = db.Table<QqStrRow>().Select(r => Enumerable.Reverse(r.Tags).Distinct().ToList()).First();
                Assert.Equal(oracle, actual);
    }

    [Fact]
    public void JsonCollectionTakeSkipAggregate_12()
    {
        using TestDatabase db = new(b => b.TypeConverters[typeof(List<string>)] = new SQLiteJsonConverter<List<string>>(TestJsonContext.Default.ListString));
                db.Table<QqStrRow>().Schema.CreateTable();
                db.Table<QqStrRow>().Add(new QqStrRow { Id = 1, Tags = ["c", "a", "b", "a"] });
                List<string> oracle = new List<string> { "c", "a", "b", "a" }.Distinct().Reverse().ToList();
                List<string> actual = db.Table<QqStrRow>().Select(r => r.Tags.Distinct().Reverse().ToList()).First();
                Assert.Equal(oracle, actual);
    }

    [Fact]
    public void JsonCollectionTakeSkipAggregate_WhereThenDistinctReverse()
    {
        using TestDatabase db = new(b => b.TypeConverters[typeof(List<string>)] = new SQLiteJsonConverter<List<string>>(TestJsonContext.Default.ListString));
        db.Table<QqStrRow>().Schema.CreateTable();
        db.Table<QqStrRow>().Add(new QqStrRow { Id = 1, Tags = ["a", "b", "a", "c"] });

        List<string> oracle = new List<string> { "a", "b", "a", "c" }.Where(t => t != "b").Distinct().Reverse().ToList();
        List<string> actual = db.Table<QqStrRow>().Select(r => r.Tags.Where(t => t != "b").Distinct().Reverse().ToList()).First();

        Assert.Equal(oracle, actual);
    }

    [Fact]
    public void JsonCollectionTakeSkipAggregate_WhereThenTakeSum()
    {
        using TestDatabase db = new(b => b.TypeConverters[typeof(List<int>)] = new SQLiteJsonConverter<List<int>>(TestJsonContext.Default.ListInt32));
        db.Table<QqIntRow>().Schema.CreateTable();
        db.Table<QqIntRow>().Add(new QqIntRow { Id = 1, Numbers = [1, 2, 3, 4, 5] });

        int oracle = new List<int> { 1, 2, 3, 4, 5 }.Where(n => n > 1).Take(2).Sum();
        int actual = db.Table<QqIntRow>().Select(r => r.Numbers.Where(n => n > 1).Take(2).Sum()).First();

        Assert.Equal(oracle, actual);
    }

    [Fact]
    public void JsonCollectionTakeSkipAggregate_TakeThenLast_NoOrder()
    {
        using TestDatabase db = new(b => b.TypeConverters[typeof(List<int>)] = new SQLiteJsonConverter<List<int>>(TestJsonContext.Default.ListInt32));
        db.Table<QqIntRow>().Schema.CreateTable();
        db.Table<QqIntRow>().Add(new QqIntRow { Id = 1, Numbers = [1, 2, 3, 4, 5] });

        int oracle = new List<int> { 1, 2, 3, 4, 5 }.Take(3).Last();
        int actual = db.Table<QqIntRow>().Select(r => r.Numbers.Take(3).Last()).First();

        Assert.Equal(oracle, actual);
    }

    [Fact]
    public void JsonCollectionTakeSkipAggregate_OrderByDescendingTakeLast()
    {
        using TestDatabase db = new(b => b.TypeConverters[typeof(List<int>)] = new SQLiteJsonConverter<List<int>>(TestJsonContext.Default.ListInt32));
        db.Table<QqIntRow>().Schema.CreateTable();
        db.Table<QqIntRow>().Add(new QqIntRow { Id = 1, Numbers = [5, 3, 1, 4, 2] });

        int oracle = new List<int> { 5, 3, 1, 4, 2 }.OrderByDescending(x => x).Take(3).Last();
        int actual = db.Table<QqIntRow>().Select(r => r.Numbers.OrderByDescending(x => x).Take(3).Last()).First();

        Assert.Equal(oracle, actual);
    }

}
public sealed class QqIntRow
{
    [System.ComponentModel.DataAnnotations.Key]
    public int Id { get; set; }

    public List<int> Numbers { get; set; } = [];
}

public sealed class QqStrRow
{
    [System.ComponentModel.DataAnnotations.Key]
    public int Id { get; set; }

    public List<string> Tags { get; set; } = [];
}
