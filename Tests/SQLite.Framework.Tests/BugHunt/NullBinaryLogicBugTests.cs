using System.ComponentModel.DataAnnotations;
using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests.BugHunt;

internal sealed class BugHuntBoolPair
{
    [Key]
    public int Id { get; set; }

    public bool? F1 { get; set; }

    public bool? F2 { get; set; }
}

public class NullBinaryLogicBugTests
{
    [Fact]
    public void BitwiseAnd_OnNullableBoolColumns_MatchesLinqToObjects()
    {
        using TestDatabase db = new();
        db.Table<BugHuntBoolPair>().Schema.CreateTable();
        db.Table<BugHuntBoolPair>().Add(new BugHuntBoolPair { Id = 1, F1 = null, F2 = false });
        db.Table<BugHuntBoolPair>().Add(new BugHuntBoolPair { Id = 2, F1 = true, F2 = false });

        BugHuntBoolPair[] seed =
        [
            new BugHuntBoolPair { Id = 1, F1 = null, F2 = false },
            new BugHuntBoolPair { Id = 2, F1 = true, F2 = false },
        ];

        List<bool?> expected = seed.OrderBy(x => x.Id).Select(x => x.F1 & x.F2).ToList();
        List<bool?> actual = db.Table<BugHuntBoolPair>().OrderBy(x => x.Id).Select(x => x.F1 & x.F2).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void BitwiseOr_OnNullableBoolColumns_MatchesLinqToObjects()
    {
        using TestDatabase db = new();
        db.Table<BugHuntBoolPair>().Schema.CreateTable();
        db.Table<BugHuntBoolPair>().Add(new BugHuntBoolPair { Id = 1, F1 = null, F2 = true });
        db.Table<BugHuntBoolPair>().Add(new BugHuntBoolPair { Id = 2, F1 = false, F2 = true });

        BugHuntBoolPair[] seed =
        [
            new BugHuntBoolPair { Id = 1, F1 = null, F2 = true },
            new BugHuntBoolPair { Id = 2, F1 = false, F2 = true },
        ];

        List<bool?> expected = seed.OrderBy(x => x.Id).Select(x => x.F1 | x.F2).ToList();
        List<bool?> actual = db.Table<BugHuntBoolPair>().OrderBy(x => x.Id).Select(x => x.F1 | x.F2).ToList();

        Assert.Equal(expected, actual);
    }
}
