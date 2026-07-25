using System.ComponentModel.DataAnnotations;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

internal sealed class GroupedResultSelectorRow
{
    [Key]
    public int Id { get; set; }

    public string Name { get; set; } = "";

    public int Value { get; set; }
}

public class GroupByResultSelectorOverloadTests
{
    private static TestDatabase SetupDatabase()
    {
        TestDatabase db = new();
        db.Table<GroupedResultSelectorRow>().Schema.CreateTable();
        db.Table<GroupedResultSelectorRow>().Add(new GroupedResultSelectorRow { Id = 1, Name = "a", Value = 10 });
        db.Table<GroupedResultSelectorRow>().Add(new GroupedResultSelectorRow { Id = 2, Name = "a", Value = 20 });
        db.Table<GroupedResultSelectorRow>().Add(new GroupedResultSelectorRow { Id = 3, Name = "b", Value = 5 });
        return db;
    }

    [Fact]
    public void ResultSelectorOverloadProjectsKeyAndGroup()
    {
        using TestDatabase db = SetupDatabase();

        List<string> expected = db.Table<GroupedResultSelectorRow>().AsEnumerable()
            .GroupBy(r => r.Name, (k, g) => k + ":" + g.Count())
            .ToList();

        Assert.Equal(["a:2", "b:1"], expected);

        List<string> actual = db.Table<GroupedResultSelectorRow>()
            .GroupBy(r => r.Name, (k, g) => k + ":" + g.Count())
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ElementAndResultSelectorOverloadProjectsKeyAndGroup()
    {
        using TestDatabase db = SetupDatabase();

        List<string> expected = db.Table<GroupedResultSelectorRow>().AsEnumerable()
            .GroupBy(r => r.Name, r => r.Value, (k, g) => k + ":" + g.Count())
            .ToList();

        Assert.Equal(["a:2", "b:1"], expected);

        List<string> actual = db.Table<GroupedResultSelectorRow>()
            .GroupBy(r => r.Name, r => r.Value, (k, g) => k + ":" + g.Count())
            .ToList();

        Assert.Equal(expected, actual);
    }
}
