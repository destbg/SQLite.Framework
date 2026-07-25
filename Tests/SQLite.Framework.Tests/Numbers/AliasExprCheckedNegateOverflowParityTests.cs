using System;
using System.Collections.Generic;
using System.Linq;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Helpers;
using Xunit;

namespace SQLite.Framework.Tests;

[Table("AliasExprChkNegRows")]
public class AliasExprChkNegRow
{
    [Key]
    public int Id { get; set; }

    public int IntValue { get; set; }
}

public class AliasExprCheckedNegateOverflowParityTests
{
    [Fact]
    public void CapturedCheckedNegateMinValue_MatchesLinqToObjects()
    {
        using TestDatabase db = new();
        db.Table<AliasExprChkNegRow>().Schema.CreateTable();
        db.Table<AliasExprChkNegRow>().Add(new AliasExprChkNegRow { Id = 1, IntValue = int.MinValue });
        db.Table<AliasExprChkNegRow>().Add(new AliasExprChkNegRow { Id = 2, IntValue = 5 });

        int captured = int.MinValue;

        AliasExprChkNegRow[] seed =
        [
            new AliasExprChkNegRow { Id = 1, IntValue = int.MinValue },
            new AliasExprChkNegRow { Id = 2, IntValue = 5 }
        ];

        Assert.Throws<OverflowException>(() =>
            seed.Where(n => n.IntValue == checked(-captured)).Select(n => n.Id).ToList());

        Assert.Throws<OverflowException>(() =>
            db.Table<AliasExprChkNegRow>().Where(n => n.IntValue == checked(-captured)).Select(n => n.Id).ToList());
    }

    [Fact]
    public void CapturedCheckedNegateLongMinValue_MatchesLinqToObjects()
    {
        using TestDatabase db = new();
        db.Table<AliasExprChkNegRow>().Schema.CreateTable();
        db.Table<AliasExprChkNegRow>().Add(new AliasExprChkNegRow { Id = 1, IntValue = 5 });

        long captured = long.MinValue;

        AliasExprChkNegRow[] seed = [new AliasExprChkNegRow { Id = 1, IntValue = 5 }];

        Assert.Throws<OverflowException>(() =>
            seed.Where(n => n.IntValue == (int)checked(-captured)).Select(n => n.Id).ToList());

        Assert.Throws<OverflowException>(() =>
            db.Table<AliasExprChkNegRow>().Where(n => n.IntValue == (int)checked(-captured)).Select(n => n.Id).ToList());
    }
}
