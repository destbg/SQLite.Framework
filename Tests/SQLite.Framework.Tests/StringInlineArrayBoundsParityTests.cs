using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class StringInlineArrayBoundsParityTests
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
    public void ConcatSizedStringArrayMatchesDotNet()
    {
        using TestDatabase db = Setup();
        List<string> expected = Rows().OrderBy(r => r.Id).Select(r => string.Concat(new string[2])).ToList();
        List<string> actual = db.Table<H20StrRow>().OrderBy(r => r.Id).Select(r => string.Concat(new string[2])).ToList();
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void JoinSizedStringArrayMatchesDotNet()
    {
        using TestDatabase db = Setup();
        List<string> expected = Rows().OrderBy(r => r.Id).Select(r => string.Join("-", new string[2])).ToList();
        List<string> actual = db.Table<H20StrRow>().OrderBy(r => r.Id).Select(r => string.Join("-", new string[2])).ToList();
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ConcatZeroSizedStringArrayMatchesDotNet()
    {
        using TestDatabase db = Setup();
        List<string> expected = Rows().OrderBy(r => r.Id).Select(r => string.Concat(new string[0])).ToList();
        List<string> actual = db.Table<H20StrRow>().OrderBy(r => r.Id).Select(r => string.Concat(new string[0])).ToList();
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ConcatZeroSizedObjectArrayMatchesDotNet()
    {
        using TestDatabase db = Setup();
        List<string> expected = Rows().OrderBy(r => r.Id).Select(r => string.Concat(new object?[0])).ToList();
        List<string> actual = db.Table<H20StrRow>().OrderBy(r => r.Id).Select(r => string.Concat(new object?[0])).ToList();
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void JoinZeroSizedStringArrayMatchesDotNet()
    {
        using TestDatabase db = Setup();
        List<string> expected = Rows().OrderBy(r => r.Id).Select(r => string.Join("-", new string[0])).ToList();
        List<string> actual = db.Table<H20StrRow>().OrderBy(r => r.Id).Select(r => string.Join("-", new string[0])).ToList();
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ConcatColumnSizedStringArrayMatchesDotNet()
    {
        using TestDatabase db = Setup();
        List<string> expected = Rows().OrderBy(r => r.Id).Select(r => string.Concat(new string[r.Num / 10])).ToList();
        List<string> actual = db.Table<H20StrRow>().OrderBy(r => r.Id).Select(r => string.Concat(new string[r.Num / 10])).ToList();
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ConcatEmptyInitStringArrayMatchesDotNet()
    {
        using TestDatabase db = Setup();
        List<string> expected = Rows().OrderBy(r => r.Id).Select(r => string.Concat(new string[] { })).ToList();
        List<string> actual = db.Table<H20StrRow>().OrderBy(r => r.Id).Select(r => string.Concat(new string[] { })).ToList();
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ConcatEmptyInitObjectArrayMatchesDotNet()
    {
        using TestDatabase db = Setup();
        List<string> expected = Rows().OrderBy(r => r.Id).Select(r => string.Concat(new object?[] { })).ToList();
        List<string> actual = db.Table<H20StrRow>().OrderBy(r => r.Id).Select(r => string.Concat(new object?[] { })).ToList();
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void JoinEmptyInitStringArrayMatchesDotNet()
    {
        using TestDatabase db = Setup();
        List<string> expected = Rows().OrderBy(r => r.Id).Select(r => string.Join("-", new string[] { })).ToList();
        List<string> actual = db.Table<H20StrRow>().OrderBy(r => r.Id).Select(r => string.Join("-", new string[] { })).ToList();
        Assert.Equal(expected, actual);
    }
}
