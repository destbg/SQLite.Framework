using SQLite.Framework;
using SQLite.Framework.Attributes;
using SQLite.Framework.Enums;
using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Helpers;
using SQLite.Framework.Tests.Entities;
using System.Globalization;

namespace SQLite.Framework.Tests;

public class AnyPredicateAfterTakeBugTests
{
    [Fact]
    public void AnyPredicateAfterTakeIgnoresLimit()
    {
        using TestDatabase db = new();
        db.Table<whereAggAnyTakeRow>().Schema.CreateTable();
        db.Table<whereAggAnyTakeRow>().Add(new whereAggAnyTakeRow { Id = 1, Value = 1 });
        db.Table<whereAggAnyTakeRow>().Add(new whereAggAnyTakeRow { Id = 2, Value = 2 });
        db.Table<whereAggAnyTakeRow>().Add(new whereAggAnyTakeRow { Id = 3, Value = 3 });
        db.Table<whereAggAnyTakeRow>().Add(new whereAggAnyTakeRow { Id = 4, Value = 5 });
        List<whereAggAnyTakeRow> mem = new()
        {
            new whereAggAnyTakeRow { Id = 1, Value = 1 },
            new whereAggAnyTakeRow { Id = 2, Value = 2 },
            new whereAggAnyTakeRow { Id = 3, Value = 3 },
            new whereAggAnyTakeRow { Id = 4, Value = 5 },
        };
        bool oracle = mem.OrderBy(x => x.Value).Take(2).Any(x => x.Value == 5);
        bool actual = db.Table<whereAggAnyTakeRow>().OrderBy(x => x.Value).Take(2).Any(x => x.Value == 5);
        Assert.Equal(oracle, actual);
    }
}

public class whereAggAnyTakeRow { [System.ComponentModel.DataAnnotations.Key] public int Id { get; set; } public int Value { get; set; } }
