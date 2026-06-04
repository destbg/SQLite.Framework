using System;
using System.Collections.Generic;
using System.Linq;
using SQLite.Framework;
using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

file sealed class ReverseConcatRow
{
    [System.ComponentModel.DataAnnotations.Key]
    public int Id { get; set; }
}

public class ReverseBeforeConcatTests
{
    [Fact]
    public void ReverseBeforeConcatReversesOnlyTheLeftOperand()
    {
        using TestDatabase db = new();
        db.Table<ReverseConcatRow>().Schema.CreateTable();

        List<ReverseConcatRow> rows =
        [
            new ReverseConcatRow { Id = 1 },
            new ReverseConcatRow { Id = 2 },
            new ReverseConcatRow { Id = 3 },
            new ReverseConcatRow { Id = 4 },
            new ReverseConcatRow { Id = 5 },
        ];
        db.Table<ReverseConcatRow>().AddRange(rows);

        List<int> oracle = rows
            .Select(x => x.Id)
            .Where(x => x <= 3)
            .Reverse()
            .Concat(rows.Select(x => x.Id).Where(x => x >= 4))
            .ToList();

        List<int> actual = db.Table<ReverseConcatRow>()
            .Select(x => x.Id)
            .Where(x => x <= 3)
            .Reverse()
            .Concat(db.Table<ReverseConcatRow>().Select(x => x.Id).Where(x => x >= 4))
            .ToList();

        Assert.Equal(oracle, actual);
    }
}
