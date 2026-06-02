using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests.BugHunt;

public class ElementAtPastEndBugTests
{
    [Fact]
    public void ElementAtFarPastEnd_ThrowsArgumentOutOfRangeLikeBcl()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().Add(new Book { Id = 1, Title = "A", AuthorId = 1, Price = 10 });
        db.Table<Book>().Add(new Book { Id = 2, Title = "B", AuthorId = 2, Price = 30 });
        db.Table<Book>().Add(new Book { Id = 3, Title = "C", AuthorId = 1, Price = 20 });

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            db.Table<Book>().OrderBy(b => b.Id).Select(b => b.Id).ElementAt(99));
    }

    [Fact]
    public void ElementAtJustPastEnd_ThrowsArgumentOutOfRangeLikeBcl()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().Add(new Book { Id = 1, Title = "A", AuthorId = 1, Price = 10 });
        db.Table<Book>().Add(new Book { Id = 2, Title = "B", AuthorId = 2, Price = 30 });
        db.Table<Book>().Add(new Book { Id = 3, Title = "C", AuthorId = 1, Price = 20 });

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            db.Table<Book>().OrderBy(b => b.Id).Select(b => b.Id).ElementAt(3));
    }
}
