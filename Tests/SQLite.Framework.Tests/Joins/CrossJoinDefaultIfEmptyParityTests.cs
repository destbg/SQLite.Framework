using System;
using System.Linq;
using System.ComponentModel.DataAnnotations;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

internal sealed class CrossDieLeft
{
    [Key]
    public int Id { get; set; }
}

internal sealed class CrossDieRight
{
    [Key]
    public int Id { get; set; }
}

public class CrossJoinDefaultIfEmptyParityTests
{
    [Fact]
    public void CrossJoinWithBareDefaultIfEmptySecondSource_ThrowsNotSupported()
    {
        using TestDatabase db = new();
        db.Table<CrossDieLeft>().Schema.CreateTable();
        db.Table<CrossDieRight>().Schema.CreateTable();

        db.Table<CrossDieLeft>().Add(new CrossDieLeft { Id = 1 });
        db.Table<CrossDieRight>().Add(new CrossDieRight { Id = 10 });

        Assert.Throws<NotSupportedException>(() =>
            (from a in db.Table<CrossDieLeft>()
             from b in db.Table<CrossDieRight>().DefaultIfEmpty()
             select new { A = a.Id, B = b == null ? 0 : b.Id })
                .ToList());
    }
}
