using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class StringCompareNullTests
{
    private static readonly (int Id, string? Name)[] Seed =
    [
        (1, null),
        (2, "z"),
        (3, "a"),
        (4, "m"),
    ];

    private static TestDatabase CreateDb((int Id, string? Name)[] seed)
    {
        TestDatabase db = new();
        db.Table<NullableStringEntity>().Schema.CreateTable();
        foreach ((int id, string? name) in seed)
        {
            db.Table<NullableStringEntity>().Add(new NullableStringEntity { Id = id, Name = name });
        }

        return db;
    }

    [Fact]
    public void Compare_AgainstNonNull_MatchesDotNet()
    {
        using TestDatabase db = CreateDb(Seed);

        List<int> expected = Seed.OrderBy(r => r.Id).Select(r => Math.Sign(string.Compare(r.Name, "m"))).ToList();
        List<int> actual = db.Table<NullableStringEntity>()
            .OrderBy(r => r.Id)
            .Select(r => string.Compare(r.Name, "m"))
            .ToList()
            .Select(Math.Sign)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Compare_AgainstNull_MatchesDotNet()
    {
        using TestDatabase db = CreateDb(Seed);

        string? comparand = null;
        List<int> expected = Seed.OrderBy(r => r.Id).Select(r => Math.Sign(string.Compare(r.Name, comparand))).ToList();
        List<int> actual = db.Table<NullableStringEntity>()
            .OrderBy(r => r.Id)
            .Select(r => string.Compare(r.Name, comparand))
            .ToList()
            .Select(Math.Sign)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void CompareTo_NonNullInstance_MatchesDotNet()
    {
        (int Id, string? Name)[] seed = [(2, "z"), (3, "a"), (4, "m")];
        using TestDatabase db = CreateDb(seed);

        List<int> expected = seed.OrderBy(r => r.Id).Select(r => Math.Sign(r.Name!.CompareTo("m"))).ToList();
        List<int> actual = db.Table<NullableStringEntity>()
            .OrderBy(r => r.Id)
            .Select(r => r.Name!.CompareTo("m"))
            .ToList()
            .Select(Math.Sign)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void CompareTo_NonNullInstance_NullArg_MatchesDotNet()
    {
        (int Id, string? Name)[] seed = [(2, "z"), (3, "a"), (4, "m")];
        using TestDatabase db = CreateDb(seed);

        string? arg = null;
        List<int> expected = seed.OrderBy(r => r.Id).Select(r => Math.Sign(r.Name!.CompareTo(arg))).ToList();
        List<int> actual = db.Table<NullableStringEntity>()
            .OrderBy(r => r.Id)
            .Select(r => r.Name!.CompareTo(arg))
            .ToList()
            .Select(Math.Sign)
            .ToList();

        Assert.Equal(expected, actual);
    }
}
