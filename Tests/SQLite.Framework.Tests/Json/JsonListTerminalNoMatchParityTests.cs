using System.Collections.Generic;
using System.Linq;
using System.ComponentModel.DataAnnotations;
using SQLite.Framework;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

internal sealed class JsonTerminalRow
{
    [Key]
    public int Id { get; set; }

    public List<int> Numbers { get; set; } = new();
}

public class JsonListTerminalNoMatchParityTests
{
    private static TestDatabase Seed()
    {
        TestDatabase db = new(b =>
            b.TypeConverters[typeof(List<int>)] = new SQLiteJsonConverter<List<int>>(TestJsonContext.Default.ListInt32));
        db.Table<JsonTerminalRow>().Schema.CreateTable();
        db.Table<JsonTerminalRow>().Add(new JsonTerminalRow { Id = 1, Numbers = new List<int> { 1, 2, 3 } });
        return db;
    }

    [Fact]
    public void FirstPredicateNoMatch_ReturnsDefault()
    {
        using TestDatabase db = Seed();

        int actual = db.Table<JsonTerminalRow>().Select(r => r.Numbers.First(x => x > 10)).First();

        Assert.Equal(0, actual);
    }

    [Fact]
    public void SinglePredicateNoMatch_ReturnsDefault()
    {
        using TestDatabase db = Seed();

        int actual = db.Table<JsonTerminalRow>().Select(r => r.Numbers.Single(x => x > 10)).First();

        Assert.Equal(0, actual);
    }

    [Fact]
    public void MinOverFilteredEmpty_ReturnsDefault()
    {
        using TestDatabase db = Seed();

        int actual = db.Table<JsonTerminalRow>().Select(r => r.Numbers.Where(n => n > 10).Min()).First();

        Assert.Equal(0, actual);
    }

    [Fact]
    public void MaxOverFilteredEmpty_ReturnsDefault()
    {
        using TestDatabase db = Seed();

        int actual = db.Table<JsonTerminalRow>().Select(r => r.Numbers.Where(n => n > 10).Max()).First();

        Assert.Equal(0, actual);
    }
}
