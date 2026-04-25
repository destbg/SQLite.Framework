using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Enums;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class EnumTests
{
    [Fact]
    public void EnumHasFlag()
    {
        using TestDatabase db = new();

        db.Table<Publisher>().CreateTable();
        db.Table<Publisher>().AddRange(new[]
        {
            new Publisher { Id = 1, Name = "Publisher 1", Type = PublisherType.Book },
            new Publisher { Id = 2, Name = "Publisher 2", Type = PublisherType.Magazine },
            new Publisher { Id = 3, Name = "Publisher 3", Type = PublisherType.Newspaper }
        });

        var query = db.Table<Publisher>().Select(p => new { p.Id, HasMagazineFlag = p.Type.HasFlag(PublisherType.Magazine) });

        SQLiteCommand command = query.ToSqlCommand();

        Assert.Single(command.Parameters);
        Assert.Equal(PublisherType.Magazine, command.Parameters[0].Value);
        Assert.Equal("""
                     SELECT p0.Id AS "Id",
                            ((p0.Type & @p0) = @p0) AS "HasMagazineFlag"
                     FROM "Publisher" AS p0
                     """.Replace("\r\n", "\n"),
            command.CommandText.Replace("\r\n", "\n"));

        var results = query.ToList();
        Assert.Equal(3, results.Count);
        Assert.False(results[0].HasMagazineFlag);
        Assert.True(results[1].HasMagazineFlag);
        Assert.True(results[2].HasMagazineFlag);
    }

    [Fact]
    public void EnumToString()
    {
        using TestDatabase db = new();

        db.Table<Publisher>().CreateTable();
        db.Table<Publisher>().AddRange(new[]
        {
            new Publisher { Id = 1, Name = "Publisher 1", Type = PublisherType.Book },
            new Publisher { Id = 2, Name = "Publisher 2", Type = PublisherType.Magazine },
            new Publisher { Id = 3, Name = "Publisher 3", Type = PublisherType.Newspaper }
        });

        var query = db.Table<Publisher>().Select(p => new { p.Id, TypeString = p.Type.ToString() });

        var results = query.ToList();
        Assert.Equal(3, results.Count);
        Assert.Equal("Book", results[0].TypeString);
        Assert.Equal("Magazine", results[1].TypeString);
        Assert.Equal("Newspaper", results[2].TypeString);
    }

    [Fact]
    public void EnumParseInWhere()
    {
        using TestDatabase db = new();

        db.Table<Publisher>().CreateTable();
        db.Table<Publisher>().AddRange(new[]
        {
            new Publisher { Id = 1, Name = "Book", Type = PublisherType.Book },
            new Publisher { Id = 2, Name = "Magazine", Type = PublisherType.Magazine },
            new Publisher { Id = 3, Name = "Newspaper", Type = PublisherType.Newspaper }
        });

        IQueryable<Publisher> query =
            from p in db.Table<Publisher>()
            where p.Type == Enum.Parse<PublisherType>(p.Name)
            select p;

        List<Publisher> results = query.ToList();
        Assert.Equal(3, results.Count);
    }

    [Fact]
    public void EnumParseInWhereUsingType()
    {
        using TestDatabase db = new();

        db.Table<Publisher>().CreateTable();
        db.Table<Publisher>().AddRange(new[]
        {
            new Publisher { Id = 1, Name = "Book", Type = PublisherType.Book },
            new Publisher { Id = 2, Name = "Magazine", Type = PublisherType.Magazine },
            new Publisher { Id = 3, Name = "Newspaper", Type = PublisherType.Newspaper }
        });

        IQueryable<Publisher> query =
            from p in db.Table<Publisher>()
            where p.Type == (PublisherType)Enum.Parse(typeof(PublisherType), p.Name)
            select p;

        List<Publisher> results = query.ToList();
        Assert.Equal(3, results.Count);
    }
}
