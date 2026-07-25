using System;
using System.Collections.Generic;
using System.Linq;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Helpers;
using Xunit;

namespace SQLite.Framework.Tests;

[Table("AliasExprChkCastRows")]
public class AliasExprChkCastRow
{
    [Key]
    public int Id { get; set; }

    public byte ByteValue { get; set; }
}

public class AliasExprCheckedCastOverflowParityTests
{
    [Fact]
    public void CapturedCheckedCastOutOfRange_MatchesLinqToObjects()
    {
        using TestDatabase db = new();
        db.Table<AliasExprChkCastRow>().Schema.CreateTable();
        db.Table<AliasExprChkCastRow>().Add(new AliasExprChkCastRow { Id = 1, ByteValue = 44 });
        db.Table<AliasExprChkCastRow>().Add(new AliasExprChkCastRow { Id = 2, ByteValue = 10 });

        int captured = 300;

        AliasExprChkCastRow[] seed =
        [
            new AliasExprChkCastRow { Id = 1, ByteValue = 44 },
            new AliasExprChkCastRow { Id = 2, ByteValue = 10 }
        ];

        Assert.Throws<OverflowException>(() =>
            seed.Where(x => x.ByteValue == checked((byte)captured)).Select(x => x.Id).ToList());

        Assert.Throws<OverflowException>(() =>
            db.Table<AliasExprChkCastRow>().Where(x => x.ByteValue == checked((byte)captured)).Select(x => x.Id).ToList());
    }
}
