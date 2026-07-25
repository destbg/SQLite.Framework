using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[JsonSerializable(typeof(List<DateTime>))]
internal partial class TemporalIndexListContext : JsonSerializerContext;

internal sealed class TemporalIndexListRow
{
    [Key]
    public int Id { get; set; }

    public List<DateTime> Dates { get; set; } = [];
}

public class JsonListTemporalIndexOfTests
{
    private static readonly DateTime Sought = new(2023, 6, 1, 10, 20, 30);

    private static TestDatabase Seed(out List<DateTime> local)
    {
        local = [new DateTime(2024, 1, 15), Sought, Sought];
        TestDatabase db = new(b => b.AddJsonContext(TemporalIndexListContext.Default));
        db.Table<TemporalIndexListRow>().Schema.CreateTable();
        db.Table<TemporalIndexListRow>().Add(new TemporalIndexListRow { Id = 1, Dates = [new DateTime(2024, 1, 15), Sought, Sought] });
        return db;
    }

    [Fact]
    public void IndexOfDateTimeDefaultStorage()
    {
        using TestDatabase db = Seed(out List<DateTime> local);

        int expected = local.IndexOf(Sought);
        int actual = db.Table<TemporalIndexListRow>().Select(r => r.Dates.IndexOf(Sought)).First();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void LastIndexOfDateTimeDefaultStorage()
    {
        using TestDatabase db = Seed(out List<DateTime> local);

        int expected = local.LastIndexOf(Sought);
        int actual = db.Table<TemporalIndexListRow>().Select(r => r.Dates.LastIndexOf(Sought)).First();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ListAnyEqualsDateTimeDefaultStorage()
    {
        using TestDatabase db = Seed(out List<DateTime> local);

        bool expected = local.Any(d => d == Sought);
        bool actual = db.Table<TemporalIndexListRow>().Select(r => r.Dates.Any(d => d == Sought)).First();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void FirstElementEqualsMethodDateTimeDefaultStorage()
    {
        using TestDatabase db = Seed(out List<DateTime> local);

        bool expected = local.First().Equals(new DateTime(2024, 1, 15));
        bool actual = db.Table<TemporalIndexListRow>().Select(r => r.Dates.First().Equals(new DateTime(2024, 1, 15))).First();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void DistinctContainsDateTimeDefaultStorage()
    {
        using TestDatabase db = Seed(out List<DateTime> local);

        bool expected = local.Distinct().Contains(Sought);
        bool actual = db.Table<TemporalIndexListRow>().Select(r => r.Dates.Distinct().Contains(Sought)).First();

        Assert.Equal(expected, actual);
    }
}
