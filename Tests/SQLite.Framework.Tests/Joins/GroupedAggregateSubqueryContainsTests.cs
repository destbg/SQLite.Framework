using System.ComponentModel.DataAnnotations;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

internal sealed class GroupedAggregateBook
{
    [Key]
    public int Id { get; set; }

    public int AuthorId { get; set; }

    public int Price { get; set; }
}

public class GroupedAggregateSubqueryContainsTests
{
    private static readonly GroupedAggregateBook[] Data =
    [
        new GroupedAggregateBook { Id = 1, AuthorId = 1, Price = 10 },
        new GroupedAggregateBook { Id = 2, AuthorId = 1, Price = 20 },
        new GroupedAggregateBook { Id = 3, AuthorId = 2, Price = 5 },
    ];

    [Fact]
    public void ContainsOverGroupedSumProjection()
    {
        using TestDatabase db = new();
        db.Table<GroupedAggregateBook>().Schema.CreateTable();
        foreach (GroupedAggregateBook b in Data)
        {
            db.Table<GroupedAggregateBook>().Add(b);
        }

        bool expected = Data.GroupBy(b => b.AuthorId).Select(g => g.Sum(x => x.Price)).Contains(30);
        bool actual = db.Table<GroupedAggregateBook>().GroupBy(b => b.AuthorId).Select(g => g.Sum(x => x.Price)).Contains(30);

        Assert.True(expected);
        Assert.Equal(expected, actual);
    }
}
