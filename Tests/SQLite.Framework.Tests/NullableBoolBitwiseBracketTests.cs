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

public class NullableBoolBitwiseBracketTests
{
    [Fact]
    public void NullableBoolBitwiseBracket_00()
    {
        using TestDatabase db = new();
                db.Table<TwoNullableBoolEntity>().Schema.CreateTable();
                bool?[] values = [null, false, true];
                List<TwoNullableBoolEntity> rows = [];
                int id = 1;
                foreach (bool? a in values)
                {
                    foreach (bool? b in values)
                    {
                        rows.Add(new TwoNullableBoolEntity { Id = id++, A = a, B = b });
                    }
                }
                foreach (TwoNullableBoolEntity r in rows)
                {
                    db.Table<TwoNullableBoolEntity>().Add(r);
                }

                List<int> oracle = rows.Where(x => (x.A & x.B) == false).Select(x => x.Id).OrderBy(i => i).ToList();
                List<int> actual = db.Table<TwoNullableBoolEntity>().Where(x => (x.A & x.B) == false).Select(x => x.Id).OrderBy(i => i).ToList();

                Assert.Equal(oracle, actual);
    }

    [Fact]
    public void NullableBoolBitwiseBracket_01()
    {
        using TestDatabase db = new();
                db.Table<TwoNullableBoolEntity>().Schema.CreateTable();
                bool?[] values = [null, false, true];
                List<TwoNullableBoolEntity> rows = [];
                int id = 1;
                foreach (bool? a in values)
                {
                    foreach (bool? b in values)
                    {
                        rows.Add(new TwoNullableBoolEntity { Id = id++, A = a, B = b });
                    }
                }
                foreach (TwoNullableBoolEntity r in rows)
                {
                    db.Table<TwoNullableBoolEntity>().Add(r);
                }

                List<int> oracle = rows.Where(x => (x.A & x.B) != true).Select(x => x.Id).OrderBy(i => i).ToList();
                List<int> actual = db.Table<TwoNullableBoolEntity>().Where(x => (x.A & x.B) != true).Select(x => x.Id).OrderBy(i => i).ToList();

                Assert.Equal(oracle, actual);
    }

    [Fact]
    public void NullableBoolBitwiseBracket_02()
    {
        using TestDatabase db = new();
                db.Table<TwoNullableBoolEntity>().Schema.CreateTable();
                bool?[] values = [null, false, true];
                List<TwoNullableBoolEntity> rows = [];
                int id = 1;
                foreach (bool? a in values)
                {
                    foreach (bool? b in values)
                    {
                        rows.Add(new TwoNullableBoolEntity { Id = id++, A = a, B = b });
                    }
                }
                foreach (TwoNullableBoolEntity r in rows)
                {
                    db.Table<TwoNullableBoolEntity>().Add(r);
                }

                List<bool?> oracle = rows.OrderBy(x => x.Id).Select(x => (x.A & x.B) ^ (x.A | x.B)).ToList();
                List<bool?> actual = db.Table<TwoNullableBoolEntity>().OrderBy(x => x.Id).Select(x => (x.A & x.B) ^ (x.A | x.B)).ToList();

                Assert.Equal(oracle, actual);
    }

}