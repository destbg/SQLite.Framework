using System;
using System.Linq;
using SQLite.Framework.Exceptions;
using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class CorrelatedSelectManySemanticsTests
{
    [Fact]
    public void CorrelatedSelectMany_InnerSourceFilter_Throws()
    {
        using TestDatabase db = new();
        db.Table<Author>().Schema.CreateTable();
        db.Table<Book>().Schema.CreateTable();
        db.Table<Author>().Add(new Author { Id = 1, Name = "A1", Email = "a1", BirthDate = new DateTime(2000, 1, 1) });
        db.Table<Book>().Add(new Book { Id = 10, Title = "b1", AuthorId = 1, Price = 1 });

        Assert.Throws<SQLiteException>(() => (
                from a in db.Table<Author>()
                from b in db.Table<Book>().Where(b => b.AuthorId == a.Id)
                select a.Name + ":" + b.Title)
            .ToList());
    }
}
