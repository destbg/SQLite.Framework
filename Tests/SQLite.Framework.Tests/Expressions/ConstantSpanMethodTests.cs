using System.Collections;
using System.ComponentModel.DataAnnotations;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

internal sealed class SpanMethodRow
{
    [Key]
    public int Id { get; set; }

    public string Name { get; set; } = "";
}

internal sealed class EnumerableSpanSource : IEnumerable<string>
{
    private readonly string[] values;

    public EnumerableSpanSource(params string[] values)
    {
        this.values = values;
    }

    public IEnumerator<string> GetEnumerator()
    {
        return ((IEnumerable<string>)values).GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public static implicit operator ReadOnlySpan<string>(EnumerableSpanSource source)
    {
        return source.values;
    }
}

public class ConstantSpanMethodTests
{
    [Fact]
    public void SpanIndexOfOverConstantCollectionIsEvaluated()
    {
        using TestDatabase db = new();
        db.Table<SpanMethodRow>().Schema.CreateTable();
        db.Table<SpanMethodRow>().Add(new SpanMethodRow { Id = 1, Name = "a" });

        string[] captured = ["a", "b"];

        List<SpanMethodRow> expected = new[] { new SpanMethodRow { Id = 1, Name = "a" } }
            .Where(r => r.Id > 0 && captured.IndexOf("a") >= 0)
            .ToList();
        List<SpanMethodRow> actual = db.Table<SpanMethodRow>()
            .Where(r => r.Id > 0 && captured.IndexOf("a") >= 0)
            .ToList();

        Assert.Equal(expected.Select(r => r.Id), actual.Select(r => r.Id));
    }

    [Fact]
    public void SpanIndexOfOverEnumerableHandlesFoundAndMissingValues()
    {
        using TestDatabase db = new();
        db.Table<SpanMethodRow>().Schema.CreateTable();
        SpanMethodRow[] rows = [new SpanMethodRow { Id = 1, Name = "a" }];
        db.Table<SpanMethodRow>().AddRange(rows);
        EnumerableSpanSource captured = new("a", "b");

        List<int> expected = rows
            .Where(r => MemoryExtensions.IndexOf<string>(captured, "a") >= 0
                && MemoryExtensions.IndexOf<string>(captured, "z") < 0)
            .Select(r => r.Id)
            .ToList();
        List<int> actual = db.Table<SpanMethodRow>()
            .Where(r => MemoryExtensions.IndexOf<string>(captured, "a") >= 0
                && MemoryExtensions.IndexOf<string>(captured, "z") < 0)
            .Select(r => r.Id)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void SpanIndexOfOverEnumerableProjectsTheConstantResult()
    {
        using TestDatabase db = new();
        db.Table<SpanMethodRow>().Schema.CreateTable();
        SpanMethodRow[] rows =
        [
            new SpanMethodRow { Id = 1, Name = "a" },
            new SpanMethodRow { Id = 2, Name = "b" }
        ];
        db.Table<SpanMethodRow>().AddRange(rows);
        EnumerableSpanSource captured = new("a", "b");

        List<int> expected = rows
            .Select(_ => MemoryExtensions.IndexOf<string>(captured, "b"))
            .ToList();
        List<int> actual = db.Table<SpanMethodRow>()
            .OrderBy(r => r.Id)
            .Select(_ => MemoryExtensions.IndexOf<string>(captured, "b"))
            .ToList();

        Assert.Equal([1, 1], expected);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void SpanIndexOfWithRowValueIsNotEvaluatedLocally()
    {
        using TestDatabase db = new();
        db.Table<SpanMethodRow>().Schema.CreateTable();
        string[] captured = ["a", "b"];

        Assert.Throws<NotSupportedException>(() => db.Table<SpanMethodRow>()
            .Where(r => captured.IndexOf(r.Name) >= 0)
            .ToList());
    }

    [Fact]
    public void OtherSpanMethodsKeepTheirUnsupportedError()
    {
        using TestDatabase db = new();
        db.Table<SpanMethodRow>().Schema.CreateTable();
        string[] left = ["a", "b"];
        string[] right = ["a", "b"];

        Assert.Throws<NotSupportedException>(() => db.Table<SpanMethodRow>()
            .Where(r => r.Id > 0 && MemoryExtensions.SequenceEqual<string>(left, right))
            .ToList());
    }

}
