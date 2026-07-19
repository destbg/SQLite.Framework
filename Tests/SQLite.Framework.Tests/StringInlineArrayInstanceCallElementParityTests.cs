using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class StringInlineArrayInstanceCallElementParityTests
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
    public void ConcatConstantReceiverCallElementMatchesDotNet()
    {
        using TestDatabase db = Setup();
        List<string> expected = Rows().OrderBy(r => r.Id).Select(r => string.Concat(new object?[] { "s=", "abc".Substring(1) })).ToList();
        List<string> actual = db.Table<H20StrRow>().OrderBy(r => r.Id).Select(r => string.Concat(new object?[] { "s=", "abc".Substring(1) })).ToList();
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ConcatColumnArgReceiverCallElementMatchesDotNet()
    {
        using TestDatabase db = Setup();
        List<string> expected = Rows().OrderBy(r => r.Id).Select(r => string.Concat(new object?[] { "s=", "abcdef".Substring(r.Num / 10) })).ToList();
        List<string> actual = db.Table<H20StrRow>().OrderBy(r => r.Id).Select(r => string.Concat(new object?[] { "s=", "abcdef".Substring(r.Num / 10) })).ToList();
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ConcatColumnReceiverCallElementMatchesDotNet()
    {
        using TestDatabase db = Setup();
        List<string> expected = Rows().OrderBy(r => r.Id).Select(r => string.Concat(new object?[] { "u=", r.Name.ToUpper() })).ToList();
        List<string> actual = db.Table<H20StrRow>().OrderBy(r => r.Id).Select(r => string.Concat(new object?[] { "u=", r.Name.ToUpper() })).ToList();
        Assert.Equal(expected, actual);
    }
}
