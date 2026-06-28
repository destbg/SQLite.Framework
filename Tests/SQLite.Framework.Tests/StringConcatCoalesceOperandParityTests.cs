using System.ComponentModel.DataAnnotations;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class StringConcatCoalesceOperandParityTests
{
    public class SccRow
    {
        [Key]
        public int Id { get; set; }
        public string? A { get; set; }
        public string? B { get; set; }
    }

    private static readonly SccRow[] Seed =
    [
        new() { Id = 1, A = null, B = null },
        new() { Id = 2, A = null, B = "bb" },
        new() { Id = 3, A = "aa", B = null },
    ];

    private static TestDatabase Create()
    {
        TestDatabase db = new();
        db.Table<SccRow>().Schema.CreateTable();
        foreach (SccRow r in Seed) db.Table<SccRow>().Add(r);
        return db;
    }

    [Fact]
    public void ConcatWithCoalesceOperand_MatchesLinqToObjects()
    {
        using TestDatabase db = Create();

        List<string> oracle = Seed.OrderBy(r => r.Id).Select(r => "x" + (r.A ?? r.B)).ToList();
        List<string> actual = db.Table<SccRow>().OrderBy(r => r.Id).Select(r => "x" + (r.A ?? r.B)).ToList();

        Assert.Equal(oracle, actual);
    }
}
