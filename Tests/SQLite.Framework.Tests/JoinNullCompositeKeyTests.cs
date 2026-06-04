using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using SQLite.Framework;
using SQLite.Framework.Enums;
using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Enums;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class JoinNullCompositeKeyTests
{
    [Fact]
    public void JoinNullCompositeKey()
    {
        using TestDatabase db = new();
        db.Table<TwoNullableIntEntity>().Schema.CreateTable();
        db.Table<TwoNullableIntEntity>().Add(new TwoNullableIntEntity { Id = 1, A = null, B = 7 });
        db.Table<TwoNullableIntEntity>().Add(new TwoNullableIntEntity { Id = 2, A = null, B = 7 });

        List<(int XId, int YId)> actual = (from x in db.Table<TwoNullableIntEntity>()
                join y in db.Table<TwoNullableIntEntity>() on new { x.A, x.B } equals new { y.A, y.B }
                select new { XId = x.Id, YId = y.Id })
            .ToList()
            .Select(p => (p.XId, p.YId))
            .OrderBy(p => p.XId).ThenBy(p => p.YId)
            .ToList();

        var left = new[]
        {
            new { Id = 1, A = (int?)null, B = (int?)7 },
            new { Id = 2, A = (int?)null, B = (int?)7 },
        };
        List<(int XId, int YId)> oracle = (from x in left
                join y in left on new { x.A, x.B } equals new { y.A, y.B }
                select new { XId = x.Id, YId = y.Id })
            .ToList()
            .Select(p => (p.XId, p.YId))
            .OrderBy(p => p.XId).ThenBy(p => p.YId)
            .ToList();

        Assert.Equal(oracle, actual);
    }

}