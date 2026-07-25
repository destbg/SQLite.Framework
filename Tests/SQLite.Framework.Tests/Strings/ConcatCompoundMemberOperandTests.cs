using System.ComponentModel.DataAnnotations;
using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

internal sealed class ConcatPieceRow
{
    [Key]
    public int Id { get; set; }

    public required string Name { get; set; }

    public TimeSpan Span { get; set; }
}

public class ConcatCompoundMemberOperandTests
{
    private static List<ConcatPieceRow> Rows() =>
    [
        new() { Id = 1, Name = "banana", Span = new TimeSpan(5, 0, 0) },
        new() { Id = 2, Name = "xyz", Span = new TimeSpan(2, 0, 0) },
    ];

    private static TestDatabase Seed()
    {
        TestDatabase db = new();
        db.Table<ConcatPieceRow>().Schema.CreateTable();
        db.Table<ConcatPieceRow>().AddRange(Rows());
        return db;
    }

    [Fact]
    public void IndexOfResultConcatenatedWithSuffix()
    {
        using TestDatabase db = Seed();

        List<string> expected = Rows().OrderBy(r => r.Id).Select(r => r.Name.IndexOf("n") + "!").ToList();
        Assert.Equal(["2!", "-1!"], expected);

        List<string> actual = db.Table<ConcatPieceRow>().OrderBy(r => r.Id).Select(r => r.Name.IndexOf("n") + "!").ToList();
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void SpanHoursConcatenatedWithSuffix()
    {
        using TestDatabase db = Seed();

        List<string> expected = Rows().OrderBy(r => r.Id).Select(r => r.Span.Hours + ":").ToList();
        Assert.Equal(["5:", "2:"], expected);

        List<string> actual = db.Table<ConcatPieceRow>().OrderBy(r => r.Id).Select(r => r.Span.Hours + ":").ToList();
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ContainsResultConcatenatedWithSuffixKeepsStoredForm()
    {
        using TestDatabase db = Seed();

        List<string> inMemory = Rows().OrderBy(r => r.Id).Select(r => r.Name.Contains("an") + "!").ToList();
        Assert.Equal(["True!", "False!"], inMemory);

        List<string> actual = db.Table<ConcatPieceRow>().OrderBy(r => r.Id).Select(r => r.Name.Contains("an") + "!").ToList();
        Assert.Equal(["1!", "0!"], actual);
    }

    [Fact]
    public void ExecuteUpdateSetFromIndexOfConcat()
    {
        using TestDatabase db = Seed();

        List<string> expected = Rows().OrderBy(r => r.Id).Select(r => r.Name.IndexOf("n") + "!").ToList();
        Assert.Equal(["2!", "-1!"], expected);

        db.Table<ConcatPieceRow>().ExecuteUpdate(s => s.Set(x => x.Name, x => x.Name.IndexOf("n") + "!"));

        List<string> actual = db.Table<ConcatPieceRow>().OrderBy(r => r.Id).Select(r => r.Name).ToList();
        Assert.Equal(expected, actual);
    }
}
