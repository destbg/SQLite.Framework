using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class TerminalContainsNullProbeTests
{
    private static TestDatabase SeedInts(params (int id, int? value)[] rows)
    {
        TestDatabase db = new();
        db.Table<NullableEntity>().Schema.CreateTable();
        foreach ((int id, int? value) in rows)
        {
            db.Table<NullableEntity>().Add(new NullableEntity { Id = id, Value = value });
        }

        return db;
    }

    [Fact]
    public void ContainsNullProbe_WithNullRow_MatchesLinqToObjects()
    {
        (int id, int? value)[] rows = [(1, null), (2, 5)];
        using TestDatabase db = SeedInts(rows);

        int? probe = null;
        bool expected = rows.Select(r => r.value).Contains(probe);
        bool actual = db.Table<NullableEntity>().Select(x => x.Value).Contains(probe);

        Assert.True(expected);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ContainsNullProbe_WithoutNullRow_MatchesLinqToObjects()
    {
        (int id, int? value)[] rows = [(1, 5), (2, 7)];
        using TestDatabase db = SeedInts(rows);

        int? probe = null;
        bool expected = rows.Select(r => r.value).Contains(probe);
        bool actual = db.Table<NullableEntity>().Select(x => x.Value).Contains(probe);

        Assert.False(expected);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ContainsNullProbe_EmptyTable_MatchesLinqToObjects()
    {
        using TestDatabase db = SeedInts();

        int? probe = null;
        bool expected = Array.Empty<int?>().Contains(probe);
        bool actual = db.Table<NullableEntity>().Select(x => x.Value).Contains(probe);

        Assert.False(expected);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ContainsNonNullProbe_WithNullRow_StillMatches()
    {
        (int id, int? value)[] rows = [(1, null), (2, 5)];
        using TestDatabase db = SeedInts(rows);

        bool foundExisting = db.Table<NullableEntity>().Select(x => x.Value).Contains(5);
        bool missing = db.Table<NullableEntity>().Select(x => x.Value).Contains(9);

        Assert.True(foundExisting);
        Assert.False(missing);
    }

    [Fact]
    public async Task ContainsAsyncNullProbe_WithNullRow_MatchesLinqToObjects()
    {
        (int id, int? value)[] rows = [(1, null), (2, 5)];
        using TestDatabase db = SeedInts(rows);

        int? probe = null;
        bool expected = rows.Select(r => r.value).Contains(probe);
        bool actual = await db.Table<NullableEntity>().Select(x => x.Value).ContainsAsync(probe);

        Assert.True(expected);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public async Task ContainsAsyncNullProbe_WithoutNullRow_MatchesLinqToObjects()
    {
        (int id, int? value)[] rows = [(1, 5), (2, 7)];
        using TestDatabase db = SeedInts(rows);

        int? probe = null;
        bool expected = rows.Select(r => r.value).Contains(probe);
        bool actual = await db.Table<NullableEntity>().Select(x => x.Value).ContainsAsync(probe);

        Assert.False(expected);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ContainsNullStringProbe_WithNullRow_MatchesLinqToObjects()
    {
        using TestDatabase db = new();
        db.Table<NullableStringEntity>().Schema.CreateTable();
        db.Table<NullableStringEntity>().Add(new NullableStringEntity { Id = 1, Name = null });
        db.Table<NullableStringEntity>().Add(new NullableStringEntity { Id = 2, Name = "a" });

        NullableStringEntity[] seed =
        [
            new NullableStringEntity { Id = 1, Name = null },
            new NullableStringEntity { Id = 2, Name = "a" },
        ];

        string? probe = null;
        bool expected = seed.Select(x => x.Name).Contains(probe);
        bool actual = db.Table<NullableStringEntity>().Select(x => x.Name).Contains(probe);

        Assert.True(expected);
        Assert.Equal(expected, actual);
    }
}
