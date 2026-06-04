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

public class ObjectEqualsValueColumnBugTests
{
    [Fact]
    public void ObjectEqualsValueColumn()
    {
        using TestDatabase db = new();
                db.Table<TwoNullableIntEntity>().Schema.CreateTable();
                (int id, int? a, int? b)[] rows = [(1, null, null), (2, 5, 5), (3, 5, 7), (4, null, 5)];
                foreach ((int rid, int? ra, int? rb) in rows)
                {
                    db.Table<TwoNullableIntEntity>().Add(new TwoNullableIntEntity { Id = rid, A = ra, B = rb });
                }

                List<int> oracle = rows.Where(x => Equals(x.a, x.b)).Select(x => x.id).OrderBy(i => i).ToList();
                List<int> actual = db.Table<TwoNullableIntEntity>().Where(x => Equals(x.A, x.B)).Select(x => x.Id).OrderBy(i => i).ToList();

                Assert.Equal(oracle, actual);
    }

}