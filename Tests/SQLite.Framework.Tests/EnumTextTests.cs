using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Enums;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class EnumTextTests
{
    private static TestDatabase SetupDatabase()
    {
        TestDatabase db = new();
        db.Schema.CreateTable<Publisher>();
        db.Execute("INSERT INTO Publisher (Id, Name, Type) VALUES (1, 'Publisher 1', @type)",
            new SQLiteParameter
            {
                Name = "@type",
                Value = "Book"
            });
        db.Execute("INSERT INTO Publisher (Id, Name, Type) VALUES (2, 'Publisher 2', @type)",
            new SQLiteParameter
            {
                Name = "@type",
                Value = "Magazine"
            });
        db.Execute("INSERT INTO Publisher (Id, Name, Type) VALUES (3, 'Publisher 3', @type)",
            new SQLiteParameter
            {
                Name = "@type",
                Value = "Newspaper"
            });
        return db;
    }

    [Fact]
    public void Read_WhenStoredAsTextName_ReturnsCorrectEnumValues()
    {
        using TestDatabase db = SetupDatabase();

        List<Publisher> results = db.Table<Publisher>().OrderBy(p => p.Id).ToList();

        Assert.Equal(3, results.Count);
        Assert.Equal(PublisherType.Book, results[0].Type);
        Assert.Equal(PublisherType.Magazine, results[1].Type);
        Assert.Equal(PublisherType.Newspaper, results[2].Type);
    }

    [Fact]
    public void Read_WhenStoredAsTextName_ViaQuery_ReturnsCorrectEnumValues()
    {
        using TestDatabase db = SetupDatabase();

        List<Publisher> results = db.Query<Publisher>("SELECT * FROM Publisher ORDER BY Id");

        Assert.Equal(3, results.Count);
        Assert.Equal(PublisherType.Book, results[0].Type);
        Assert.Equal(PublisherType.Magazine, results[1].Type);
        Assert.Equal(PublisherType.Newspaper, results[2].Type);
    }

    [Fact]
    public void Read_WhenStoredAsTextName_UnknownValue_ReturnsDefault()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<Publisher>();
        db.Execute("INSERT INTO Publisher (Id, Name, Type) VALUES (1, 'Publisher 1', @type)",
            new SQLiteParameter
            {
                Name = "@type",
                Value = "UnknownType"
            });

        List<Publisher> results = db.Table<Publisher>().ToList();

        Assert.Single(results);
        Assert.Equal(default, results[0].Type);
    }
}