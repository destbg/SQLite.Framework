using System.ComponentModel.DataAnnotations;
using SQLite.Framework;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

file sealed class JsonProjRow
{
    [Key]
    public int Id { get; set; }

    public List<int> Numbers { get; set; } = [];
}

public class JsonSelectProjectionThenFilterTests
{
    private static TestDatabase CreateDb(params int[] numbers)
    {
        TestDatabase db = new(b =>
            b.TypeConverters[typeof(List<int>)] =
                new SQLiteJsonConverter<List<int>>(TestJsonContext.Default.ListInt32));
        db.Table<JsonProjRow>().Schema.CreateTable();
        db.Table<JsonProjRow>().Add(new JsonProjRow { Id = 1, Numbers = numbers.ToList() });
        return db;
    }

    [Fact]
    public void SelectThenWhere_CarriesProjection()
    {
        using TestDatabase db = CreateDb(1, 2, 3);

        List<int> expected = new[] { 1, 2, 3 }.Select(x => x * 2).Where(v => v > 5).ToList();
        List<int> actual = db.Table<JsonProjRow>()
            .Select(r => r.Numbers.Select(x => x * 2).Where(v => v > 5).ToList())
            .First();

        Assert.Equal([6], expected);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void SelectThenContains_True_CarriesProjection()
    {
        using TestDatabase db = CreateDb(1, 2, 3);

        bool expected = new[] { 1, 2, 3 }.Select(x => x * 2).Contains(6);
        bool actual = db.Table<JsonProjRow>().Select(r => r.Numbers.Select(x => x * 2).Contains(6)).First();

        Assert.True(expected);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void SelectThenContains_False_CarriesProjection()
    {
        using TestDatabase db = CreateDb(1, 2, 3);

        bool expected = new[] { 1, 2, 3 }.Select(x => x * 2).Contains(5);
        bool actual = db.Table<JsonProjRow>().Select(r => r.Numbers.Select(x => x * 2).Contains(5)).First();

        Assert.False(expected);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ChainedSelectThenWhere_CarriesBothProjections()
    {
        using TestDatabase db = CreateDb(1, 2, 3);

        List<int> expected = new[] { 1, 2, 3 }.Select(x => x * 2).Select(y => y + 1).Where(v => v > 4).ToList();
        List<int> actual = db.Table<JsonProjRow>()
            .Select(r => r.Numbers.Select(x => x * 2).Select(y => y + 1).Where(v => v > 4).ToList())
            .First();

        Assert.Equal([5, 7], expected);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void SelectWithCapturedFactorThenWhere_CarriesProjectionAndParameter()
    {
        using TestDatabase db = CreateDb(1, 2, 3);
        int factor = 10;

        List<int> expected = new[] { 1, 2, 3 }.Select(x => x * factor).Where(v => v >= 20).ToList();
        List<int> actual = db.Table<JsonProjRow>()
            .Select(r => r.Numbers.Select(x => x * factor).Where(v => v >= 20).ToList())
            .First();

        Assert.Equal([20, 30], expected);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void WhereThenSelect_StillCorrect()
    {
        using TestDatabase db = CreateDb(1, 2, 3);

        List<int> expected = new[] { 1, 2, 3 }.Where(x => x > 1).Select(x => x * 2).ToList();
        List<int> actual = db.Table<JsonProjRow>()
            .Select(r => r.Numbers.Where(x => x > 1).Select(x => x * 2).ToList())
            .First();

        Assert.Equal([4, 6], expected);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void SelectThenSum_CarriesProjection()
    {
        using TestDatabase db = CreateDb(1, 2, 3);

        int expected = new[] { 1, 2, 3 }.Select(x => x * 2).Sum();
        int actual = db.Table<JsonProjRow>().Select(r => r.Numbers.Select(x => x * 2).Sum()).First();

        Assert.Equal(12, expected);
        Assert.Equal(expected, actual);
    }
}
