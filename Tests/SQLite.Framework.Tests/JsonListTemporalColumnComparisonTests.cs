using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[JsonSerializable(typeof(List<DateTime>))]
internal partial class TemporalColumnListContext : JsonSerializerContext;

internal sealed class TemporalColumnListRow
{
    [Key]
    public int Id { get; set; }

    public DateTime CreatedAt { get; set; }

    public List<DateTime> Dates { get; set; } = [];
}

public class JsonListTemporalColumnComparisonTests
{
    private static readonly DateTime Stamp = new(2024, 3, 4, 5, 6, 7);

    private static TestDatabase Seed(out List<TemporalColumnListRow> mem)
    {
        mem =
        [
            new() { Id = 1, CreatedAt = Stamp, Dates = [Stamp, new DateTime(2024, 1, 1)] },
            new() { Id = 2, CreatedAt = Stamp, Dates = [new DateTime(2024, 1, 1)] },
        ];
        TestDatabase db = new(b => b.AddJsonContext(TemporalColumnListContext.Default));
        db.Table<TemporalColumnListRow>().Schema.CreateTable();
        db.Table<TemporalColumnListRow>().Add(new TemporalColumnListRow { Id = 1, CreatedAt = Stamp, Dates = [Stamp, new DateTime(2024, 1, 1)] });
        db.Table<TemporalColumnListRow>().Add(new TemporalColumnListRow { Id = 2, CreatedAt = Stamp, Dates = [new DateTime(2024, 1, 1)] });
        return db;
    }

    [Fact]
    public void ContainsColumnValueDoesNotMatch()
    {
        using TestDatabase db = Seed(out List<TemporalColumnListRow> mem);

        List<int> memory = mem.Where(r => r.Dates.Contains(r.CreatedAt)).Select(r => r.Id).ToList();
        Assert.Equal([1], memory);

        List<int> actual = db.Table<TemporalColumnListRow>().Where(r => r.Dates.Contains(r.CreatedAt)).Select(r => r.Id).ToList();

        Assert.Equal([], actual);
    }

    [Fact]
    public void FirstElementEqualsColumnDoesNotMatch()
    {
        using TestDatabase db = Seed(out List<TemporalColumnListRow> mem);

        List<bool> memory = mem.OrderBy(r => r.Id).Select(r => r.Dates.First() == r.CreatedAt).ToList();
        Assert.Equal([true, false], memory);

        List<bool> actual = db.Table<TemporalColumnListRow>().OrderBy(r => r.Id).Select(r => r.Dates.First() == r.CreatedAt).ToList();

        Assert.Equal([false, false], actual);
    }
}
