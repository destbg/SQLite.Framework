using System.ComponentModel.DataAnnotations;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class EqualsComputedRow
{
    [Key]
    public int Id { get; set; }

    public int Amount { get; set; }
}

public class EqualsComputedOperandClientArgumentTests
{
    private static List<EqualsComputedRow> Rows() =>
    [
        new() { Id = 1, Amount = 3 },
        new() { Id = 2, Amount = 5 },
    ];

    private static TestDatabase Seed()
    {
        TestDatabase db = new();
        db.Table<EqualsComputedRow>().Schema.CreateTable();
        db.Table<EqualsComputedRow>().AddRange(Rows());
        return db;
    }

    private static int TargetFor(int id)
    {
        return id == 1 ? 6 : 11;
    }

    [Fact]
    public void InstanceEqualsStaticHelperOnProductInSelectProjects()
    {
        using TestDatabase db = Seed();

        List<bool> expected = Rows()
            .OrderBy(r => r.Id)
            .Select(r => (r.Amount * 2).Equals(TargetFor(r.Id)))
            .ToList();
        Assert.Equal([true, false], expected);

        List<bool> actual = db.Table<EqualsComputedRow>()
            .OrderBy(r => r.Id)
            .Select(r => (r.Amount * 2).Equals(TargetFor(r.Id)))
            .ToList();
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void StaticEqualsStaticHelperOnProductInSelectProjects()
    {
        using TestDatabase db = Seed();

        List<bool> expected = Rows()
            .OrderBy(r => r.Id)
            .Select(r => Equals(r.Amount * 2, TargetFor(r.Id)))
            .ToList();
        Assert.Equal([true, false], expected);

        List<bool> actual = db.Table<EqualsComputedRow>()
            .OrderBy(r => r.Id)
            .Select(r => Equals(r.Amount * 2, TargetFor(r.Id)))
            .ToList();
        Assert.Equal(expected, actual);
    }
}
