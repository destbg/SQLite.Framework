using System.ComponentModel.DataAnnotations;
using SQLite.Framework;
using SQLite.Framework.Attributes;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class ReturningCapturedPlayer
{
    [Key][AutoIncrement] public int Id { get; set; }
    public int Score { get; set; }
}

public class ReturningCapturedVariableParityTests
{
    [Fact]
    public void ReturningProjection_ReevaluatesCapturedVariableOnEachCall()
    {
        using TestDatabase db = new();
        db.Table<ReturningCapturedPlayer>().Schema.CreateTable();

        int threshold = 10;
        SQLiteReturningTable<ReturningCapturedPlayer, bool> ret = db.Table<ReturningCapturedPlayer>().Returning(x => x.Score > threshold);

        bool first = ret.Add(new ReturningCapturedPlayer { Score = 50 });
        threshold = 200;
        bool second = ret.Add(new ReturningCapturedPlayer { Score = 100 });

        Assert.True(first);
        Assert.False(second);
    }
}
