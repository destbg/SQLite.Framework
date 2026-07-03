using System.ComponentModel.DataAnnotations;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class RuntimeComparisonRow
{
    [Key]
    public int Id { get; set; }

    public string Name { get; set; } = "";

    public bool IgnoreCaseFlag { get; set; }
}

public class RuntimeStringComparisonArgumentTests
{
    private static List<RuntimeComparisonRow> Rows() =>
    [
        new() { Id = 1, Name = "ALPHA", IgnoreCaseFlag = true },
        new() { Id = 2, Name = "alpha", IgnoreCaseFlag = false },
        new() { Id = 3, Name = "BETA", IgnoreCaseFlag = true },
    ];

    private static TestDatabase Seed()
    {
        TestDatabase db = new();
        db.Table<RuntimeComparisonRow>().Schema.CreateTable();
        db.Table<RuntimeComparisonRow>().AddRange(Rows());
        return db;
    }

    [Fact]
    public void StaticEqualsWithComparisonFromColumnFilters()
    {
        using TestDatabase db = Seed();

        List<int> expected = Rows()
            .Where(r => string.Equals(r.Name, "alpha", r.IgnoreCaseFlag ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal))
            .Select(r => r.Id)
            .ToList();
        Assert.Equal([1, 2], expected);

        List<int> actual = db.Table<RuntimeComparisonRow>()
            .Where(r => string.Equals(r.Name, "alpha", r.IgnoreCaseFlag ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal))
            .Select(r => r.Id)
            .ToList();
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void CompareWithIgnoreCaseBoolFromColumnFilters()
    {
        using TestDatabase db = Seed();

        List<int> expected = Rows()
            .Where(r => string.Compare(r.Name, "alpha", r.IgnoreCaseFlag) == 0)
            .Select(r => r.Id)
            .ToList();
        Assert.Equal([1, 2], expected);

        List<int> actual = db.Table<RuntimeComparisonRow>()
            .Where(r => string.Compare(r.Name, "alpha", r.IgnoreCaseFlag) == 0)
            .Select(r => r.Id)
            .ToList();
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void InstanceEqualsWithComparisonFromColumnFilters()
    {
        using TestDatabase db = Seed();

        List<int> expected = Rows()
            .Where(r => r.Name.Equals("alpha", r.IgnoreCaseFlag ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal))
            .Select(r => r.Id)
            .ToList();
        Assert.Equal([1, 2], expected);

        List<int> actual = db.Table<RuntimeComparisonRow>()
            .Where(r => r.Name.Equals("alpha", r.IgnoreCaseFlag ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal))
            .Select(r => r.Id)
            .ToList();
        Assert.Equal(expected, actual);
    }
}
