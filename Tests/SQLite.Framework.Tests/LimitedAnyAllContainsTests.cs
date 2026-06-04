using System.ComponentModel.DataAnnotations;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

internal sealed class LacRow
{
    [Key]
    public int Id { get; set; }
    public int Value { get; set; }
}

public class LimitedAnyAllContainsTests
{
    private static readonly LacRow[] Seed =
    [
        new LacRow { Id = 1, Value = 1 },
        new LacRow { Id = 2, Value = 2 },
        new LacRow { Id = 3, Value = 3 },
        new LacRow { Id = 4, Value = 5 },
    ];

    private static TestDatabase CreateDb()
    {
        TestDatabase db = new();
        db.Table<LacRow>().Schema.CreateTable();
        db.Table<LacRow>().AddRange(Seed);
        return db;
    }

    private static void AssertSame(Func<IQueryable<LacRow>, bool> framework, Func<IEnumerable<LacRow>, bool> oracle, bool expected)
    {
        using TestDatabase db = CreateDb();
        bool mem = oracle(Seed);
        bool sql = framework(db.Table<LacRow>());
        Assert.Equal(expected, mem);
        Assert.Equal(mem, sql);
    }

    [Fact]
    public void AnyPredicate_AfterTake_OutsideWindow_IsFalse()
        => AssertSame(q => q.OrderBy(x => x.Value).Take(2).Any(x => x.Value == 5),
                      s => s.OrderBy(x => x.Value).Take(2).Any(x => x.Value == 5), false);

    [Fact]
    public void AnyPredicate_AfterTake_InsideWindow_IsTrue()
        => AssertSame(q => q.OrderBy(x => x.Value).Take(2).Any(x => x.Value == 2),
                      s => s.OrderBy(x => x.Value).Take(2).Any(x => x.Value == 2), true);

    [Fact]
    public void AnyPredicate_AfterTakeCoveringMatch_IsTrue()
        => AssertSame(q => q.OrderBy(x => x.Value).Take(4).Any(x => x.Value == 5),
                      s => s.OrderBy(x => x.Value).Take(4).Any(x => x.Value == 5), true);

    [Fact]
    public void AnyPredicate_AfterSkip_InsideTail_IsTrue()
        => AssertSame(q => q.OrderBy(x => x.Value).Skip(3).Any(x => x.Value == 5),
                      s => s.OrderBy(x => x.Value).Skip(3).Any(x => x.Value == 5), true);

    [Fact]
    public void AnyPredicate_AfterSkip_OutsideTail_IsFalse()
        => AssertSame(q => q.OrderBy(x => x.Value).Skip(3).Any(x => x.Value == 1),
                      s => s.OrderBy(x => x.Value).Skip(3).Any(x => x.Value == 1), false);

    [Fact]
    public void AnyNoPredicate_AfterTake_IsTrue()
        => AssertSame(q => q.OrderBy(x => x.Value).Take(2).Any(),
                      s => s.OrderBy(x => x.Value).Take(2).Any(), true);

    [Fact]
    public void AnyNoPredicate_AfterTakeZero_IsFalse()
        => AssertSame(q => q.OrderBy(x => x.Value).Take(0).Any(),
                      s => s.OrderBy(x => x.Value).Take(0).Any(), false);

    [Fact]
    public void AnyNoPredicate_AfterSkipPastEnd_IsFalse()
        => AssertSame(q => q.OrderBy(x => x.Value).Skip(4).Any(),
                      s => s.OrderBy(x => x.Value).Skip(4).Any(), false);

    [Fact]
    public void AllPredicate_AfterSkip_Violated_IsFalse()
        => AssertSame(q => q.OrderBy(x => x.Value).Skip(3).All(x => x.Value < 3),
                      s => s.OrderBy(x => x.Value).Skip(3).All(x => x.Value < 3), false);

    [Fact]
    public void AllPredicate_AfterSkip_Satisfied_IsTrue()
        => AssertSame(q => q.OrderBy(x => x.Value).Skip(2).All(x => x.Value >= 3),
                      s => s.OrderBy(x => x.Value).Skip(2).All(x => x.Value >= 3), true);

    [Fact]
    public void AllPredicate_AfterTake_Satisfied_IsTrue()
        => AssertSame(q => q.OrderBy(x => x.Value).Take(2).All(x => x.Value < 3),
                      s => s.OrderBy(x => x.Value).Take(2).All(x => x.Value < 3), true);

    [Fact]
    public void AllPredicate_AfterTake_Violated_IsFalse()
        => AssertSame(q => q.OrderBy(x => x.Value).Take(3).All(x => x.Value < 3),
                      s => s.OrderBy(x => x.Value).Take(3).All(x => x.Value < 3), false);

    [Fact]
    public void Contains_AfterTake_OutsideWindow_IsFalse()
        => AssertSame(q => q.OrderBy(x => x.Value).Take(2).Select(x => x.Value).Contains(5),
                      s => s.OrderBy(x => x.Value).Take(2).Select(x => x.Value).Contains(5), false);

    [Fact]
    public void Contains_AfterTake_InsideWindow_IsTrue()
        => AssertSame(q => q.OrderBy(x => x.Value).Take(4).Select(x => x.Value).Contains(5),
                      s => s.OrderBy(x => x.Value).Take(4).Select(x => x.Value).Contains(5), true);

    [Fact]
    public void Contains_AfterSkip_InsideTail_IsTrue()
        => AssertSame(q => q.OrderBy(x => x.Value).Skip(3).Select(x => x.Value).Contains(5),
                      s => s.OrderBy(x => x.Value).Skip(3).Select(x => x.Value).Contains(5), true);

    [Fact]
    public void Contains_AfterSkip_OutsideTail_IsFalse()
        => AssertSame(q => q.OrderBy(x => x.Value).Skip(3).Select(x => x.Value).Contains(1),
                      s => s.OrderBy(x => x.Value).Skip(3).Select(x => x.Value).Contains(1), false);

    [Fact]
    public void AnyPredicate_AfterSkipTakeWindow_MatchesLinqToObjects()
        => AssertSame(q => q.OrderBy(x => x.Value).Skip(1).Take(2).Any(x => x.Value == 5),
                      s => s.OrderBy(x => x.Value).Skip(1).Take(2).Any(x => x.Value == 5), false);

    [Fact]
    public void Contains_AfterSkipTakeWindow_MatchesLinqToObjects()
        => AssertSame(q => q.OrderBy(x => x.Value).Skip(1).Take(2).Select(x => x.Value).Contains(3),
                      s => s.OrderBy(x => x.Value).Skip(1).Take(2).Select(x => x.Value).Contains(3), true);

    [Fact]
    public void AnyPredicate_AfterTakeDescending_MatchesLinqToObjects()
        => AssertSame(q => q.OrderByDescending(x => x.Value).Take(2).Any(x => x.Value == 1),
                      s => s.OrderByDescending(x => x.Value).Take(2).Any(x => x.Value == 1), false);

    [Fact]
    public void AnyPredicate_NoLimit_StillWorks()
        => AssertSame(q => q.Any(x => x.Value == 5),
                      s => s.Any(x => x.Value == 5), true);

    [Fact]
    public void AllPredicate_NoLimit_StillWorks()
        => AssertSame(q => q.All(x => x.Value > 0),
                      s => s.All(x => x.Value > 0), true);
}
