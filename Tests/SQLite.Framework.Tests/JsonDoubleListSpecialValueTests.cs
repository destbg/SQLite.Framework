using System.ComponentModel.DataAnnotations;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

internal sealed class JsonDoubleListRow
{
    [Key]
    public int Id { get; set; }

    public List<double> Vals { get; set; } = [];
}

public class JsonDoubleListSpecialValueTests
{
    private static TestDatabase Setup()
    {
        TestDatabase db = new(b => b.TypeConverters[typeof(List<double>)] =
            new SQLiteJsonConverter<List<double>>(TestJsonContext.Default.ListDouble));
        db.Table<JsonDoubleListRow>().Schema.CreateTable();
        return db;
    }

    [Fact]
    public void NaNElementRoundTrips()
    {
        using TestDatabase db = Setup();
        List<double> seed = [1.0, double.NaN];

        db.Table<JsonDoubleListRow>().Add(new JsonDoubleListRow { Id = 1, Vals = seed });
        List<double> actual = db.Table<JsonDoubleListRow>().First().Vals;

        Assert.Equal(seed, actual);
    }

    [Fact]
    public void InfinityElementRoundTrips()
    {
        using TestDatabase db = Setup();
        List<double> seed = [1.0, double.PositiveInfinity];

        db.Table<JsonDoubleListRow>().Add(new JsonDoubleListRow { Id = 1, Vals = seed });
        List<double> actual = db.Table<JsonDoubleListRow>().First().Vals;

        Assert.Equal(seed, actual);
    }
}
