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

public class DistinctCountNullTests
{
    [Fact]
    public void DistinctCountNull_00()
    {
        using TestDatabase db = new();
                db.Table<NullableEntity>().Schema.CreateTable();
                db.Table<NullableEntity>().AddRange(new[]
                {
                    new NullableEntity { Id = 1, Value = 10 },
                    new NullableEntity { Id = 2, Value = null },
                    new NullableEntity { Id = 3, Value = 10 },
                    new NullableEntity { Id = 4, Value = 20 }
                });
                NullableEntity[] seed =
                [
                    new NullableEntity { Id = 1, Value = 10 },
                    new NullableEntity { Id = 2, Value = null },
                    new NullableEntity { Id = 3, Value = 10 },
                    new NullableEntity { Id = 4, Value = 20 }
                ];
                int oracle = seed.Select(e => e.Value).Distinct().Count();
                int actual = db.Table<NullableEntity>().Select(e => e.Value).Distinct().Count();
                Assert.Equal(oracle, actual);
    }

    [Fact]
    public void DistinctCountNull_01()
    {
        using TestDatabase db = new();
                db.Table<NullableEntity>().Schema.CreateTable();
                db.Table<NullableEntity>().AddRange(new[]
                {
                    new NullableEntity { Id = 1, Value = null },
                    new NullableEntity { Id = 2, Value = null }
                });
                NullableEntity[] seed =
                [
                    new NullableEntity { Id = 1, Value = null },
                    new NullableEntity { Id = 2, Value = null }
                ];
                int oracle = seed.Select(e => e.Value).Distinct().Count();
                int actual = db.Table<NullableEntity>().Select(e => e.Value).Distinct().Count();
                Assert.Equal(oracle, actual);
    }

    [Fact]
    public void DistinctCountNull_02()
    {
        using TestDatabase db = new();
                db.Table<NullableEntity>().Schema.CreateTable();
                db.Table<NullableEntity>().AddRange(new[]
                {
                    new NullableEntity { Id = 1, Value = 10 },
                    new NullableEntity { Id = 2, Value = null },
                    new NullableEntity { Id = 3, Value = 10 },
                    new NullableEntity { Id = 4, Value = 20 }
                });
                NullableEntity[] seed =
                [
                    new NullableEntity { Id = 1, Value = 10 },
                    new NullableEntity { Id = 2, Value = null },
                    new NullableEntity { Id = 3, Value = 10 },
                    new NullableEntity { Id = 4, Value = 20 }
                ];
                int oracle = seed.Select(e => e.Value).Distinct().Count(v => v == null || v > 5);
                int actual = db.Table<NullableEntity>().Select(e => e.Value).Distinct().Count(v => v == null || v > 5);
                Assert.Equal(oracle, actual);
    }

}