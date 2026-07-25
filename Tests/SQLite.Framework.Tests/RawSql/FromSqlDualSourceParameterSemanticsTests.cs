using System;
using System.Collections.Generic;
using System.Linq;
using SQLite.Framework;
using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class FromSqlDualSourceParameterSemanticsTests
{
    [Fact]
    public void TwoFromSqlSources_SameParameterName_ShareLastValue()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        db.Table<Author>().Schema.CreateTable();
        db.Table<Book>().Add(new Book { Id = 1, Title = "a", AuthorId = 1, Price = 1 });
        db.Table<Book>().Add(new Book { Id = 2, Title = "b", AuthorId = 2, Price = 2 });
        db.Table<Author>().Add(new Author { Id = 1, Name = "n1", Email = "e1", BirthDate = new DateTime(2000, 1, 1) });
        db.Table<Author>().Add(new Author { Id = 2, Name = "n2", Email = "e2", BirthDate = new DateTime(2000, 1, 1) });

        List<int> actual = (
                from b in db.FromSql<Book>(
                    "SELECT * FROM \"Books\" WHERE \"BookId\" = @x",
                    new SQLiteParameter { Name = "@x", Value = 1 })
                from a in db.FromSql<Author>(
                    "SELECT * FROM \"Authors\" WHERE \"AuthorId\" = @x",
                    new SQLiteParameter { Name = "@x", Value = 2 })
                select b.Id)
            .ToList();

        Assert.Equal([2], actual);
    }
}
