using System.ComponentModel.DataAnnotations;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

internal sealed class OrderedJoinSourceRow
{
    [Key]
    public int Id { get; set; }

    public string Name { get; set; } = "";

    public int Value { get; set; }
}

public class JoinOrderedSubquerySourceTests
{
    private static TestDatabase SetupDatabase()
    {
        TestDatabase db = new();
        db.Table<OrderedJoinSourceRow>().Schema.CreateTable();
        db.Table<OrderedJoinSourceRow>().Add(new OrderedJoinSourceRow { Id = 1, Name = "a", Value = 10 });
        db.Table<OrderedJoinSourceRow>().Add(new OrderedJoinSourceRow { Id = 2, Name = "a", Value = 20 });
        db.Table<OrderedJoinSourceRow>().Add(new OrderedJoinSourceRow { Id = 3, Name = "b", Value = 5 });
        return db;
    }

    [Fact]
    public void JoinToOrderedInnerSourceTranslates()
    {
        using TestDatabase db = SetupDatabase();

        List<string> expected = db.Table<OrderedJoinSourceRow>().AsEnumerable()
            .Join(
                db.Table<OrderedJoinSourceRow>().AsEnumerable().OrderBy(x => x.Value),
                a => a.Id,
                b => b.Id,
                (a, b) => b.Name)
            .ToList();

        Assert.Equal(["a", "a", "b"], expected);

        List<string> actual = db.Table<OrderedJoinSourceRow>()
            .Join(
                db.Table<OrderedJoinSourceRow>().OrderBy(x => x.Value),
                a => a.Id,
                b => b.Id,
                (a, b) => b.Name)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void SelectManyTwoArgumentOverloadTranslates()
    {
        using TestDatabase db = SetupDatabase();

        List<string> expected = db.Table<OrderedJoinSourceRow>().AsEnumerable()
            .SelectMany(_ => db.Table<OrderedJoinSourceRow>().AsEnumerable().Select(b => b.Name))
            .ToList();

        Assert.Equal(9, expected.Count);

        List<string> actual = db.Table<OrderedJoinSourceRow>()
            .SelectMany(_ => db.Table<OrderedJoinSourceRow>().Select(b => b.Name))
            .ToList();

        Assert.Equal(expected, actual);
    }
}
