using System.ComponentModel.DataAnnotations;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

internal sealed class JsonDatesRow
{
    [Key]
    public int Id { get; set; }

    public List<DateTime> Dates { get; set; } = [];
}

public class JsonDateTimeElementPredicateTests
{
    private static readonly List<DateTime> Seed =
    [
        new DateTime(2023, 6, 1),
        new DateTime(2024, 1, 15),
        new DateTime(2024, 3, 20)
    ];

    private static TestDatabase SetupDatabase()
    {
        TestDatabase db = new(b => b.TypeConverters[typeof(List<DateTime>)] =
            new SQLiteJsonConverter<List<DateTime>>(TestJsonContext.Default.ListDateTime));
        db.Table<JsonDatesRow>().Schema.CreateTable();
        db.Table<JsonDatesRow>().Add(new JsonDatesRow { Id = 1, Dates = Seed });
        return db;
    }

    [Fact]
    public void GreaterThanPredicateOnTextDatesDivergesFromMemory()
    {
        using TestDatabase db = SetupDatabase();
        DateTime cutoff = new(2023, 12, 31);

        int expected = Seed.Count(d => d > cutoff);

        Assert.Equal(2, expected);

        int actual = db.Table<JsonDatesRow>()
            .Select(r => r.Dates.Count(d => d > cutoff))
            .First();

        Assert.Equal(3, actual);
    }

    [Fact]
    public void ContainsOnTextDatesDivergesFromMemory()
    {
        using TestDatabase db = SetupDatabase();
        DateTime sought = new(2023, 6, 1);

        bool expected = Seed.Contains(sought);

        Assert.True(expected);

        bool actual = db.Table<JsonDatesRow>()
            .Select(r => r.Dates.Contains(sought))
            .First();

        Assert.False(actual);
    }

    [Fact]
    public void YearPredicateOnTextDatesDivergesFromMemory()
    {
        using TestDatabase db = SetupDatabase();

        int expected = Seed.Count(d => d.Year >= 2024);

        Assert.Equal(2, expected);

        int actual = db.Table<JsonDatesRow>()
            .Select(r => r.Dates.Count(d => d.Year >= 2024))
            .First();

        Assert.Equal(0, actual);
    }
}
