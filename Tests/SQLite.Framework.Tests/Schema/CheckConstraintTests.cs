using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class CheckConstraintTests
{
    [Fact]
    public void Check_AllowsValuesThatSatisfyConstraint()
    {
        using ModelTestDatabase db = new(model => model.Entity<BookArchive>()
            .Check(b => b.Price > 0, name: "CK_Price_Positive"));
        db.Schema.CreateTable<BookArchive>();

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
        using ModelTestDatabase db = new(model => model.Entity<BookArchive>()
            .Check(b => b.Price > 0, name: "CK_Price_Positive"));
        db.Schema.CreateTable<BookArchive>();

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
        using ModelTestDatabase db = new(model => model.Entity<BookArchive>()
            .Check(b => b.Price > 0));
        db.Schema.CreateTable<BookArchive>();

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
        using ModelTestDatabase db = new(model => model.Entity<BookArchive>()
            .Check(b => b.Price > 0, name: "CK_Price")
            .Check(b => b.AuthorId > 0, name: "CK_Author"));
        db.Schema.CreateTable<BookArchive>();

        Assert.ThrowsAny<Exception>(() => db.Table<BookArchive>().Add(new BookArchive
        {
            Id = 1,
            Title = "bad",
            AuthorId = 0,
            Price = 1
        }));
    }
}
