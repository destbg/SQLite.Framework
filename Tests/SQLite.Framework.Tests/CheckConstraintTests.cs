using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class CheckConstraintTests
{
    [Fact]
    public void Check_AllowsValuesThatSatisfyConstraint()
    {
        using TestDatabase db = new();
        db.Schema.Table<BookArchive>()
            .Check(b => b.Price > 0, name: "CK_Price_Positive")
            .CreateTable();

        db.Table<BookArchive>().Add(new BookArchive
        {
            Id = 1,
            Title = "ok",
            AuthorId = 1,
            Price = 10
        });

        Assert.Single(db.Table<BookArchive>().ToList());
    }

    [Fact]
    public void Check_RejectsValuesThatViolateConstraint()
    {
        using TestDatabase db = new();
        db.Schema.Table<BookArchive>()
            .Check(b => b.Price > 0, name: "CK_Price_Positive")
            .CreateTable();

        Assert.ThrowsAny<Exception>(() => db.Table<BookArchive>().Add(new BookArchive
        {
            Id = 1,
            Title = "bad",
            AuthorId = 1,
            Price = 0
        }));
    }

    [Fact]
    public void Check_NoName_StillEnforced()
    {
        using TestDatabase db = new();
        db.Schema.Table<BookArchive>()
            .Check(b => b.Price > 0)
            .CreateTable();

        Assert.ThrowsAny<Exception>(() => db.Table<BookArchive>().Add(new BookArchive
        {
            Id = 1,
            Title = "bad",
            AuthorId = 1,
            Price = -1
        }));
    }

    [Fact]
    public void Check_MultipleConstraintsAllEnforced()
    {
        using TestDatabase db = new();
        db.Schema.Table<BookArchive>()
            .Check(b => b.Price > 0, name: "CK_Price")
            .Check(b => b.AuthorId > 0, name: "CK_Author")
            .CreateTable();

        Assert.ThrowsAny<Exception>(() => db.Table<BookArchive>().Add(new BookArchive
        {
            Id = 1,
            Title = "bad",
            AuthorId = 0,
            Price = 1
        }));
    }
}
