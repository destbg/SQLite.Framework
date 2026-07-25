using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class EmbeddedNulStringFunctionTests
{
    [Fact]
    public void RoundTripAndEqualityKeepWholeString()
    {
        using TestDatabase db = new();
        db.Table<TwoStringEntity>().Schema.CreateTable();
        db.Table<TwoStringEntity>().AddRange(
        [
            new TwoStringEntity { Id = 1, A = "a\0b", B = "" },
            new TwoStringEntity { Id = 2, A = "a", B = "" },
        ]);

        TwoStringEntity row = db.Table<TwoStringEntity>().First(x => x.A == "a\0b");
        Assert.Equal(1, row.Id);
        Assert.Equal("a\0b", row.A);
    }

    [Fact]
    public void LengthCountsCharactersBeforeEmbeddedNul()
    {
        using TestDatabase db = new();
        db.Table<TwoStringEntity>().Schema.CreateTable();
        db.Table<TwoStringEntity>().AddRange(
        [
            new TwoStringEntity { Id = 1, A = "a\0b", B = "" },
            new TwoStringEntity { Id = 2, A = "a", B = "" },
        ]);

        List<int> inMemory = new[] { "a\0b", "a" }.Select(s => s.Length).ToList();
        Assert.Equal([3, 1], inMemory);

        List<int> actual = db.Table<TwoStringEntity>().OrderBy(x => x.Id).Select(x => x.A.Length).ToList();
        Assert.Equal([1, 1], actual);
    }

    [Fact]
    public void ContainsSeesOnlyTextBeforeEmbeddedNul()
    {
        using TestDatabase db = new();
        db.Table<TwoStringEntity>().Schema.CreateTable();
        db.Table<TwoStringEntity>().AddRange(
        [
            new TwoStringEntity { Id = 1, A = "a\0bc", B = "" },
            new TwoStringEntity { Id = 2, A = "abc", B = "" },
        ]);

        List<int> inMemory = new[]
            {
                new TwoStringEntity { Id = 1, A = "a\0bc", B = "" },
                new TwoStringEntity { Id = 2, A = "abc", B = "" },
            }
            .Where(x => x.A.Contains("bc")).Select(x => x.Id).OrderBy(i => i).ToList();
        Assert.Equal([1, 2], inMemory);

        List<int> actual = db.Table<TwoStringEntity>()
            .Where(x => x.A.Contains("bc")).Select(x => x.Id).OrderBy(i => i).ToList();
        Assert.Equal([2], actual);
    }

    [Fact]
    public void SubstringSeesOnlyTextBeforeEmbeddedNul()
    {
        using TestDatabase db = new();
        db.Table<TwoStringEntity>().Schema.CreateTable();
        db.Table<TwoStringEntity>().Add(new TwoStringEntity { Id = 1, A = "a\0bcd", B = "" });

        string inMemory = "a\0bcd".Substring(2, 2);
        Assert.Equal("bc", inMemory);

        List<string> actual = db.Table<TwoStringEntity>().Select(x => x.A.Substring(2, 2)).ToList();
        Assert.Equal([""], actual);
    }
}
