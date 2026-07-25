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

public class ContainsComputedProjectionTests
{
    [Fact]
    public void ContainsComputedProjection()
    {
        using TestDatabase db = new();
                db.Table<NullableEntity>().Schema.CreateTable();
                db.Table<NullableEntity>().AddRange(new[]
                {
                    new NullableEntity { Id = 1, Value = null },
                    new NullableEntity { Id = 2, Value = 3 }
                });
                NullableEntity[] seed =
                [
                    new NullableEntity { Id = 1, Value = null },
                    new NullableEntity { Id = 2, Value = 3 }
                ];
                bool oracle = seed.Select(e => e.Value ?? 7).Contains(7);
                bool actual = db.Table<NullableEntity>().Select(e => e.Value ?? 7).Contains(7);
                Assert.Equal(oracle, actual);
    }

}