using SQLite.Framework;
using SQLite.Framework.Attributes;
using SQLite.Framework.Enums;
using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Helpers;
using SQLite.Framework.Tests.Entities;
using System.Globalization;

namespace SQLite.Framework.Tests;

public class AllPredicateAfterSkipBugTests
{
    [Fact]
    public void AllPredicateAfterSkipIgnoresLimit()
    {
        using TestDatabase db = new();
        db.Table<whereAggAllSkipRow>().Schema.CreateTable();
        db.Table<whereAggAllSkipRow>().Add(new whereAggAllSkipRow { Id = 1, Value = 1 });
        db.Table<whereAggAllSkipRow>().Add(new whereAggAllSkipRow { Id = 2, Value = 2 });
        db.Table<whereAggAllSkipRow>().Add(new whereAggAllSkipRow { Id = 3, Value = 3 });
        db.Table<whereAggAllSkipRow>().Add(new whereAggAllSkipRow { Id = 4, Value = 5 });
        List<whereAggAllSkipRow> mem = new()
        {
            new whereAggAllSkipRow { Id = 1, Value = 1 },
            new whereAggAllSkipRow { Id = 2, Value = 2 },
            new whereAggAllSkipRow { Id = 3, Value = 3 },
            new whereAggAllSkipRow { Id = 4, Value = 5 },
        };
        bool oracle = mem.OrderBy(x => x.Value).Skip(3).All(x => x.Value < 3);
        bool actual = db.Table<whereAggAllSkipRow>().OrderBy(x => x.Value).Skip(3).All(x => x.Value < 3);
        Assert.Equal(oracle, actual);
    }
}

public class whereAggAllSkipRow { [System.ComponentModel.DataAnnotations.Key] public int Id { get; set; } public int Value { get; set; } }
