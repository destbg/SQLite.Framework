using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class UpdateFromTests
{
    private static string N(string s) => s.Replace("\r\n", "\n");

    [Fact]
    public void UpdateFrom_SingleJoin_CopiesValueFromJoinedTable()
    {
        using TestDatabase db = new();
        db.Table<Author>().Schema.CreateTable();
        db.Table<Book>().Schema.CreateTable();

        db.Table<Author>().Add(new Author { Id = 1, Name = "Asimov", Email = "a@b.c", BirthDate = DateTime.UnixEpoch });
        db.Table<Book>().Add(new Book { Id = 1, Title = "Old", AuthorId = 1, Price = 1 });

        int affected = db.Table<Book>()
            .Join(db.Table<Author>(), b => b.AuthorId, a => a.Id, (b, a) => new { b, a })
            .ExecuteUpdate(s => s.Set(x => x.b.Title, x => x.a.Name));

        Assert.Equal(1, affected);
        Book row = db.Table<Book>().First();
        Assert.Equal("Asimov", row.Title);
    }

    [Fact]
    public void UpdateFrom_WithWhere_OnlyUpdatesMatchingRows()
    {
        using TestDatabase db = new();
        db.Table<Author>().Schema.CreateTable();
        db.Table<Book>().Schema.CreateTable();

        db.Table<Author>().Add(new Author { Id = 1, Name = "Alpha", Email = "a@b.c", BirthDate = DateTime.UnixEpoch });
        db.Table<Author>().Add(new Author { Id = 2, Name = "Beta", Email = "a@b.c", BirthDate = DateTime.UnixEpoch });
        db.Table<Book>().Add(new Book { Id = 1, Title = "First", AuthorId = 1, Price = 1 });
        db.Table<Book>().Add(new Book { Id = 2, Title = "Second", AuthorId = 2, Price = 2 });

        int affected = db.Table<Book>()
            .Join(db.Table<Author>(), b => b.AuthorId, a => a.Id, (b, a) => new { b, a })
            .Where(x => x.a.Name == "Beta")
            .ExecuteUpdate(s => s.Set(x => x.b.Title, "Marked"));

        Assert.Equal(1, affected);
        Assert.Equal("First", db.Table<Book>().First(b => b.Id == 1).Title);
        Assert.Equal("Marked", db.Table<Book>().First(b => b.Id == 2).Title);
    }

    [Fact]
    public void UpdateFrom_GeneratesUpdateFromSql()
    {
        using TestDatabase db = new();
        db.Table<Author>().Schema.CreateTable();
        db.Table<Book>().Schema.CreateTable();

        SQLiteCommand cmd = db.Table<Book>()
            .Join(db.Table<Author>(), b => b.AuthorId, a => a.Id, (b, a) => new { b, a })
            .Where(x => x.a.Name == "X")
            .ToSqlCommand();

        Assert.Contains("JOIN", cmd.CommandText);
    }

    [Fact]
    public void UpdateFrom_SetLvalueOnSource_Throws()
    {
        using TestDatabase db = new();
        db.Table<Author>().Schema.CreateTable();
        db.Table<Book>().Schema.CreateTable();

        Assert.Throws<ArgumentException>(() =>
            db.Table<Book>()
                .Join(db.Table<Author>(), b => b.AuthorId, a => a.Id, (b, a) => new { b, a })
                .ExecuteUpdate(s => s.Set(x => x.a.Name, "won't work")));
    }

    [Fact]
    public void UpdateFrom_RvalueArithmetic_RoundTrips()
    {
        using TestDatabase db = new();
        db.Table<Author>().Schema.CreateTable();
        db.Table<Book>().Schema.CreateTable();

        db.Table<Author>().Add(new Author { Id = 1, Name = "A", Email = "a@b.c", BirthDate = DateTime.UnixEpoch });
        db.Table<Book>().Add(new Book { Id = 1, Title = "T", AuthorId = 1, Price = 100 });

        db.Table<Book>()
            .Join(db.Table<Author>(), b => b.AuthorId, a => a.Id, (b, a) => new { b, a })
            .ExecuteUpdate(s => s.Set(x => x.b.Price, x => x.b.Price + x.a.Id));

        Assert.Equal(101, db.Table<Book>().First().Price);
    }

    [Fact]
    public void UpdateFrom_TwoJoins_EmitsCommaSeparatedFromList()
    {
        using TestDatabase db = new();
        db.Table<Author>().Schema.CreateTable();
        db.Table<Book>().Schema.CreateTable();

        db.Table<Author>().Add(new Author { Id = 1, Name = "First", Email = "a@b.c", BirthDate = DateTime.UnixEpoch });
        db.Table<Author>().Add(new Author { Id = 2, Name = "Second", Email = "a@b.c", BirthDate = DateTime.UnixEpoch });
        db.Table<Book>().Add(new Book { Id = 1, Title = "Old", AuthorId = 1, Price = 1 });

        int affected = db.Table<Book>()
            .Join(db.Table<Author>(), b => b.AuthorId, a => a.Id, (b, a) => new { b, a })
            .Join(db.Table<Author>(), x => x.a.Id + 1, a2 => a2.Id, (x, a2) => new { x.b, x.a, a2 })
            .ExecuteUpdate(s => s.Set(x => x.b.Title, x => x.a.Name + " - " + x.a2.Name));

        Assert.Equal(1, affected);
        Assert.Equal("First - Second", db.Table<Book>().First().Title);
    }

    [Fact]
    public void UpdateFrom_NoWhereClause_UpdatesAllJoinedRows()
    {
        using TestDatabase db = new();
        db.Table<Author>().Schema.CreateTable();
        db.Table<Book>().Schema.CreateTable();

        db.Table<Author>().Add(new Author { Id = 1, Name = "A", Email = "a@b.c", BirthDate = DateTime.UnixEpoch });
        db.Table<Book>().Add(new Book { Id = 1, Title = "T1", AuthorId = 1, Price = 1 });
        db.Table<Book>().Add(new Book { Id = 2, Title = "T2", AuthorId = 1, Price = 2 });
        db.Table<Book>().Add(new Book { Id = 3, Title = "Orphan", AuthorId = 99, Price = 3 });

        int affected = db.Table<Book>()
            .Join(db.Table<Author>(), b => b.AuthorId, a => a.Id, (b, a) => new { b, a })
            .ExecuteUpdate(s => s.Set(x => x.b.Title, "Updated"));

        Assert.Equal(2, affected);
        Assert.Equal("Updated", db.Table<Book>().First(b => b.Id == 1).Title);
        Assert.Equal("Updated", db.Table<Book>().First(b => b.Id == 2).Title);
        Assert.Equal("Orphan", db.Table<Book>().First(b => b.Id == 3).Title);
    }
}
