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

public class StringCompareIgnoreCaseSignTests
{
    [Fact]
    public void StringCompareIgnoreCaseSign_00()
    {
        using TestDatabase db = new();
                db.Table<Book>().Schema.CreateTable();
                db.Table<Book>().Add(new Book { Id = 1, Title = "_", AuthorId = 1, Price = 1 });

                int oracle = Math.Sign(string.Compare("_", "a", StringComparison.OrdinalIgnoreCase));
                int actual = Math.Sign(db.Table<Book>().Where(b => b.Id == 1).Select(b => string.Compare(b.Title, "a", StringComparison.OrdinalIgnoreCase)).First());

                Assert.Equal(oracle, actual);
    }

    [Fact]
    public void StringCompareIgnoreCaseSign_01()
    {
        using TestDatabase db = new();
                db.Table<Book>().Schema.CreateTable();
                db.Table<Book>().Add(new Book { Id = 1, Title = "_", AuthorId = 1, Price = 1 });

                int oracle = Math.Sign(string.Compare("_", 0, "a", 0, 1, StringComparison.OrdinalIgnoreCase));
                int actual = Math.Sign(db.Table<Book>().Where(b => b.Id == 1).Select(b => string.Compare(b.Title, 0, "a", 0, 1, StringComparison.OrdinalIgnoreCase)).First());

                Assert.Equal(oracle, actual);
    }

}