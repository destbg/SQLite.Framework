using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using SQLite.Framework;
using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

file sealed class JsonCompRow
{
    [Key]
    public int Id { get; set; }

    public List<int> Numbers { get; set; } = [];
}

file sealed class JsonNestedCompRow
{
    [Key]
    public int Id { get; set; }

    public List<PersonWithTags> People { get; set; } = [];
}

public class JsonWindowCompositionTests
{
    private static readonly List<int> A = [10, 20, 30, 40, 50, 60, 70, 80, 90];
    private static readonly List<int> B = [3, 1, 3, 2, 1, 3, 2];

    private static readonly PersonWithTags[] People =
    [
        new PersonWithTags { Name = "Alice", Tags = ["a", "b"] },
        new PersonWithTags { Name = "Bob", Tags = ["c", "d", "e"] },
    ];

    private static TestDatabase CreateDb()
    {
        TestDatabase db = new(b =>
            b.TypeConverters[typeof(List<int>)] =
                new SQLiteJsonConverter<List<int>>(TestJsonContext.Default.ListInt32));
        db.Table<JsonCompRow>().Schema.CreateTable();
        db.Table<JsonCompRow>().Add(new JsonCompRow { Id = 1, Numbers = [.. A] });
        db.Table<JsonCompRow>().Add(new JsonCompRow { Id = 2, Numbers = [.. B] });
        return db;
    }

    private static TestDatabase CreateNestedDb()
    {
        TestDatabase db = new(b =>
        {
            b.TypeConverters[typeof(List<PersonWithTags>)] =
                new SQLiteJsonConverter<List<PersonWithTags>>(TestJsonContext.Default.ListPersonWithTags);
            b.TypeConverters[typeof(List<string>)] =
                new SQLiteJsonConverter<List<string>>(TestJsonContext.Default.ListString);
        });
        db.Table<JsonNestedCompRow>().Schema.CreateTable();
        db.Table<JsonNestedCompRow>().Add(new JsonNestedCompRow { Id = 1, People = People.ToList() });
        return db;
    }

    [Fact]
    public void SkipThenElementAt()
    {
        using TestDatabase db = CreateDb();
        int oracle = A.Skip(2).ElementAt(3);
        int actual = db.Table<JsonCompRow>().Where(r => r.Id == 1).Select(r => r.Numbers.Skip(2).ElementAt(3)).First();
        Assert.Equal(oracle, actual);
    }

    [Fact]
    public void SkipThenElementAtFirstOfRemaining()
    {
        using TestDatabase db = CreateDb();
        int oracle = A.Skip(4).ElementAt(0);
        int actual = db.Table<JsonCompRow>().Where(r => r.Id == 1).Select(r => r.Numbers.Skip(4).ElementAt(0)).First();
        Assert.Equal(oracle, actual);
    }

    [Fact]
    public void TakeThenElementAt()
    {
        using TestDatabase db = CreateDb();
        int oracle = A.Take(5).ElementAt(2);
        int actual = db.Table<JsonCompRow>().Where(r => r.Id == 1).Select(r => r.Numbers.Take(5).ElementAt(2)).First();
        Assert.Equal(oracle, actual);
    }

    [Fact]
    public void SkipThenTakeThenElementAt()
    {
        using TestDatabase db = CreateDb();
        int oracle = A.Skip(1).Take(5).ElementAt(2);
        int actual = db.Table<JsonCompRow>().Where(r => r.Id == 1).Select(r => r.Numbers.Skip(1).Take(5).ElementAt(2)).First();
        Assert.Equal(oracle, actual);
    }

    [Fact]
    public void TakeThenSkipThenElementAt()
    {
        using TestDatabase db = CreateDb();
        int oracle = A.Take(6).Skip(2).ElementAt(1);
        int actual = db.Table<JsonCompRow>().Where(r => r.Id == 1).Select(r => r.Numbers.Take(6).Skip(2).ElementAt(1)).First();
        Assert.Equal(oracle, actual);
    }

    [Fact]
    public void SkipThenSkipThenElementAt()
    {
        using TestDatabase db = CreateDb();
        int oracle = A.Skip(2).Skip(3).ElementAt(1);
        int actual = db.Table<JsonCompRow>().Where(r => r.Id == 1).Select(r => r.Numbers.Skip(2).Skip(3).ElementAt(1)).First();
        Assert.Equal(oracle, actual);
    }

