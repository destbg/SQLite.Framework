using System.ComponentModel.DataAnnotations;
using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class ExecuteUpdateDeletePrecedenceTests
{
    internal sealed class EuRow
    {
        [Key]
        public int Id { get; set; }
        public int N { get; set; }
        public required string Str { get; set; }
        public bool Flag { get; set; }
    }

    private static List<EuRow> Data() =>
    [
        new() { Id = 1, N = 0, Str = "abc", Flag = true },
        new() { Id = 2, N = 0, Str = "xyz", Flag = false },
        new() { Id = 3, N = 0, Str = "abz", Flag = false },
    ];

    private static TestDatabase Seed()
    {
        TestDatabase db = new();
        db.Table<EuRow>().Schema.CreateTable();
        db.Table<EuRow>().AddRange(Data());
        return db;
    }

    [Fact]
    public void SetToIndexOfArithmeticKeepsPrecedence()
    {
        using TestDatabase db = Seed();
        List<EuRow> oracle = Data();
        foreach (EuRow r in oracle) r.N = r.Str.IndexOf('b') * 2;
        List<int> expected = oracle.OrderBy(x => x.Id).Select(x => x.N).ToList();
        Assert.Equal([2, -2, 2], expected);

        db.Table<EuRow>().ExecuteUpdate(s => s.Set(x => x.N, x => x.Str.IndexOf('b') * 2));

        List<int> actual = db.Table<EuRow>().OrderBy(x => x.Id).Select(x => x.N).ToList();
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void DeleteWithBoolEqualsPredicateKeepsPrecedence()
    {
        using TestDatabase db = Seed();
        List<int> expected = Data().Where(x => !(x.Flag == x.Str.StartsWith("a"))).Select(x => x.Id).OrderBy(i => i).ToList();

        db.Table<EuRow>().Where(x => x.Flag == x.Str.StartsWith("a")).ExecuteDelete();

        List<int> actual = db.Table<EuRow>().Select(x => x.Id).OrderBy(i => i).ToList();
        Assert.Equal(expected, actual);
    }
}
