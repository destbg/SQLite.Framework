using System.Collections.Generic;
using System.Linq;
using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class PrivateNestedTypeInMemoryLinqTests
{
    private sealed record PrivateRow(int Bucket, double Value);

    protected sealed record ProtectedRow(int Bucket, double Value);

    private static TestDatabase WithBooks(params (int Id, string Title, int AuthorId, double Price)[] rows)
    {
        TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        foreach ((int id, string title, int authorId, double price) in rows)
            db.Table<Book>().Add(new Book { Id = id, Title = title, AuthorId = authorId, Price = price });
        return db;
    }

    [Fact]
    public void InMemoryGroupBy_OverPrivateElementType()
    {
        using TestDatabase db = WithBooks((1, "a", 1, 5), (2, "b", 1, 20), (3, "c", 2, 30), (4, "d", 2, 1));
        List<PrivateRow> rows = db.Table<Book>().OrderBy(b => b.Id).ToList()
            .Select(b => new PrivateRow(b.AuthorId, b.Price))
            .ToList();

        List<double> actual = rows
            .GroupBy(r => r.Bucket)
            .OrderBy(g => g.Key)
            .Select(g => g.Sum(x => x.Value))
            .ToList();

        List<double> expected = new List<(int Bucket, double Value)> { (1, 5), (1, 20), (2, 30), (2, 1) }
            .GroupBy(r => r.Bucket)
            .OrderBy(g => g.Key)
            .Select(g => g.Sum(x => x.Value))
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void InMemoryGroupBy_OverProtectedElementType()
    {
        using TestDatabase db = WithBooks((1, "a", 1, 5), (2, "b", 1, 20), (3, "c", 2, 30));
        List<ProtectedRow> rows = db.Table<Book>().OrderBy(b => b.Id).ToList()
            .Select(b => new ProtectedRow(b.AuthorId, b.Price))
            .ToList();

        List<int> actual = rows
            .GroupBy(r => r.Bucket)
            .OrderBy(g => g.Key)
            .Select(g => g.Count())
            .ToList();

        List<int> expected = new List<(int Bucket, double Value)> { (1, 5), (1, 20), (2, 30) }
            .GroupBy(r => r.Bucket)
            .OrderBy(g => g.Key)
            .Select(g => g.Count())
            .ToList();

        Assert.Equal(expected, actual);
    }
}
