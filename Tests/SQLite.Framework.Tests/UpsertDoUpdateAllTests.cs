using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Attributes;
using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

file sealed class SwappedNameUpsertEntity
{
    [Key]
    public int Id { get; set; }

    [Column("Code")]
    [Indexed(IsUnique = true)]
    public string Status { get; set; } = "";

    [Column("Status")]
    public string Code { get; set; } = "";
}

public class UpsertDoUpdateAllTests
{
    [Fact]
    public void DoUpdateAllDoesNotRewritePrimaryKeyOnNonKeyConflict()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().Add(new Book { Id = 10, Title = "A", AuthorId = 1, Price = 5.0 });

        db.Table<Book>().Upsert(
            new Book { Id = 20, Title = "B", AuthorId = 2, Price = 5.0 },
            c => c.OnConflict(b => b.Price).DoUpdateAll());

        Book row = db.Table<Book>().Single();
        Assert.Equal("B", row.Title);
        Assert.Equal(10, row.Id);
    }

    [Fact]
    public void DoUpdateAllWithSwappedColumnNamesLeavesRowUnchanged()
    {
        using TestDatabase db = new();
        db.Table<SwappedNameUpsertEntity>().Schema.CreateTable();
        db.Table<SwappedNameUpsertEntity>().Add(new SwappedNameUpsertEntity { Id = 1, Status = "a", Code = "x" });

        int changes = db.Table<SwappedNameUpsertEntity>().Upsert(
            new SwappedNameUpsertEntity { Id = 2, Status = "a", Code = "y" },
            c => c.OnConflict(b => b.Status).DoUpdateAll());

        SwappedNameUpsertEntity row = db.Table<SwappedNameUpsertEntity>().Single();
        Assert.Equal(0, changes);
        Assert.Equal(1, row.Id);
        Assert.Equal("a", row.Status);
        Assert.Equal("x", row.Code);
    }
}
