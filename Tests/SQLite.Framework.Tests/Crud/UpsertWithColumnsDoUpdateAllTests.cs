using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class UpsertWithColumnsDoUpdateAllTests
{
    [Fact]
    public void WithColumns_DoUpdateAll_ExtraColumnIsUpdatedOnConflict()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        db.Table<Book>().Add(new Book { Id = 1, Title = "original", AuthorId = 1, Price = 5 });

        db.Table<Book>()
            .WithColumns(c => c.Set(x => x.Title, "forced"))
            .Upsert(
                new Book { Id = 1, Title = "entity-title", AuthorId = 9, Price = 99 },
                c => c.OnConflict(b => b.Id).DoUpdateAll());

        Book updated = db.Table<Book>().Single();
        Assert.Equal("forced", updated.Title);
        Assert.Equal(9, updated.AuthorId);
        Assert.Equal(99, updated.Price);
    }

    [Fact]
    public void WithColumns_DoUpdateAll_WithoutConflict_ExtraColumnIsWritten()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        db.Table<Book>()
            .WithColumns(c => c.Set(x => x.Title, "forced"))
            .Upsert(
                new Book { Id = 1, Title = "entity-title", AuthorId = 1, Price = 5 },
                c => c.OnConflict(b => b.Id).DoUpdateAll());

        Book inserted = db.Table<Book>().Single();
        Assert.Equal("forced", inserted.Title);
    }

    [Fact]
    public void WithColumns_DoUpdateAll_ExtraOnConflictColumn_IsNotInSetClause()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        db.Table<Book>().Add(new Book { Id = 1, Title = "original", AuthorId = 1, Price = 5 });

        db.Table<Book>()
            .WithColumns(c => c.Set(x => x.Id, 1))
            .Upsert(
                new Book { Id = 0, Title = "updated", AuthorId = 2, Price = 6 },
                c => c.OnConflict(b => b.Id).DoUpdateAll());

        Book updated = db.Table<Book>().Single();
        Assert.Equal(1, updated.Id);
        Assert.Equal("updated", updated.Title);
        Assert.Equal(2, updated.AuthorId);
    }

    [Fact]
    public void WithColumns_DoUpdateAll_ExtraOnPrimaryKey_ConflictOnOtherColumn_KeepsExistingKey()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        db.Table<Book>().Add(new Book { Id = 1, Title = "original", AuthorId = 1, Price = 5 });

        db.Table<Book>()
            .WithColumns(c => c.Set(x => x.Id, 9))
            .Upsert(
                new Book { Id = 0, Title = "updated", AuthorId = 2, Price = 5 },
                c => c.OnConflict(b => b.Price).DoUpdateAll());

        Book updated = db.Table<Book>().Single();
        Assert.Equal(1, updated.Id);
        Assert.Equal("updated", updated.Title);
        Assert.Equal(5, updated.Price);
    }

    [Fact]
    public void WithColumns_Empty_DoUpdateAll_BehavesLikePlainTable()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        db.Table<Book>().Add(new Book { Id = 1, Title = "original", AuthorId = 1, Price = 5 });

        db.Table<Book>()
            .WithColumns(_ => { })
            .Upsert(
                new Book { Id = 1, Title = "updated", AuthorId = 2, Price = 6 },
                c => c.OnConflict(b => b.Id).DoUpdateAll());

        Book updated = db.Table<Book>().Single();
        Assert.Equal(1, updated.Id);
        Assert.Equal("updated", updated.Title);
        Assert.Equal(2, updated.AuthorId);
        Assert.Equal(6, updated.Price);
    }
}
