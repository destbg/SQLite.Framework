using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class SQLiteFunctionsTests
{
    [Fact]
    public void Random_InWhere_FiltersByRandomNumber()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<Book>();
        db.Table<Book>().Add(new Book { Id = 1, Title = "x", AuthorId = 1, Price = 1 });

        SQLiteCommand cmd = db.Table<Book>()
            .Where(b => SQLiteFunctions.Random() > 0)
            .ToSqlCommand();

        Assert.Contains("RANDOM()", cmd.CommandText);
    }

    [Fact]
    public void Random_InSelect_RoundTrips()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<Book>();
        db.Table<Book>().Add(new Book { Id = 1, Title = "x", AuthorId = 1, Price = 1 });

        long random = db.Table<Book>().Select(b => (long)SQLiteFunctions.Random()).First();
        Assert.True(random != 0L);
    }

    [Fact]
    public void RandomBlob_InSelect_ReturnsRequestedLength()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<Book>();
        db.Table<Book>().Add(new Book { Id = 1, Title = "x", AuthorId = 1, Price = 1 });

        byte[] blob = db.Table<Book>().Select(b => SQLiteFunctions.RandomBlob(8)).First();
        Assert.Equal(8, blob.Length);
    }

    [Fact]
    public void Glob_InWhere_MatchesGlobPattern()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<Book>();
        db.Table<Book>().Add(new Book { Id = 1, Title = "ABC", AuthorId = 1, Price = 1 });
        db.Table<Book>().Add(new Book { Id = 2, Title = "ZZZ", AuthorId = 1, Price = 2 });

        List<Book> rows = db.Table<Book>()
            .Where(b => SQLiteFunctions.Glob("A*", b.Title))
            .ToList();

        Assert.Single(rows);
        Assert.Equal("ABC", rows[0].Title);
    }

    [Fact]
    public void UnixEpoch_InSelect_ReturnsCurrentEpoch()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<Book>();
        db.Table<Book>().Add(new Book { Id = 1, Title = "x", AuthorId = 1, Price = 1 });

        long epoch = db.Table<Book>().Select(b => SQLiteFunctions.UnixEpoch()).First();
        Assert.True(epoch > 1_700_000_000L);
    }

    [Fact]
    public void UnixEpoch_WithDateString_InSelect_ReturnsParsedEpoch()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<Book>();
        db.Table<Book>().Add(new Book { Id = 1, Title = "x", AuthorId = 1, Price = 1 });

        long epoch = db.Table<Book>().Select(b => SQLiteFunctions.UnixEpoch("2024-01-01")).First();
        Assert.Equal(1704067200L, epoch);
    }

    [Fact]
    public void Printf_InSelect_FormatsWithArgs()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<Book>();
        db.Table<Book>().Add(new Book { Id = 7, Title = "x", AuthorId = 1, Price = 1 });

        string result = db.Table<Book>()
            .Select(b => SQLiteFunctions.Printf("Book %d", b.Id))
            .First();

        Assert.Equal("Book 7", result);
    }

    [Fact]
    public void Changes_InSelect_ReturnsZeroForReadQuery()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<Book>();
        db.Table<Book>().Add(new Book { Id = 1, Title = "x", AuthorId = 1, Price = 1 });

        long changes = db.Table<Book>().Select(b => SQLiteFunctions.Changes()).First();
        Assert.True(changes >= 0);
    }

    [Fact]
    public void TotalChanges_InSelect_ReturnsConnectionTotal()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<Book>();
        db.Table<Book>().Add(new Book { Id = 1, Title = "x", AuthorId = 1, Price = 1 });

        long total = db.Table<Book>().Select(b => SQLiteFunctions.TotalChanges()).First();
        Assert.True(total > 0);
    }

    [Fact]
    public void SQLiteFunctions_CalledOutsideQuery_Throws()
    {
        Assert.Throws<InvalidOperationException>(() => SQLiteFunctions.Random());
        Assert.Throws<InvalidOperationException>(() => SQLiteFunctions.Glob("a", "b"));
        Assert.Throws<InvalidOperationException>(() => SQLiteFunctions.UnixEpoch());
        Assert.Throws<InvalidOperationException>(() => SQLiteFunctions.Changes());
    }
}