    [Fact]
    public void OrderByDescendingThenElementAt()
    {
        using TestDatabase db = CreateDb();
        int oracle = A.OrderByDescending(x => x).ElementAt(2);
        int actual = db.Table<JsonCompRow>().Where(r => r.Id == 1).Select(r => r.Numbers.OrderByDescending(x => x).ElementAt(2)).First();
        Assert.Equal(oracle, actual);
    }

    [Fact]
    public void OrderByThenSkipThenElementAt()
    {
        using TestDatabase db = CreateDb();
        int oracle = A.OrderBy(x => x).Skip(2).ElementAt(1);
        int actual = db.Table<JsonCompRow>().Where(r => r.Id == 1).Select(r => r.Numbers.OrderBy(x => x).Skip(2).ElementAt(1)).First();
        Assert.Equal(oracle, actual);
    }

    [Fact]
    public void WhereThenElementAt()
    {
        using TestDatabase db = CreateDb();
        int oracle = A.Where(x => x > 30).ElementAt(1);
        int actual = db.Table<JsonCompRow>().Where(r => r.Id == 1).Select(r => r.Numbers.Where(x => x > 30).ElementAt(1)).First();
        Assert.Equal(oracle, actual);
    }

    [Fact]
    public void SkipThenWhereThenElementAt()
    {
        using TestDatabase db = CreateDb();
        int oracle = A.Skip(1).Where(x => x > 30).ElementAt(1);
        int actual = db.Table<JsonCompRow>().Where(r => r.Id == 1).Select(r => r.Numbers.Skip(1).Where(x => x > 30).ElementAt(1)).First();
        Assert.Equal(oracle, actual);
    }

    [Fact]
    public void ReverseThenElementAt()
    {
        using TestDatabase db = CreateDb();
        int oracle = A.Select(x => x).Reverse().ElementAt(2);
        int actual = db.Table<JsonCompRow>().Where(r => r.Id == 1).Select(r => r.Numbers.Select(x => x).Reverse().ElementAt(2)).First();
        Assert.Equal(oracle, actual);
    }

    [Fact]
    public void SkipThenReverseThenElementAt()
    {
        using TestDatabase db = CreateDb();
        int oracle = A.Skip(2).Reverse().ElementAt(1);
        int actual = db.Table<JsonCompRow>().Where(r => r.Id == 1).Select(r => r.Numbers.Skip(2).Reverse().ElementAt(1)).First();
        Assert.Equal(oracle, actual);
    }

    [Fact]
    public void TakeThenReverseThenElementAt()
    {
        using TestDatabase db = CreateDb();
        int oracle = A.Take(4).Reverse().ElementAt(1);
        int actual = db.Table<JsonCompRow>().Where(r => r.Id == 1).Select(r => r.Numbers.Take(4).Reverse().ElementAt(1)).First();
        Assert.Equal(oracle, actual);
    }

    [Fact]
    public void SkipThenElementAtWithVariableIndex()
    {
        using TestDatabase db = CreateDb();
        int idx = 3;
        int oracle = A.Skip(2).ElementAt(idx);
        int actual = db.Table<JsonCompRow>().Where(r => r.Id == 1).Select(r => r.Numbers.Skip(2).ElementAt(idx)).First();
        Assert.Equal(oracle, actual);
    }

    [Fact]
    public void DistinctThenElementAt()
    {
        using TestDatabase db = CreateDb();
        int oracle = B.Distinct().ElementAt(1);
        int actual = db.Table<JsonCompRow>().Where(r => r.Id == 2).Select(r => r.Numbers.Distinct().ElementAt(1)).First();
        Assert.Equal(oracle, actual);
    }

    [Fact]
    public void DistinctThenElementAtLast()
    {
        using TestDatabase db = CreateDb();
        int oracle = B.Distinct().ElementAt(2);
        int actual = db.Table<JsonCompRow>().Where(r => r.Id == 2).Select(r => r.Numbers.Distinct().ElementAt(2)).First();
        Assert.Equal(oracle, actual);
    }

