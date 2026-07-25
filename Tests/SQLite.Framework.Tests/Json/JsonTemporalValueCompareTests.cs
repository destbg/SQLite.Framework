using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[JsonSerializable(typeof(Dictionary<string, DateTime>))]
[JsonSerializable(typeof(List<DateTime>))]
internal partial class TemporalValueContext : JsonSerializerContext;

internal sealed class DueMapRow
{
    [Key]
    public int Id { get; set; }

    public Dictionary<string, DateTime> Map { get; set; } = [];
}

internal sealed class WhenListRow
{
    [Key]
    public int Id { get; set; }

    public List<DateTime> Whens { get; set; } = [];
}

public class JsonTemporalValueCompareTests
{
    [Fact]
    public void DictionaryIndexerDateTimeEqualityMatchesLinq()
    {
        DateTime due = new(2024, 5, 6, 7, 8, 9);
        List<DueMapRow> local =
        [
            new DueMapRow { Id = 1, Map = new Dictionary<string, DateTime> { ["due"] = due } },
            new DueMapRow { Id = 2, Map = new Dictionary<string, DateTime> { ["due"] = due.AddDays(1) } },
        ];
        using TestDatabase db = new(b => b.AddJsonContext(TemporalValueContext.Default));
        db.Table<DueMapRow>().Schema.CreateTable();
        db.Table<DueMapRow>().AddRange(local);

        int expected = local.Count(r => r.Map["due"] == due);
        int actual = db.Table<DueMapRow>().Count(r => r.Map["due"] == due);

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void GroupKeyDateTimeEqualityMatchesLinq()
    {
        DateTime due = new(2024, 5, 6, 7, 8, 9);
        List<DateTime> whens = [due, due.AddDays(1), due];
        using TestDatabase db = new(b => b.AddJsonContext(TemporalValueContext.Default));
        db.Table<WhenListRow>().Schema.CreateTable();
        db.Table<WhenListRow>().Add(new WhenListRow { Id = 1, Whens = whens });

        int expected = whens.GroupBy(x => x).Where(g => g.Key == due).Count();
        int actual = db.Table<WhenListRow>().Select(r => r.Whens.GroupBy(x => x).Where(g => g.Key == due).Count()).First();

        Assert.Equal(expected, actual);
    }
}
