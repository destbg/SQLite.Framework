using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class StringConcatNullOperandTests
{
    private static (TestDatabase db, NullableStringEntity[] seed) Seed(params (int id, string? name)[] rows)
    {
        TestDatabase db = new();
        db.Table<NullableStringEntity>().Schema.CreateTable();
        foreach ((int id, string? name) in rows)
        {
            db.Table<NullableStringEntity>().Add(new NullableStringEntity { Id = id, Name = name });
        }

        NullableStringEntity[] seed = rows.Select(r => new NullableStringEntity { Id = r.id, Name = r.name }).ToArray();
        return (db, seed);
    }

    [Fact]
    public void ConcatRightConditionalNullBranch_MatchesLinqToObjects()
    {
        (TestDatabase db, NullableStringEntity[] seed) = Seed((1, "x"));
        using TestDatabase _ = db;

        string expected = seed.Where(e => e.Id == 1).Select(e => e.Name + (e.Id == 1 ? null : "z")).First();
        string actual = db.Table<NullableStringEntity>().Where(e => e.Id == 1).Select(e => e.Name + (e.Id == 1 ? null : "z")).First();

        Assert.Equal("x", expected);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ConcatLeftConditionalNullBranch_MatchesLinqToObjects()
    {
        (TestDatabase db, NullableStringEntity[] seed) = Seed((1, "x"));
        using TestDatabase _ = db;

        string expected = seed.Where(e => e.Id == 1).Select(e => (e.Id == 1 ? null : "z") + e.Name).First();
        string actual = db.Table<NullableStringEntity>().Where(e => e.Id == 1).Select(e => (e.Id == 1 ? null : "z") + e.Name).First();

        Assert.Equal("x", expected);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ConcatCapturedNullVariable_MatchesLinqToObjects()
    {
        (TestDatabase db, NullableStringEntity[] seed) = Seed((1, "x"));
        using TestDatabase _ = db;

        string? captured = null;
        string expected = seed.Where(e => e.Id == 1).Select(e => e.Name + captured).First();
        string actual = db.Table<NullableStringEntity>().Where(e => e.Id == 1).Select(e => e.Name + captured).First();

        Assert.Equal("x", expected);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ConcatBothConditionalNullBranches_MatchesLinqToObjects()
    {
        (TestDatabase db, NullableStringEntity[] seed) = Seed((1, "x"));
        using TestDatabase _ = db;

        string expected = seed.Where(e => e.Id == 1).Select(e => (e.Id == 1 ? null : "a") + (e.Id == 1 ? null : "b")).First();
        string actual = db.Table<NullableStringEntity>().Where(e => e.Id == 1).Select(e => (e.Id == 1 ? null : "a") + (e.Id == 1 ? null : "b")).First();

        Assert.Equal("", expected);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ConcatNullableColumnNullRow_StillCoalesces()
    {
        (TestDatabase db, NullableStringEntity[] seed) = Seed((1, null), (2, "y"));
        using TestDatabase _ = db;

        List<string> expected = seed.OrderBy(e => e.Id).Select(e => e.Name + "!").ToList();
        List<string> actual = db.Table<NullableStringEntity>().OrderBy(e => e.Id).Select(e => e.Name + "!").ToList();

        Assert.Equal(["!", "y!"], expected);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ConcatNonNullLiteral_MatchesLinqToObjects()
    {
        (TestDatabase db, NullableStringEntity[] seed) = Seed((1, "x"));
        using TestDatabase _ = db;

        string expected = seed.Where(e => e.Id == 1).Select(e => e.Name + ".txt").First();
        string actual = db.Table<NullableStringEntity>().Where(e => e.Id == 1).Select(e => e.Name + ".txt").First();

        Assert.Equal("x.txt", expected);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ConcatWithSubstringOperand_MatchesLinqToObjects()
    {
        (TestDatabase db, NullableStringEntity[] seed) = Seed((1, "abc"));
        using TestDatabase _ = db;

        string expected = seed.Where(e => e.Id == 1).Select(e => e.Name!.Substring(0, 1) + e.Name).First();
        string actual = db.Table<NullableStringEntity>().Where(e => e.Id == 1).Select(e => e.Name!.Substring(0, 1) + e.Name).First();

        Assert.Equal("aabc", expected);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void StringConcatMethod_WithCapturedNull_MatchesLinqToObjects()
    {
        (TestDatabase db, NullableStringEntity[] seed) = Seed((1, "x"));
        using TestDatabase _ = db;

        string? captured = null;
        string expected = seed.Where(e => e.Id == 1).Select(e => string.Concat(e.Name, captured, "!")).First();
        string actual = db.Table<NullableStringEntity>().Where(e => e.Id == 1).Select(e => string.Concat(e.Name, captured, "!")).First();

        Assert.Equal("x!", expected);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void StringJoinMethod_WithCapturedNullElement_MatchesLinqToObjects()
    {
        (TestDatabase db, NullableStringEntity[] seed) = Seed((1, "x"));
        using TestDatabase _ = db;

        string? captured = null;
        string expected = seed.Where(e => e.Id == 1).Select(e => string.Join("-", new[] { e.Name, captured, "z" })).First();
        string actual = db.Table<NullableStringEntity>().Where(e => e.Id == 1).Select(e => string.Join("-", new[] { e.Name, captured, "z" })).First();

        Assert.Equal("x--z", expected);
        Assert.Equal(expected, actual);
    }
}
