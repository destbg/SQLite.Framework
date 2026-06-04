using System;
using System.Collections.Generic;
using System.Linq;
using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class ConcatNonStringColumnTests
{
    [Fact]
    public void ConcatNonStringColumn_00()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().Add(new Book { Id = 1, Title = "abc", AuthorId = 1, Price = 5.0 });

        string actual = db.Table<Book>().OrderBy(b => b.Id).Select(b => "x=" + (b.Price > 0)).First();

        Assert.Equal("x=1", actual);
    }
}
