using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using SQLite.Framework.Enums;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[JsonSerializable(typeof(List<TimeSpan>))]
internal partial class SpanAddListContext : JsonSerializerContext;

internal sealed class SpanAddRow
{
    [Key]
    public int Id { get; set; }

    public DateTime When { get; set; }

    public List<TimeSpan> Spans { get; set; } = [];
}

public class JsonSpanTextStorageDateAddTests
{
    [Fact]
    public void AddFirstJsonSpanTextStorage()
    {
        using TestDatabase db = new(b =>
        {
            b.UseTimeSpanStorage(TimeSpanStorageMode.Text);
            b.AddJsonContext(SpanAddListContext.Default);
        });
        db.Table<SpanAddRow>().Schema.CreateTable();
        db.Table<SpanAddRow>().Add(new SpanAddRow { Id = 1, When = new DateTime(2024, 5, 10, 8, 0, 0), Spans = [TimeSpan.FromMinutes(1)] });

        List<SpanAddRow> mem = [new() { Id = 1, When = new DateTime(2024, 5, 10, 8, 0, 0), Spans = [TimeSpan.FromMinutes(1)] }];
        List<DateTime> expected = mem.Select(r => r.When.Add(r.Spans.First())).ToList();
        List<DateTime> actual = db.Table<SpanAddRow>().Select(r => r.When.Add(r.Spans.First())).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void AddFirstJsonSpanInWhereThrows()
    {
        using TestDatabase db = new(b =>
        {
            b.UseTimeSpanStorage(TimeSpanStorageMode.Text);
            b.AddJsonContext(SpanAddListContext.Default);
        });
        db.Table<SpanAddRow>().Schema.CreateTable();
        db.Table<SpanAddRow>().Add(new SpanAddRow { Id = 1, When = new DateTime(2024, 5, 10, 8, 0, 0), Spans = [TimeSpan.FromMinutes(1)] });

        Assert.Throws<NotSupportedException>(() =>
            db.Table<SpanAddRow>().Where(r => r.When.Add(r.Spans.First()) > new DateTime(2024, 5, 10)).ToList());
    }
}
