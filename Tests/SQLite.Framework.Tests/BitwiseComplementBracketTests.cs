using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("BitComplementRow")]
public class BitComplementRow
{
    [Key]
    public int Id { get; set; }

    public string Name { get; set; } = "";

    public TimeSpan Span { get; set; }

    public DateTime When { get; set; }
}

public class BitwiseComplementBracketTests
{
    private static List<BitComplementRow> Rows() =>
    [
        new() { Id = 1, Name = "abc", Span = new TimeSpan(1, 23, 45, 30), When = new DateTime(2024, 1, 1, 8, 30, 15, 999) },
        new() { Id = 2, Name = "bcd", Span = new TimeSpan(0, 5, 10, 20), When = new DateTime(2024, 6, 2, 12, 0, 0, 5) },
        new() { Id = 3, Name = "xyz", Span = new TimeSpan(2, 23, 59, 58), When = new DateTime(2023, 3, 3, 3, 3, 3, 999) },
    ];

    private static TestDatabase Seed(string methodName)
    {
        TestDatabase db = new(methodName);
        db.Table<BitComplementRow>().Schema.CreateTable();
        db.Table<BitComplementRow>().AddRange(Rows());
        return db;
    }

    [Fact]
    public void ComplementOfIndexOfMatchesLinq()
    {
        using TestDatabase db = Seed(nameof(ComplementOfIndexOfMatchesLinq));
        List<int> expected = Rows().Select(r => ~r.Name.IndexOf("b")).ToList();
        List<int> actual = db.Table<BitComplementRow>().OrderBy(r => r.Id).Select(r => ~r.Name.IndexOf("b")).ToList();
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ComplementOfTimeSpanHoursMatchesLinq()
    {
        using TestDatabase db = Seed(nameof(ComplementOfTimeSpanHoursMatchesLinq));
        List<int> expected = Rows().Select(r => ~r.Span.Hours).ToList();
        List<int> actual = db.Table<BitComplementRow>().OrderBy(r => r.Id).Select(r => ~r.Span.Hours).ToList();
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ComplementOfDateTimeMillisecondMatchesLinq()
    {
        using TestDatabase db = Seed(nameof(ComplementOfDateTimeMillisecondMatchesLinq));
        List<int> expected = Rows().Select(r => ~r.When.Millisecond).ToList();
        List<int> actual = db.Table<BitComplementRow>().OrderBy(r => r.Id).Select(r => ~r.When.Millisecond).ToList();
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ComplementOfIndexOfInFilterMatchesLinq()
    {
        using TestDatabase db = Seed(nameof(ComplementOfIndexOfInFilterMatchesLinq));
        List<int> expected = Rows().Where(r => ~r.Name.IndexOf("b") == -2).Select(r => r.Id).ToList();
        List<int> actual = db.Table<BitComplementRow>().Where(r => ~r.Name.IndexOf("b") == -2).OrderBy(r => r.Id).Select(r => r.Id).ToList();
        Assert.Equal(expected, actual);
    }
}
