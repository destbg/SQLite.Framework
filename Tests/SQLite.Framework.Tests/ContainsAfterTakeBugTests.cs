using SQLite.Framework;
using SQLite.Framework.Attributes;
using SQLite.Framework.Enums;
using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Helpers;
using SQLite.Framework.Tests.Entities;
using System.Globalization;

namespace SQLite.Framework.Tests;

public class ContainsAfterTakeBugTests
{
    [Fact]
    public void ContainsAfterTakeIgnoresLimit()
    {
        using TestDatabase db = new();
        db.Table<whereAggContainsTakeRow>().Schema.CreateTable();
        db.Table<whereAggContainsTakeRow>().Add(new whereAggContainsTakeRow { Id = 1, Value = 1 });
        db.Table<whereAggContainsTakeRow>().Add(new whereAggContainsTakeRow { Id = 2, Value = 2 });
        db.Table<whereAggContainsTakeRow>().Add(new whereAggContainsTakeRow { Id = 3, Value = 3 });
        db.Table<whereAggContainsTakeRow>().Add(new whereAggContainsTakeRow { Id = 4, Value = 5 });
        List<whereAggContainsTakeRow> mem = new()
        {
            new whereAggContainsTakeRow { Id = 1, Value = 1 },
            new whereAggContainsTakeRow { Id = 2, Value = 2 },
            new whereAggContainsTakeRow { Id = 3, Value = 3 },
            new whereAggContainsTakeRow { Id = 4, Value = 5 },
        };
        bool oracle = mem.OrderBy(x => x.Value).Take(2).Select(x => x.Value).Contains(5);
        bool actual = db.Table<whereAggContainsTakeRow>().OrderBy(x => x.Value).Take(2).Select(x => x.Value).Contains(5);
        Assert.Equal(oracle, actual);
    }
}

public class whereAggContainsTakeRow { [System.ComponentModel.DataAnnotations.Key] public int Id { get; set; } public int Value { get; set; } }
