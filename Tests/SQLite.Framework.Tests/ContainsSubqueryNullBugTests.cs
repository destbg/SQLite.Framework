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

public class ContainsSubqueryNullBugTests
{
    [Fact]
    public void ContainsSubqueryNull_00()
    {
        using TestDatabase db = new();
                db.Table<NullableEntity>().Schema.CreateTable();
                db.Schema.CreateTable<Author>();
                db.Table<NullableEntity>().Add(new NullableEntity { Id = 1, Value = 5 });
                db.Table<Author>().Add(new Author { Id = 5, Name = "x", Email = "e", BirthDate = new DateTime(2000, 1, 1) });
                db.Table<Author>().Add(new Author { Id = 6, Name = "y", Email = "e", BirthDate = new DateTime(2000, 1, 1) });

                List<NullableEntity> memN = new() { new NullableEntity { Id = 1, Value = 5 } };
                List<Author> memA = new()
                {
                    new Author { Id = 5, Name = "x", Email = "e", BirthDate = new DateTime(2000, 1, 1) },
                    new Author { Id = 6, Name = "y", Email = "e", BirthDate = new DateTime(2000, 1, 1) }
                };

                List<int> oracle = memA.Where(a => memN.Select(n => n.Value).Contains(a.Id)).Select(a => a.Id).OrderBy(i => i).ToList();
                List<int> actual = db.Table<Author>().Where(a => db.Table<NullableEntity>().Select(n => n.Value).Contains(a.Id)).Select(a => a.Id).OrderBy(i => i).ToList();

                Assert.Equal(oracle, actual);
    }

    [Fact]
    public void ContainsSubqueryNull_01()
    {
        using TestDatabase db = new();
                db.Table<TwoNullableIntEntity>().Schema.CreateTable();
                db.Table<TwoNullableIntEntity>().Add(new TwoNullableIntEntity { Id = 1, A = null, B = null });
                db.Table<TwoNullableIntEntity>().Add(new TwoNullableIntEntity { Id = 2, A = 5, B = 5 });

                List<TwoNullableIntEntity> mem = new()
                {
                    new TwoNullableIntEntity { Id = 1, A = null, B = null },
                    new TwoNullableIntEntity { Id = 2, A = 5, B = 5 }
                };

                List<int> oracle = mem.Where(x => mem.Select(y => y.B).Contains(x.A)).Select(x => x.Id).OrderBy(i => i).ToList();
                List<int> actual = db.Table<TwoNullableIntEntity>().Where(x => db.Table<TwoNullableIntEntity>().Select(y => y.B).Contains(x.A)).Select(x => x.Id).OrderBy(i => i).ToList();

                Assert.Equal(oracle, actual);
    }

    [Fact]
    public void ContainsSubqueryNull_02()
    {
        using TestDatabase db = new();
                db.Table<NullableStringEntity>().Schema.CreateTable();
                db.CreateCommand("INSERT INTO NullableStringEntity (\"Id\",\"Name\") VALUES (1,NULL),(2,'a')", []).ExecuteNonQuery();

                List<NullableStringEntity> mem = new()
                {
                    new NullableStringEntity { Id = 1, Name = null },
                    new NullableStringEntity { Id = 2, Name = "a" }
                };

                List<int> oracle = mem.Where(x => mem.Select(y => y.Name).Contains(x.Name)).Select(x => x.Id).OrderBy(i => i).ToList();
                List<int> actual = db.Table<NullableStringEntity>().Where(x => db.Table<NullableStringEntity>().Select(y => y.Name).Contains(x.Name)).Select(x => x.Id).OrderBy(i => i).ToList();

                Assert.Equal(oracle, actual);
    }

}