using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class AliasVisitorCoverageTests
{
    [Fact]
    public void GroupBy_ComplexKey_ProjectKeyIntoAnonymous_EmitsKeyColumns()
    {
        using TestDatabase db = new();

        SQLiteCommand command = db.Table<Book>()
            .GroupBy(b => new { b.AuthorId, b.Title })
            .Select(g => new { g.Key, Count = g.Count() })
            .ToSqlCommand();

        Assert.Contains("BookAuthorId", command.CommandText);
        Assert.Contains("BookTitle", command.CommandText);
        Assert.Contains("COUNT", command.CommandText);
        Assert.DoesNotContain("\"Key.Id\"", command.CommandText);
        Assert.DoesNotContain("\"Key.Price\"", command.CommandText);
        Assert.DoesNotContain("\"Key\"", command.CommandText);
    }

    [Fact]
    public void GroupBy_ComplexKey_ProjectKeyIntoAnonymous_MaterializesEachComponent()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<Book>();

        db.Table<Book>().AddRange(new[]
        {
            new Book { Id = 1, Title = "A", AuthorId = 1, Price = 1.0 },
            new Book { Id = 2, Title = "A", AuthorId = 1, Price = 2.0 },
            new Book { Id = 3, Title = "B", AuthorId = 2, Price = 3.0 },
        });

        var rows = db.Table<Book>()
            .GroupBy(b => new { b.AuthorId, b.Title })
            .Select(g => new { g.Key, Count = g.Count() })
            .OrderBy(r => r.Key.Title)
            .ToList();

        Assert.Equal(2, rows.Count);
        Assert.Equal(1, rows[0].Key.AuthorId);
        Assert.Equal("A", rows[0].Key.Title);
        Assert.Equal(2, rows[0].Count);
        Assert.Equal(2, rows[1].Key.AuthorId);
        Assert.Equal("B", rows[1].Key.Title);
        Assert.Equal(1, rows[1].Count);
    }
}
