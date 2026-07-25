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
    public void StaticEqualsWithComparisonFromColumnThrowsInWhere()
    {
        using TestDatabase db = Seed();

        Exception? ex = Record.Exception(() => db.Table<RuntimeComparisonRow>()
            .Where(r => string.Equals(r.Name, "alpha", r.IgnoreCaseFlag ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal))
            .Select(r => r.Id)
            .ToList());

        Assert.IsType<NotSupportedException>(ex);
    }

    [Fact]
    public void CompareWithIgnoreCaseBoolFromColumnThrowsInWhere()
    {
        using TestDatabase db = Seed();

        Exception? ex = Record.Exception(() => db.Table<RuntimeComparisonRow>()
            .Where(r => string.Compare(r.Name, "alpha", r.IgnoreCaseFlag) == 0)
            .Select(r => r.Id)
            .ToList());

        Assert.IsType<NotSupportedException>(ex);
    }

    [Fact]
    public void InstanceEqualsWithComparisonFromColumnThrowsInWhere()
    {
        using TestDatabase db = Seed();

        Exception? ex = Record.Exception(() => db.Table<RuntimeComparisonRow>()
            .Where(r => r.Name.Equals("alpha", r.IgnoreCaseFlag ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal))
            .Select(r => r.Id)
            .ToList());

        Assert.IsType<NotSupportedException>(ex);
    }

    [Fact]
    public void StaticEqualsWithComparisonFromColumnProjectsInSelect()
    {
        using TestDatabase db = Seed();

        List<bool> expected = Rows()
            .OrderBy(r => r.Id)
            .Select(r => string.Equals(r.Name, "alpha", r.IgnoreCaseFlag ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal))
            .ToList();
        Assert.Equal([true, true, false], expected);

        List<bool> actual = db.Table<RuntimeComparisonRow>()
            .OrderBy(r => r.Id)
            .Select(r => string.Equals(r.Name, "alpha", r.IgnoreCaseFlag ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal))
            .ToList();
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void InstanceEqualsWithComparisonFromColumnProjectsInSelect()
    {
        using TestDatabase db = Seed();

        List<bool> expected = Rows()
            .OrderBy(r => r.Id)
            .Select(r => r.Name.Equals("alpha", r.IgnoreCaseFlag ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal))
            .ToList();
        Assert.Equal([true, true, false], expected);

        List<bool> actual = db.Table<RuntimeComparisonRow>()
            .OrderBy(r => r.Id)
            .Select(r => r.Name.Equals("alpha", r.IgnoreCaseFlag ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal))
            .ToList();
        Assert.Equal(expected, actual);
    }
}
