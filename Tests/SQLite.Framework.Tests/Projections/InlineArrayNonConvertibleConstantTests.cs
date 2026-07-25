using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class InlineArrayNonConvertibleConstantTests
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
    public void ConcatCapturedGuidMatchesDotNet()
    {
        using TestDatabase db = Setup();
        Guid g = new("11111111-2222-3333-4444-555555555555");
        List<string> expected = Rows().OrderBy(r => r.Id).Select(r => string.Concat(new object?[] { "g=", g })).ToList();
        List<string> actual = db.Table<H20StrRow>().OrderBy(r => r.Id).Select(r => string.Concat(new object?[] { "g=", g })).ToList();
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ConcatCapturedTimeSpanMatchesDotNet()
    {
        using TestDatabase db = Setup();
        TimeSpan span = TimeSpan.FromMinutes(5);
        List<string> expected = Rows().OrderBy(r => r.Id).Select(r => string.Concat(new object?[] { "t=", span })).ToList();
        List<string> actual = db.Table<H20StrRow>().OrderBy(r => r.Id).Select(r => string.Concat(new object?[] { "t=", span })).ToList();
        Assert.Equal(expected, actual);
    }
}
