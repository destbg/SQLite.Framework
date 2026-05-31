using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class ReverseAndDateBugTests
{
    [Fact]
    public void ReverseBeforeSubqueryForcingOperatorThrowsClearError()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        db.Table<Author>().Schema.CreateTable();
        db.Table<Book>().Add(new Book { Id = 1, Title = "a", AuthorId = 1, Price = 1 });
        db.Table<Author>().Add(new Author { Id = 1, Name = "x", Email = "e", BirthDate = new DateTime(2000, 1, 1) });

        Assert.Throws<NotSupportedException>(() =>
            db.Table<Book>().OrderBy(b => b.Id).Reverse()
                .SelectMany(_ => db.Table<Author>(), (b, a) => new { b.Id, a.Name })
                .ToList());
    }

    [Fact]
    public void AddMonthsPreservesSubSecondTicks()
    {
        using TestDatabase db = new();
        db.Table<Author>().Schema.CreateTable();
        DateTime birth = new DateTime(2000, 1, 15, 1, 2, 3).AddTicks(5);
        db.Table<Author>().Add(new Author { Id = 1, Name = "x", Email = "e", BirthDate = birth });

        DateTime result = db.Table<Author>().Where(a => a.Id == 1).Select(a => a.BirthDate.AddMonths(1)).First();

        Assert.Equal(birth.AddMonths(1), result);
    }
}
