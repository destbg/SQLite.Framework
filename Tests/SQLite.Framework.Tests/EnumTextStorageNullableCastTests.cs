using System.ComponentModel.DataAnnotations;
using SQLite.Framework.Enums;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

internal sealed class NullableEnumCastRow
{
    [Key]
    public int Id { get; set; }

    public EnumCastTier Tier { get; set; }

    public EnumCastTier? OptionalTier { get; set; }
}

public class EnumTextStorageNullableCastTests
{
    private static readonly NullableEnumCastRow[] Data =
    [
        new NullableEnumCastRow { Id = 1, Tier = EnumCastTier.Silver, OptionalTier = EnumCastTier.Silver },
        new NullableEnumCastRow { Id = 2, Tier = EnumCastTier.Gold, OptionalTier = null },
    ];

    private static TestDatabase NewDb()
    {
        TestDatabase db = new(b => b.EnumStorage = EnumStorageMode.Text);
        db.Table<NullableEnumCastRow>().Schema.CreateTable();
        foreach (NullableEnumCastRow r in Data)
        {
            db.Table<NullableEnumCastRow>().Add(r);
        }

        return db;
    }

    [Fact]
    public void LiteralComparedToRightSideEnumWideningCastMatchesDotNet()
    {
        using TestDatabase db = NewDb();

        List<int> expected = Data.Where(r => 2L == (long)r.Tier).Select(r => r.Id).OrderBy(id => id).ToList();
        List<int> actual = db.Table<NullableEnumCastRow>()
            .Where(x => 2L == (long)x.Tier)
            .Select(x => x.Id)
            .OrderBy(id => id)
            .ToList();

        Assert.Equal([1], expected);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void NullableEnumColumnComparedToEnumLiteralMatchesDotNet()
    {
        using TestDatabase db = NewDb();

        List<int> expected = Data.Where(r => r.OptionalTier == EnumCastTier.Silver).Select(r => r.Id).OrderBy(id => id).ToList();
        List<int> actual = db.Table<NullableEnumCastRow>()
            .Where(x => x.OptionalTier == EnumCastTier.Silver)
            .Select(x => x.Id)
            .OrderBy(id => id)
            .ToList();

        Assert.Equal([1], expected);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void EnumLiteralComparedToRightSideNullableEnumColumnMatchesDotNet()
    {
        using TestDatabase db = NewDb();

        List<int> expected = Data.Where(r => EnumCastTier.Silver == r.OptionalTier).Select(r => r.Id).OrderBy(id => id).ToList();
        List<int> actual = db.Table<NullableEnumCastRow>()
            .Where(x => EnumCastTier.Silver == x.OptionalTier)
            .Select(x => x.Id)
            .OrderBy(id => id)
            .ToList();

        Assert.Equal([1], expected);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void NullableEnumColumnWidenedToNullableLongMatchesDotNet()
    {
        using TestDatabase db = NewDb();

        List<int> expected = Data.Where(r => 2L == (long?)r.OptionalTier).Select(r => r.Id).OrderBy(id => id).ToList();
        List<int> actual = db.Table<NullableEnumCastRow>()
            .Where(x => 2L == (long?)x.OptionalTier)
            .Select(x => x.Id)
            .OrderBy(id => id)
            .ToList();

        Assert.Equal([1], expected);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void CastEnumToNullableUnderlyingInProjectionMatchesDotNet()
    {
        using TestDatabase db = NewDb();

        List<int?> expected = Data.OrderBy(r => r.Id).Select(r => (int?)r.Tier).ToList();
        List<int?> actual = db.Table<NullableEnumCastRow>()
            .OrderBy(r => r.Id)
            .Select(r => (int?)r.Tier)
            .ToList();

        Assert.Equal(expected, actual);
    }
}