    [Fact]
    public void DistinctThenFirst()
    {
        using TestDatabase db = CreateDb();
        int oracle = B.Distinct().First();
        int actual = db.Table<JsonCompRow>().Where(r => r.Id == 2).Select(r => r.Numbers.Distinct().First()).First();
        Assert.Equal(oracle, actual);
    }

    [Fact]
    public void TakeThenDistinctThenElementAt()
    {
        using TestDatabase db = CreateDb();
        int oracle = B.Take(4).Distinct().ElementAt(1);
        int actual = db.Table<JsonCompRow>().Where(r => r.Id == 2).Select(r => r.Numbers.Take(4).Distinct().ElementAt(1)).First();
        Assert.Equal(oracle, actual);
    }

    [Fact]
    public void SkipThenDistinctThenElementAt()
    {
        using TestDatabase db = CreateDb();
        int oracle = B.Skip(1).Distinct().ElementAt(1);
        int actual = db.Table<JsonCompRow>().Where(r => r.Id == 2).Select(r => r.Numbers.Skip(1).Distinct().ElementAt(1)).First();
        Assert.Equal(oracle, actual);
    }

    [Fact]
    public void DistinctThenSkipThenElementAt()
    {
        using TestDatabase db = CreateDb();
        int oracle = B.Distinct().Skip(1).ElementAt(0);
        int actual = db.Table<JsonCompRow>().Where(r => r.Id == 2).Select(r => r.Numbers.Distinct().Skip(1).ElementAt(0)).First();
        Assert.Equal(oracle, actual);
    }

    [Fact]
    public void DistinctThenTakeSequence()
    {
        using TestDatabase db = CreateDb();
        List<int> oracle = B.Distinct().Take(2).ToList();
        List<int> actual = db.Table<JsonCompRow>().Where(r => r.Id == 2).Select(r => r.Numbers.Distinct().Take(2)).First().ToList();
        Assert.Equal(oracle, actual);
    }

    [Fact]
    public void SkipThenTakeThenSum()
    {
        using TestDatabase db = CreateDb();
        int oracle = A.Skip(2).Take(3).Sum();
        int actual = db.Table<JsonCompRow>().Where(r => r.Id == 1).Select(r => r.Numbers.Skip(2).Take(3).Sum()).First();
        Assert.Equal(oracle, actual);
    }

    [Fact]
    public void SkipThenTakeThenMin()
    {
        using TestDatabase db = CreateDb();
        int oracle = A.Skip(2).Take(3).Min();
        int actual = db.Table<JsonCompRow>().Where(r => r.Id == 1).Select(r => r.Numbers.Skip(2).Take(3).Min()).First();
        Assert.Equal(oracle, actual);
    }

    [Fact]
    public void SkipThenTakeThenMax()
    {
        using TestDatabase db = CreateDb();
        int oracle = A.Skip(2).Take(3).Max();
        int actual = db.Table<JsonCompRow>().Where(r => r.Id == 1).Select(r => r.Numbers.Skip(2).Take(3).Max()).First();
        Assert.Equal(oracle, actual);
    }

    [Fact]
    public void SkipThenTakeThenCount()
    {
        using TestDatabase db = CreateDb();
        int oracle = A.Skip(2).Take(3).Count();
        int actual = db.Table<JsonCompRow>().Where(r => r.Id == 1).Select(r => r.Numbers.Skip(2).Take(3).Count()).First();
        Assert.Equal(oracle, actual);
    }

    [Fact]
    public void SkipThenTakeThenAny()
    {
        using TestDatabase db = CreateDb();
        bool oracle = A.Skip(2).Take(3).Any(x => x > 45);
        bool actual = db.Table<JsonCompRow>().Where(r => r.Id == 1).Select(r => r.Numbers.Skip(2).Take(3).Any(x => x > 45)).First();
        Assert.Equal(oracle, actual);
    }

    [Fact]
    public void SkipThenTakeThenAll()
    {
        using TestDatabase db = CreateDb();
        bool oracle = A.Skip(2).Take(3).All(x => x > 25);
        bool actual = db.Table<JsonCompRow>().Where(r => r.Id == 1).Select(r => r.Numbers.Skip(2).Take(3).All(x => x > 25)).First();
        Assert.Equal(oracle, actual);
    }

