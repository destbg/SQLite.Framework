using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class StringInlineArrayBoundsClientEvalParityTests
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
    public void ConcatSizedIntArrayMatchesDotNet()
    {
        using TestDatabase db = Setup();
        List<string> expected = Rows().OrderBy(r => r.Id).Select(r => string.Concat(new int[3])).ToList();
        List<string> actual = db.Table<H20StrRow>().OrderBy(r => r.Id).Select(r => string.Concat(new int[3])).ToList();
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ConcatColumnSizedIntArrayMatchesDotNet()
    {
        using TestDatabase db = Setup();
        List<string> expected = Rows().OrderBy(r => r.Id).Select(r => string.Concat(new int[r.Num / 10])).ToList();
        List<string> actual = db.Table<H20StrRow>().OrderBy(r => r.Id).Select(r => string.Concat(new int[r.Num / 10])).ToList();
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void JoinColumnSizedStringArrayMatchesDotNet()
    {
        using TestDatabase db = Setup();
        List<string> expected = Rows().OrderBy(r => r.Id).Select(r => string.Join("-", new string[r.Num / 10])).ToList();
        List<string> actual = db.Table<H20StrRow>().OrderBy(r => r.Id).Select(r => string.Join("-", new string[r.Num / 10])).ToList();
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void JoinColumnSeparatorSizedStringArrayMatchesDotNet()
    {
        using TestDatabase db = Setup();
        List<string> expected = Rows().OrderBy(r => r.Id).Select(r => string.Join(r.Name, new string[2])).ToList();
        List<string> actual = db.Table<H20StrRow>().OrderBy(r => r.Id).Select(r => string.Join(r.Name, new string[2])).ToList();
        Assert.Equal(expected, actual);
    }
}
