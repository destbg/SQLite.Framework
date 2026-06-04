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

public class WindowSumAllNullTests
{
    [Fact]
    public void WindowSumAllNull()
    {
        using TestDatabase db = new();
                db.Table<TwoNullableIntEntity>().Schema.CreateTable();
                db.Table<TwoNullableIntEntity>().Add(new TwoNullableIntEntity { Id = 1, A = 1, B = null });
                db.Table<TwoNullableIntEntity>().Add(new TwoNullableIntEntity { Id = 2, A = 1, B = null });
                db.Table<TwoNullableIntEntity>().Add(new TwoNullableIntEntity { Id = 3, A = 2, B = 5 });

                List<int?> actual = db.Table<TwoNullableIntEntity>()
                    .Select(e => new { e.Id, V = SQLiteWindowFunctions.Sum(e.B).Over().PartitionBy(e.A).AsValue() })
                    .OrderBy(x => x.Id)
                    .Select(x => x.V)
                    .ToList();

                var rows = new[]
                {
                    new { Id = 1, A = 1, B = (int?)null },
                    new { Id = 2, A = 1, B = (int?)null },
                    new { Id = 3, A = 2, B = (int?)5 },
                };
                List<int?> oracle = rows
                    .OrderBy(x => x.Id)
                    .Select(r => rows.Where(x => x.A == r.A).Select(x => x.B).Sum())
                    .ToList();

                Assert.Equal(oracle, actual);
    }

}