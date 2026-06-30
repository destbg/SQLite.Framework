using System.ComponentModel.DataAnnotations;
using SQLite.Framework.Enums;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

internal enum EnumCastTier
{
    Bronze = 1,
    Silver = 2,
    Gold = 3,
}

internal sealed class EnumCastProjectionRow
{
    [Key]
    public int Id { get; set; }

    public EnumCastTier Tier { get; set; }
}

public class EnumCastUnderlyingProjectionTextStorageTests
{
    private static readonly EnumCastProjectionRow[] Data =
    [
        new EnumCastProjectionRow { Id = 1, Tier = EnumCastTier.Silver },
        new EnumCastProjectionRow { Id = 2, Tier = EnumCastTier.Gold },
    ];

    [Fact]
    public void CastEnumToUnderlyingInProjection()
    {
        using TestDatabase db = new(b => b.EnumStorage = EnumStorageMode.Text);
        db.Table<EnumCastProjectionRow>().Schema.CreateTable();
        foreach (EnumCastProjectionRow r in Data)
        {
            db.Table<EnumCastProjectionRow>().Add(r);
        }

        List<int> expected = Data.OrderBy(r => r.Id).Select(r => (int)r.Tier).ToList();
        List<int> actual = db.Table<EnumCastProjectionRow>().OrderBy(r => r.Id).Select(r => (int)r.Tier).ToList();

        Assert.Equal([2, 3], expected);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void CastEnumToUnderlyingInProjectionIntegerStorage()
    {
        using TestDatabase db = new();
        db.Table<EnumCastProjectionRow>().Schema.CreateTable();
        foreach (EnumCastProjectionRow r in Data)
        {
            db.Table<EnumCastProjectionRow>().Add(r);
        }

        List<int> expected = Data.OrderBy(r => r.Id).Select(r => (int)r.Tier).ToList();
        List<int> actual = db.Table<EnumCastProjectionRow>().OrderBy(r => r.Id).Select(r => (int)r.Tier).ToList();

        Assert.Equal([2, 3], expected);
        Assert.Equal(expected, actual);
    }
}
