using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class StringOrderingSemanticsTests
{
    private static readonly (int id, string title)[] Rows =
    {
        (1, "B"),
        (2, "a"),
        (3, "C"),
        (4, "b"),
    };

    private static TestDatabase Seed()
    {
        TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        int price = 1;
        foreach ((int id, string title) in Rows)
        {
            db.Table<Book>().Add(new Book { Id = id, Title = title, AuthorId = 1, Price = price++ });
        }

        return db;
    }

    [Fact]
    public void OrderByStringMatchesOrdinalComparer()
    {
        using TestDatabase db = Seed();

        List<int> actual = db.Table<Book>().OrderBy(x => x.Title).Select(x => x.Id).ToList();
        List<int> oracle = Rows.OrderBy(r => r.title, StringComparer.Ordinal).Select(r => r.id).ToList();

        Assert.Equal(oracle, actual);
    }

    [Fact]
    public void CompareToMatchesOrdinalComparison()
    {
        using TestDatabase db = Seed();

        int actual = db.Table<Book>().Where(x => x.Id == 2).Select(x => x.Title.CompareTo("B")).First();
        int oracle = string.CompareOrdinal("a", "B");

        Assert.Equal(Math.Sign(oracle), Math.Sign(actual));
    }
}
