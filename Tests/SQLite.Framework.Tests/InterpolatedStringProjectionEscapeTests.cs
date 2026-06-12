using System.ComponentModel.DataAnnotations;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

internal sealed class InterpolatedEscapeRow
{
    [Key]
    public int Id { get; set; }

    public string Name { get; set; } = "";

    public int Value { get; set; }
}

public class InterpolatedStringProjectionEscapeTests
{
    [Fact]
    public void NewlineAndFormatClauseAreKept()
    {
        using TestDatabase db = SetupDatabase();

        List<string> expected = db.Table<InterpolatedEscapeRow>().AsEnumerable()
            .OrderBy(r => r.Id)
            .Select(r => $"a\nb{r.Value:D5}")
            .ToList();

        Assert.Equal(["a\nb00010", "a\nb00025"], expected);

        List<string> actual = db.Table<InterpolatedEscapeRow>()
            .OrderBy(r => r.Id)
            .Select(r => $"a\nb{r.Value:D5}")
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void LiteralBracesAreKept()
    {
        using TestDatabase db = SetupDatabase();

        List<string> expected = db.Table<InterpolatedEscapeRow>().AsEnumerable()
            .OrderBy(r => r.Id)
            .Select(r => $"{{x}}{r.Value}")
            .ToList();

        Assert.Equal(["{x}10", "{x}25"], expected);

        List<string> actual = db.Table<InterpolatedEscapeRow>()
            .OrderBy(r => r.Id)
            .Select(r => $"{{x}}{r.Value}")
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void FourHoleInterpolationFormatsAllHoles()
    {
        using TestDatabase db = SetupDatabase();

        List<string> expected = db.Table<InterpolatedEscapeRow>().AsEnumerable()
            .OrderBy(r => r.Id)
            .Select(r => $"{r.Id:D2}|{r.Name}|{r.Value:D3}|{r.Id}")
            .ToList();

        Assert.Equal(["01|a|010|1", "02|b|025|2"], expected);

        List<string> actual = db.Table<InterpolatedEscapeRow>()
            .OrderBy(r => r.Id)
            .Select(r => $"{r.Id:D2}|{r.Name}|{r.Value:D3}|{r.Id}")
            .ToList();

        Assert.Equal(expected, actual);
    }

    private static TestDatabase SetupDatabase()
    {
        TestDatabase db = new();
        db.Table<InterpolatedEscapeRow>().Schema.CreateTable();
        db.Table<InterpolatedEscapeRow>().Add(new InterpolatedEscapeRow { Id = 1, Name = "a", Value = 10 });
        db.Table<InterpolatedEscapeRow>().Add(new InterpolatedEscapeRow { Id = 2, Name = "b", Value = 25 });
        return db;
    }
}
