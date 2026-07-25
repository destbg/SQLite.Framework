using System.ComponentModel.DataAnnotations;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

file sealed class TwoNullableStringRow
{
    [Key]
    public int Id { get; set; }
    public string? Name1 { get; set; }
    public string? Name2 { get; set; }
}

public class StaticStringEqualsNullColumnParityTests
{
    private static readonly (int Id, string? Name1, string? Name2)[] Seed =
    [
        (1, null, null),
        (2, "a", "a"),
        (3, "a", "b"),
        (4, null, "a"),
    ];

    private static TestDatabase CreateDb()
    {
        TestDatabase db = new();
        db.Table<TwoNullableStringRow>().Schema.CreateTable();
        foreach ((int id, string? n1, string? n2) in Seed)
        {
            db.Table<TwoNullableStringRow>().Add(new TwoNullableStringRow { Id = id, Name1 = n1, Name2 = n2 });
        }
        return db;
    }

    [Fact]
    public void StaticEquals_BothNullColumns_MatchesDotNet()
    {
        using TestDatabase db = CreateDb();

        List<int> expected = Seed.Where(r => string.Equals(r.Name1, r.Name2)).Select(r => r.Id).OrderBy(i => i).ToList();
        List<int> actual = db.Table<TwoNullableStringRow>().Where(x => string.Equals(x.Name1, x.Name2)).Select(x => x.Id).OrderBy(i => i).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void StaticEquals_WithOrdinalComparison_BothNullColumns_MatchesDotNet()
    {
        using TestDatabase db = CreateDb();

        List<int> expected = Seed.Where(r => string.Equals(r.Name1, r.Name2, StringComparison.Ordinal)).Select(r => r.Id).OrderBy(i => i).ToList();
        List<int> actual = db.Table<TwoNullableStringRow>().Where(x => string.Equals(x.Name1, x.Name2, StringComparison.Ordinal)).Select(x => x.Id).OrderBy(i => i).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void StaticEquals_WithOrdinalIgnoreCaseComparison_BothNullColumns_MatchesDotNet()
    {
        using TestDatabase db = CreateDb();

        List<int> expected = Seed.Where(r => string.Equals(r.Name1, r.Name2, StringComparison.OrdinalIgnoreCase)).Select(r => r.Id).OrderBy(i => i).ToList();
        List<int> actual = db.Table<TwoNullableStringRow>().Where(x => string.Equals(x.Name1, x.Name2, StringComparison.OrdinalIgnoreCase)).Select(x => x.Id).OrderBy(i => i).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void StaticEquals_NullColumnAgainstNullLiteral_MatchesDotNet()
    {
        using TestDatabase db = CreateDb();

        string? nullValue = null;
        List<int> expected = Seed.Where(r => string.Equals(r.Name1, nullValue)).Select(r => r.Id).OrderBy(i => i).ToList();
        List<int> actual = db.Table<TwoNullableStringRow>().Where(x => string.Equals(x.Name1, nullValue)).Select(x => x.Id).OrderBy(i => i).ToList();

        Assert.Equal(expected, actual);
    }
}
