using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class CorrelatedSubqueryTerminalThrowSemanticsTests
{
    private static List<Author> Authors() =>
    [
        new Author { Id = 1, Name = "a", Email = "a@x", BirthDate = new DateTime(1980, 1, 1) },
        new Author { Id = 2, Name = "b", Email = "b@x", BirthDate = new DateTime(1990, 1, 1) },
    ];

    private static List<Book> Books() =>
    [
        new Book { Id = 1, Title = "banana", AuthorId = 1, Price = 2.0 },
        new Book { Id = 2, Title = "apple", AuthorId = 1, Price = 4.0 },
    ];

    private static TestDatabase Seed()
    {
        TestDatabase db = new();
        db.Table<Author>().Schema.CreateTable();
        db.Table<Book>().Schema.CreateTable();
        db.Table<Author>().AddRange(Authors());
        db.Table<Book>().AddRange(Books());
        return db;
    }

    [Fact]
    public void AverageOverEmptySubqueryReturnsDefault()
    {
        using TestDatabase db = Seed();

        Exception? linqEx = Record.Exception(() => Authors()
            .Select(a => Books().Where(b => b.AuthorId == a.Id).Average(b => b.Price)).ToList());
        Assert.IsType<InvalidOperationException>(linqEx);

        List<double> actual = db.Table<Author>().OrderBy(a => a.Id)
            .Select(a => db.Table<Book>().Where(b => b.AuthorId == a.Id).Average(b => b.Price)).ToList();
        Assert.Equal([3.0, 0.0], actual);
    }

    [Fact]
    public void FirstOverEmptySubqueryReturnsDefault()
    {
        using TestDatabase db = Seed();

        Exception? linqEx = Record.Exception(() => Authors()
            .Select(a => Books().Where(b => b.AuthorId == a.Id).Select(b => b.Title).First()).ToList());
        Assert.IsType<InvalidOperationException>(linqEx);

        List<string?> actual = db.Table<Author>().OrderBy(a => a.Id)
            .Select(a => db.Table<Book>().Where(b => b.AuthorId == a.Id).Select(b => b.Title).First()).ToList()!;
        Assert.Equal(["banana", null], actual);
    }

    [Fact]
    public void SingleOverTwoRowSubqueryReturnsFirstRow()
    {
        using TestDatabase db = Seed();

        Exception? linqEx = Record.Exception(() => Authors().Where(a => a.Id == 1)
            .Select(a => Books().Where(b => b.AuthorId == a.Id).Select(b => b.Title).Single()).ToList());
        Assert.IsType<InvalidOperationException>(linqEx);

        List<string> actual = db.Table<Author>().Where(a => a.Id == 1)
            .Select(a => db.Table<Book>().Where(b => b.AuthorId == a.Id).Select(b => b.Title).Single()).ToList();
        Assert.Equal(["banana"], actual);
    }
}
