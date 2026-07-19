using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H20StrRows")]
public class H20StrRow
{
    [Key]
    public int Id { get; set; }

    public string Name { get; set; } = "";

    public string? Note { get; set; }

    public int Num { get; set; }

    public bool Flag { get; set; }

    public DateTime When { get; set; }

    public double Ratio { get; set; }

    public decimal Price { get; set; }

    public H20StrMood Mood { get; set; }

    public int? MaybeNum { get; set; }
}

public enum H20StrMood
{
    Calm,
    Happy
}

public class StringInlineArrayConstantElementParityTests
{
    private static List<H20StrRow> Rows() =>
    [
        new H20StrRow { Id = 1, Name = "a", Num = 10, Flag = true, When = new DateTime(2020, 1, 2), Ratio = 1.5, Price = 1.50m, Mood = H20StrMood.Happy },
        new H20StrRow { Id = 2, Name = "b", Num = 20, Flag = false, When = new DateTime(2021, 3, 4), Ratio = 2.5, Price = 2.00m, Mood = H20StrMood.Calm, MaybeNum = 5 },
    ];

    private static TestDatabase Setup()
    {
        TestDatabase db = new();
        db.Table<H20StrRow>().Schema.CreateTable();
        db.Table<H20StrRow>().AddRange(Rows());
        return db;
    }

    [Fact]
    public void ConcatConstantBoolMatchesDotNet()
    {
        using TestDatabase db = Setup();
        List<string> expected = Rows().OrderBy(r => r.Id).Select(r => string.Concat(new object?[] { "v=", true })).ToList();
        List<string> actual = db.Table<H20StrRow>().OrderBy(r => r.Id).Select(r => string.Concat(new object?[] { "v=", true })).ToList();
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ConcatColumnAndConstantBoolMatchesDotNet()
    {
        using TestDatabase db = Setup();
        List<string> expected = Rows().OrderBy(r => r.Id).Select(r => string.Concat(new object?[] { r.Name, true })).ToList();
        List<string> actual = db.Table<H20StrRow>().OrderBy(r => r.Id).Select(r => string.Concat(new object?[] { r.Name, true })).ToList();
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ConcatConstantDateTimeMatchesDotNet()
    {
        using TestDatabase db = Setup();
        List<string> expected = Rows().OrderBy(r => r.Id).Select(r => string.Concat(new object?[] { "d=", new DateTime(2020, 1, 2, 3, 4, 5) })).ToList();
        List<string> actual = db.Table<H20StrRow>().OrderBy(r => r.Id).Select(r => string.Concat(new object?[] { "d=", new DateTime(2020, 1, 2, 3, 4, 5) })).ToList();
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ConcatConstantNaNMatchesDotNet()
    {
        using TestDatabase db = Setup();
        List<string> expected = Rows().OrderBy(r => r.Id).Select(r => string.Concat(new object?[] { "n=", double.NaN })).ToList();
        List<string> actual = db.Table<H20StrRow>().OrderBy(r => r.Id).Select(r => string.Concat(new object?[] { "n=", double.NaN })).ToList();
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ConcatConstantDecimalMatchesDotNet()
    {
        using TestDatabase db = Setup();
        List<string> expected = Rows().OrderBy(r => r.Id).Select(r => string.Concat(new object?[] { 1.50m })).ToList();
        List<string> actual = db.Table<H20StrRow>().OrderBy(r => r.Id).Select(r => string.Concat(new object?[] { 1.50m })).ToList();
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ConcatConstantEnumMatchesDotNet()
    {
        using TestDatabase db = Setup();
        List<string> expected = Rows().OrderBy(r => r.Id).Select(r => string.Concat(new object?[] { H20StrMood.Happy })).ToList();
        List<string> actual = db.Table<H20StrRow>().OrderBy(r => r.Id).Select(r => string.Concat(new object?[] { H20StrMood.Happy })).ToList();
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ConcatTimeSpanElementMatchesDotNet()
    {
        using TestDatabase db = Setup();
        List<string> expected = Rows().OrderBy(r => r.Id).Select(r => string.Concat(new object?[] { TimeSpan.FromMinutes(5) })).ToList();
        List<string> actual = db.Table<H20StrRow>().OrderBy(r => r.Id).Select(r => string.Concat(new object?[] { TimeSpan.FromMinutes(5) })).ToList();
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void JoinConstantBoolsMatchesDotNet()
    {
        using TestDatabase db = Setup();
        List<string> expected = Rows().OrderBy(r => r.Id).Select(r => string.Join("-", new object?[] { true, false })).ToList();
        List<string> actual = db.Table<H20StrRow>().OrderBy(r => r.Id).Select(r => string.Join("-", new object?[] { true, false })).ToList();
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void WhereConcatConstantBoolMatchesDotNet()
    {
        using TestDatabase db = Setup();
        List<int> expected = Rows().Where(r => string.Concat(new object?[] { r.Name, true }) == "aTrue").Select(r => r.Id).ToList();
        List<int> actual = db.Table<H20StrRow>().Where(r => string.Concat(new object?[] { r.Name, true }) == "aTrue").Select(r => r.Id).ToList();
        Assert.Equal(expected, actual);
    }
}
