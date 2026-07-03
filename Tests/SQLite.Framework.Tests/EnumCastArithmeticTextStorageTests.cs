using System.ComponentModel.DataAnnotations;
using SQLite.Framework.Enums;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

internal enum ServiceTier
{
    Bronze = 1,
    Silver = 2,
    Gold = 3,
}

internal sealed class ServiceTierRow
{
    [Key]
    public int Id { get; set; }

    public ServiceTier Tier { get; set; }

    public ServiceTier Fallback { get; set; }
}

public class EnumCastArithmeticTextStorageTests
{
    private static List<ServiceTierRow> Rows() =>
    [
        new() { Id = 1, Tier = ServiceTier.Gold, Fallback = ServiceTier.Bronze },
        new() { Id = 2, Tier = ServiceTier.Silver, Fallback = ServiceTier.Silver },
        new() { Id = 3, Tier = ServiceTier.Bronze, Fallback = ServiceTier.Gold },
    ];

    private static TestDatabase Seed()
    {
        TestDatabase db = new(b => b.UseEnumStorage(EnumStorageMode.Text));
        db.Table<ServiceTierRow>().Schema.CreateTable();
        db.Table<ServiceTierRow>().AddRange(Rows());
        return db;
    }

    [Fact]
    public void CastTimesConstantProjection()
    {
        using TestDatabase db = Seed();

        List<int> expected = Rows().OrderBy(r => r.Id).Select(r => (int)r.Tier * 2).ToList();
        Assert.Equal([6, 4, 2], expected);

        List<int> actual = db.Table<ServiceTierRow>().OrderBy(r => r.Id).Select(r => (int)r.Tier * 2).ToList();
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void CastPlusConstantComparison()
    {
        using TestDatabase db = Seed();

        List<int> expected = Rows().Where(r => (int)r.Tier + 1 == 3).Select(r => r.Id).OrderBy(id => id).ToList();
        Assert.Equal([2], expected);

        List<int> actual = db.Table<ServiceTierRow>()
            .Where(r => (int)r.Tier + 1 == 3).Select(r => r.Id).OrderBy(id => id).ToList();
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void CastComparedToColumn()
    {
        using TestDatabase db = Seed();

        List<int> expected = Rows().Where(r => (int)r.Tier == r.Id).Select(r => r.Id).OrderBy(id => id).ToList();
        Assert.Equal([2], expected);

        List<int> actual = db.Table<ServiceTierRow>()
            .Where(r => (int)r.Tier == r.Id).Select(r => r.Id).OrderBy(id => id).ToList();
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void CastRelationalToColumn()
    {
        using TestDatabase db = Seed();

        List<int> expected = Rows().Where(r => (int)r.Tier < r.Id).Select(r => r.Id).OrderBy(id => id).ToList();
        Assert.Equal([3], expected);

        List<int> actual = db.Table<ServiceTierRow>()
            .Where(r => (int)r.Tier < r.Id).Select(r => r.Id).OrderBy(id => id).ToList();
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void CastEqualsUnderlyingConstant()
    {
        using TestDatabase db = Seed();

        List<int> expected = Rows().Where(r => (int)r.Tier == 2).Select(r => r.Id).OrderBy(id => id).ToList();
        Assert.Equal([2], expected);

        List<int> actual = db.Table<ServiceTierRow>()
            .Where(r => (int)r.Tier == 2).Select(r => r.Id).OrderBy(id => id).ToList();
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void CastEqualsCastOfOtherEnumColumn()
    {
        using TestDatabase db = Seed();

        List<int> expected = Rows().Where(r => (int)r.Tier == (int)r.Fallback).Select(r => r.Id).OrderBy(id => id).ToList();
        Assert.Equal([2], expected);

        List<int> actual = db.Table<ServiceTierRow>()
            .Where(r => (int)r.Tier == (int)r.Fallback).Select(r => r.Id).OrderBy(id => id).ToList();
        Assert.Equal(expected, actual);
    }
}
