using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests.BugHunt;

public class PrintfFormatArgsBugTests
{
    [Fact]
    public void Printf_WithCapturedArray_SubstitutesArgs()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().Add(new Book { Id = 1, Title = "x", AuthorId = 1, Price = 1 });

        object[] args = [7, "Alpha"];

        string result = db.Table<Book>()
            .Select(b => SQLiteFunctions.Printf("Book %d: %s", args))
            .First();

        Assert.Equal("Book 7: Alpha", result);
    }

    [Fact]
    public void Format_WithCapturedArray_SubstitutesArgs()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().Add(new Book { Id = 1, Title = "x", AuthorId = 1, Price = 1 });

        object[] args = [42];

        string result = db.Table<Book>()
            .Select(b => SQLiteFunctions.Format("N=%d", args))
            .First();

        Assert.Equal("N=42", result);
    }
}
