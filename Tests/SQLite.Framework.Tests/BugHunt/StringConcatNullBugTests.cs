using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests.BugHunt;

public class StringConcatNullBugTests
{
    [Fact]
    public void ConcatWithConditionalNullBranch_MatchesLinqToObjects()
    {
        using TestDatabase db = new();
        db.Table<NullableStringEntity>().Schema.CreateTable();
        db.Table<NullableStringEntity>().Add(new NullableStringEntity { Id = 1, Name = "x" });

        string expected = "p" + (1 == 1 ? null : "z");
        string actual = db.Table<NullableStringEntity>()
            .Where(e => e.Id == 1)
            .Select(e => "p" + (e.Id == 1 ? null : "z"))
            .First();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ConcatWithCapturedNullVariable_MatchesLinqToObjects()
    {
        using TestDatabase db = new();
        db.Table<NullableStringEntity>().Schema.CreateTable();
        db.Table<NullableStringEntity>().Add(new NullableStringEntity { Id = 1, Name = "x" });

        string? captured = null;
        string expected = "x" + captured;
        string actual = db.Table<NullableStringEntity>()
            .Where(e => e.Id == 1)
            .Select(e => e.Name + captured)
            .First();

        Assert.Equal(expected, actual);
    }
}
