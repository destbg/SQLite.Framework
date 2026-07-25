using System;
using System.Linq;
using System.ComponentModel.DataAnnotations;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

file sealed class StringEqualsComparisonRow
{
    [Key]
    public int Id { get; set; }

    public string Name { get; set; } = "";
}

public class StringEqualsComparisonNullParityTests
{
    private static TestDatabase CreateDb()
    {
        TestDatabase db = new();
        db.Table<StringEqualsComparisonRow>().Schema.CreateTable();
        db.Table<StringEqualsComparisonRow>().Add(new StringEqualsComparisonRow { Id = 1, Name = "abc" });
        return db;
    }

    [Fact]
    public void InstanceEquals_NegatedWithNullArgument_MatchesLinqToObjects()
    {
        using TestDatabase db = CreateDb();
        string? other = null;

        List<StringEqualsComparisonRow> seed = [new StringEqualsComparisonRow { Id = 1, Name = "abc" }];
        List<int> expected = seed
            .Where(b => !b.Name.Equals(other, StringComparison.OrdinalIgnoreCase))
            .Select(b => b.Id)
            .ToList();

        List<int> actual = db.Table<StringEqualsComparisonRow>()
            .Where(b => !b.Name.Equals(other, StringComparison.OrdinalIgnoreCase))
            .Select(b => b.Id)
            .ToList();

        Assert.Equal(expected, actual);
    }
}
