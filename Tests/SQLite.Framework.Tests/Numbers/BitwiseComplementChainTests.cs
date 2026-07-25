using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("BitComplementChainRow")]
public class BitComplementChainRow
{
    [Key]
    public int Id { get; set; }

    public string Name { get; set; } = "";

    public TimeSpan Span { get; set; }

    public uint Unsigned { get; set; }
}

public class BitwiseComplementChainTests
{
    private static List<BitComplementChainRow> Rows() =>
    [
        new() { Id = 1, Name = "abc", Span = new TimeSpan(0, 4, 30, 10), Unsigned = 7 },
        new() { Id = 2, Name = "bcd", Span = new TimeSpan(1, 17, 5, 20), Unsigned = 4000000000 },
        new() { Id = 3, Name = "xyz", Span = new TimeSpan(2, 9, 59, 58), Unsigned = 0 },
    ];

    private static TestDatabase Seed(string methodName)
    {
        TestDatabase db = new(methodName);
        db.Table<BitComplementChainRow>().Schema.CreateTable();
        db.Table<BitComplementChainRow>().AddRange(Rows());
        return db;
    }

    [Fact]
    public void DoubleComplementOfIndexOfMatchesLinq()
    {
        using TestDatabase db = Seed(nameof(DoubleComplementOfIndexOfMatchesLinq));
        List<int> expected = Rows().Select(r => ~~r.Name.IndexOf("b")).ToList();
        List<int> actual = db.Table<BitComplementChainRow>().OrderBy(r => r.Id).Select(r => ~~r.Name.IndexOf("b")).ToList();
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ComplementOfTimeSpanHoursInOrderByMatchesLinq()
    {
        using TestDatabase db = Seed(nameof(ComplementOfTimeSpanHoursInOrderByMatchesLinq));
        List<int> expected = Rows().OrderBy(r => ~r.Span.Hours).Select(r => r.Id).ToList();
        List<int> actual = db.Table<BitComplementChainRow>().OrderBy(r => ~r.Span.Hours).Select(r => r.Id).ToList();
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ComplementOfIndexOfTimesTwoMatchesLinq()
    {
        using TestDatabase db = Seed(nameof(ComplementOfIndexOfTimesTwoMatchesLinq));
        List<int> expected = Rows().Select(r => ~r.Name.IndexOf("b") * 2).ToList();
        List<int> actual = db.Table<BitComplementChainRow>().OrderBy(r => r.Id).Select(r => ~r.Name.IndexOf("b") * 2).ToList();
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ComplementOfUnsignedColumnMatchesLinq()
    {
        using TestDatabase db = Seed(nameof(ComplementOfUnsignedColumnMatchesLinq));
        List<uint> expected = Rows().Select(r => ~r.Unsigned).ToList();
        List<uint> actual = db.Table<BitComplementChainRow>().OrderBy(r => r.Id).Select(r => ~r.Unsigned).ToList();
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ComplementOfTimeSpanMinutesMatchesLinq()
    {
        using TestDatabase db = Seed(nameof(ComplementOfTimeSpanMinutesMatchesLinq));
        List<int> expected = Rows().Select(r => ~r.Span.Minutes).ToList();
        List<int> actual = db.Table<BitComplementChainRow>().OrderBy(r => r.Id).Select(r => ~r.Span.Minutes).ToList();
        Assert.Equal(expected, actual);
    }
}
