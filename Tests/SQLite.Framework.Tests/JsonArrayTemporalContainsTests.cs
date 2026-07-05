using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[JsonSerializable(typeof(DateTime[]))]
internal partial class TemporalArrayContext : JsonSerializerContext;

internal sealed class TemporalArrayRow
{
    [Key]
    public int Id { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime[] Dates { get; set; } = [];
}

public class JsonArrayTemporalContainsTests
{
    private static readonly DateTime Sought = new(2024, 5, 6, 7, 8, 9);

    private static TestDatabase Seed(out DateTime[] local)
    {
        local = [Sought, new DateTime(2024, 1, 15)];
        TestDatabase db = new(b => b.AddJsonContext(TemporalArrayContext.Default));
        db.Table<TemporalArrayRow>().Schema.CreateTable();
        db.Table<TemporalArrayRow>().Add(new TemporalArrayRow { Id = 1, CreatedAt = Sought, Dates = [Sought, new DateTime(2024, 1, 15)] });
        return db;
    }

    [Fact]
    public void ArrayContainsDateTimeDefaultStorage()
    {
        using TestDatabase db = Seed(out DateTime[] local);

        bool expected = local.Contains(Sought);
        bool actual = db.Table<TemporalArrayRow>().Select(r => r.Dates.Contains(Sought)).First();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ArrayAnyEqualsDateTimeDefaultStorage()
    {
        using TestDatabase db = Seed(out DateTime[] local);

        bool expected = local.Any(d => d == Sought);
        bool actual = db.Table<TemporalArrayRow>().Select(r => r.Dates.Any(d => d == Sought)).First();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ArrayIndexOfDateTimeDefaultStorage()
    {
        using TestDatabase db = Seed(out DateTime[] local);

        int expected = Array.IndexOf(local, Sought);
        int actual = db.Table<TemporalArrayRow>().Select(r => Array.IndexOf(r.Dates, Sought)).First();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void CapturedArrayContainsColumnMatchesLinqToObjects()
    {
        using TestDatabase db = Seed(out DateTime[] local);

        bool expected = local.Contains(Sought);
        bool actual = db.Table<TemporalArrayRow>().Select(r => local.Contains(r.CreatedAt)).First();

        Assert.Equal(expected, actual);
    }
}
