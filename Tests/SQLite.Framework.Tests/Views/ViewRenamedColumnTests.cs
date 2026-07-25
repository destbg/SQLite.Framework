using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework;
using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("vRenamedBook")]
public class RenamedBookView
{
    [Column("ViewId")]
    public int Id { get; set; }

    [Column("ViewTitle")]
    public string Title { get; set; } = "";
}

[Table("vPartRenamedBook")]
public class PartRenamedBookView
{
    [Column("ViewId")]
    public int Id { get; set; }

    public string Title { get; set; } = "";
}

[Table("vExtraBook")]
public class ExtraBookView
{
    [Column("ViewId")]
    public int Id { get; set; }

    [NotMapped]
    public string Extra { get; set; } = "";
}

public class ViewRenamedColumnTests
{
    [Fact]
    public void CreateViewWithRenamedColumnsReadsBack()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().Add(new Book { Id = 1, Title = "T", AuthorId = 1, Price = 5 });

        db.Schema.CreateView<RenamedBookView>(() =>
            from b in db.Table<Book>()
            select new RenamedBookView { Id = b.Id, Title = b.Title });

        List<RenamedBookView> rows = db.ReadOnlyTable<RenamedBookView>().ToList();
        Assert.Equal("T", Assert.Single(rows).Title);
    }

    [Fact]
    public void CreateViewWithOneRenamedColumnReadsBack()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().Add(new Book { Id = 1, Title = "T", AuthorId = 1, Price = 5 });

        db.Schema.CreateView<PartRenamedBookView>(() =>
            from b in db.Table<Book>()
            select new PartRenamedBookView { Id = b.Id, Title = b.Title });

        List<PartRenamedBookView> rows = db.ReadOnlyTable<PartRenamedBookView>().ToList();
        Assert.Equal("T", Assert.Single(rows).Title);
    }

    [Fact]
    public void CreateViewWithNotMappedMemberReadsBack()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().Add(new Book { Id = 1, Title = "T", AuthorId = 1, Price = 5 });

        db.Schema.CreateView<ExtraBookView>(() =>
            from b in db.Table<Book>()
            select new ExtraBookView { Id = b.Id, Extra = b.Title });

        List<ExtraBookView> rows = db.ReadOnlyTable<ExtraBookView>().ToList();
        Assert.Equal(1, Assert.Single(rows).Id);
    }
}