    [Fact]
    public void SkipThenTakeThenContains()
    {
        using TestDatabase db = CreateDb();
        bool oracle = A.Skip(2).Take(3).Contains(40);
        bool actual = db.Table<JsonCompRow>().Where(r => r.Id == 1).Select(r => r.Numbers.Skip(2).Take(3).Contains(40)).First();
        Assert.Equal(oracle, actual);
    }

    [Fact]
    public void SkipThenFirst()
    {
        using TestDatabase db = CreateDb();
        int oracle = A.Skip(7).First();
        int actual = db.Table<JsonCompRow>().Where(r => r.Id == 1).Select(r => r.Numbers.Skip(7).First()).First();
        Assert.Equal(oracle, actual);
    }

    [Fact]
    public void SkipThenTakeThenFirst()
    {
        using TestDatabase db = CreateDb();
        int oracle = A.Skip(2).Take(3).First();
        int actual = db.Table<JsonCompRow>().Where(r => r.Id == 1).Select(r => r.Numbers.Skip(2).Take(3).First()).First();
        Assert.Equal(oracle, actual);
    }

    [Fact]
    public void SkipThenTakeThenLast()
    {
        using TestDatabase db = CreateDb();
        int oracle = A.Skip(2).Take(3).Last();
        int actual = db.Table<JsonCompRow>().Where(r => r.Id == 1).Select(r => r.Numbers.Skip(2).Take(3).Last()).First();
        Assert.Equal(oracle, actual);
    }

    [Fact]
    public void SkipThenLastPredicate()
    {
        using TestDatabase db = CreateDb();
        int oracle = A.Skip(1).Last(x => x < 70);
        int actual = db.Table<JsonCompRow>().Where(r => r.Id == 1).Select(r => r.Numbers.Skip(1).Last(x => x < 70)).First();
        Assert.Equal(oracle, actual);
    }

    [Fact]
    public void SkipThenTakeThenWhereSequence()
    {
        using TestDatabase db = CreateDb();
        List<int> oracle = A.Skip(1).Take(4).Where(x => x % 20 == 0).ToList();
        List<int> actual = db.Table<JsonCompRow>().Where(r => r.Id == 1).Select(r => r.Numbers.Skip(1).Take(4).Where(x => x % 20 == 0)).First().ToList();
        Assert.Equal(oracle, actual);
    }

    [Fact]
    public void OrderByDescendingThenSkipThenTakeSequence()
    {
        using TestDatabase db = CreateDb();
        List<int> oracle = A.OrderByDescending(x => x).Skip(2).Take(3).ToList();
        List<int> actual = db.Table<JsonCompRow>().Where(r => r.Id == 1).Select(r => r.Numbers.OrderByDescending(x => x).Skip(2).Take(3)).First().ToList();
        Assert.Equal(oracle, actual);
    }

    [Fact]
    public void SelectManyThenSkipThenElementAt()
    {
        using TestDatabase db = CreateNestedDb();
        string oracle = People.SelectMany(p => p.Tags).Skip(1).ElementAt(2);
        string actual = db.Table<JsonNestedCompRow>().Where(r => r.Id == 1).Select(r => r.People.SelectMany(p => p.Tags).Skip(1).ElementAt(2)).First();
        Assert.Equal(oracle, actual);
    }

    [Fact]
    public void SelectManyThenSkipThenFirst()
    {
        using TestDatabase db = CreateNestedDb();
        string oracle = People.SelectMany(p => p.Tags).Skip(2).First();
        string actual = db.Table<JsonNestedCompRow>().Where(r => r.Id == 1).Select(r => r.People.SelectMany(p => p.Tags).Skip(2).First()).First();
        Assert.Equal(oracle, actual);
    }

    [Fact]
    public void SelectManyThenTakeThenElementAt()
    {
        using TestDatabase db = CreateNestedDb();
        string oracle = People.SelectMany(p => p.Tags).Take(3).ElementAt(1);
        string actual = db.Table<JsonNestedCompRow>().Where(r => r.Id == 1).Select(r => r.People.SelectMany(p => p.Tags).Take(3).ElementAt(1)).First();
        Assert.Equal(oracle, actual);
    }
}
