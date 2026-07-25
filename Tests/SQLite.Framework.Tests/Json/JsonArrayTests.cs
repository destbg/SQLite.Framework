using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using SQLite.Framework;
using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

file sealed class NumbersRow
{
    [Key]
    public int Id { get; set; }

    public List<int> Numbers { get; set; } = [];
}

public class JsonArrayTests
{
    private static TestDatabase NumbersDb()
    {
        return new TestDatabase(b => b.TypeConverters[typeof(List<int>)] =
            new SQLiteJsonConverter<List<int>>(TestJsonContext.Default.ListInt32));
    }

    [Fact]
    public void SelectThenDistinctThenReverse_KeepsProjection()
    {
        using TestDatabase db = NumbersDb();
        db.Table<NumbersRow>().Schema.CreateTable();
        db.Table<NumbersRow>().Add(new NumbersRow { Id = 1, Numbers = [5, 3, 5, 8, 3, 3] });

        List<int> oracle = new List<int> { 5, 3, 5, 8, 3, 3 }.Select(x => x * 10).Distinct().Reverse().ToList();
        Assert.Equal([80, 30, 50], oracle);

        List<int> actual = db.Table<NumbersRow>()
            .Select(r => r.Numbers.Select(x => x * 10).Distinct().Reverse().ToList())
            .First();

        Assert.Equal(oracle, actual);
    }

    [Fact]
    public void SelectThenReverseThenDistinct_KeepsProjection()
    {
        using TestDatabase db = NumbersDb();
        db.Table<NumbersRow>().Schema.CreateTable();
        db.Table<NumbersRow>().Add(new NumbersRow { Id = 1, Numbers = [5, 3, 5, 8, 3, 3] });

        List<int> oracle = new List<int> { 5, 3, 5, 8, 3, 3 }.Select(x => x * 10).Reverse().Distinct().ToList();
        Assert.Equal([30, 80, 50], oracle);

        List<int> actual = db.Table<NumbersRow>()
            .Select(r => r.Numbers.Select(x => x * 10).Reverse().Distinct().ToList())
            .First();

        Assert.Equal(oracle, actual);
    }
}