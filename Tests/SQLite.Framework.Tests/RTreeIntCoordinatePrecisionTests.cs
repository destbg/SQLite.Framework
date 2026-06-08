using System.ComponentModel.DataAnnotations;
using SQLite.Framework.Attributes;
using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[RTreeIndex]
file sealed class IntBoundCell
{
    [Key]
    public int Id { get; set; }

    [RTreeMin("X")]
    public int Lo { get; set; }

    [RTreeMax("X")]
    public int Hi { get; set; }
}

public class RTreeIntCoordinatePrecisionTests
{
    [Fact]
    public void IntCoordinateLosesPrecisionAbove2Pow24()
    {
        using TestDatabase db = new();
        db.Table<IntBoundCell>().Schema.CreateTable();
        db.Table<IntBoundCell>().Add(new IntBoundCell { Id = 1, Lo = 16777217, Hi = 16777217 });

        List<IntBoundCell> seed = [new IntBoundCell { Id = 1, Lo = 16777217, Hi = 16777217 }];

        int actual = db.Table<IntBoundCell>().First().Lo;

        Assert.Equal(16777217, seed.First().Lo);
        Assert.Equal(16777216, actual);
    }
}
