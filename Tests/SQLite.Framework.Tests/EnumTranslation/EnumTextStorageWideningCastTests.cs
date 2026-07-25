using SQLite.Framework.Enums;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class EnumTextStorageWideningCastTests
{
    [Fact]
    public void CastTextEnumToLongMatchesDotNet()
    {
        using TestDatabase db = new(b => b.EnumStorage = EnumStorageMode.Text);
        db.Table<EnumCastProjectionRow>().Schema.CreateTable();
        db.Table<EnumCastProjectionRow>().Add(new EnumCastProjectionRow { Id = 1, Tier = EnumCastTier.Silver });

        long expected = (long)EnumCastTier.Silver;
        Assert.Equal(2L, expected);

        long actual = db.Table<EnumCastProjectionRow>().Select(x => (long)x.Tier).Single();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void CastTextEnumToByteMatchesDotNet()
    {
        using TestDatabase db = new(b => b.EnumStorage = EnumStorageMode.Text);
        db.Table<EnumCastProjectionRow>().Schema.CreateTable();
        db.Table<EnumCastProjectionRow>().Add(new EnumCastProjectionRow { Id = 1, Tier = EnumCastTier.Silver });

        byte expected = (byte)EnumCastTier.Silver;
        Assert.Equal((byte)2, expected);

        byte actual = db.Table<EnumCastProjectionRow>().Select(x => (byte)x.Tier).Single();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void CastTextEnumToUnderlyingInWhereMatchesDotNet()
    {
        using TestDatabase db = new(b => b.EnumStorage = EnumStorageMode.Text);
        db.Table<EnumCastProjectionRow>().Schema.CreateTable();
        db.Table<EnumCastProjectionRow>().Add(new EnumCastProjectionRow { Id = 1, Tier = EnumCastTier.Silver });
        db.Table<EnumCastProjectionRow>().Add(new EnumCastProjectionRow { Id = 2, Tier = EnumCastTier.Gold });

        List<int> expected = new[]
            {
                (1, EnumCastTier.Silver),
                (2, EnumCastTier.Gold),
            }
            .Where(r => (int)r.Item2 == 2)
            .Select(r => r.Item1)
            .ToList();
        Assert.Equal(new[] { 1 }, expected);

        List<int> actual = db.Table<EnumCastProjectionRow>()
            .Where(x => (int)x.Tier == 2)
            .Select(x => x.Id)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void CastTextEnumToLongInWhereMatchesDotNet()
    {
        using TestDatabase db = new(b => b.EnumStorage = EnumStorageMode.Text);
        db.Table<EnumCastProjectionRow>().Schema.CreateTable();
        db.Table<EnumCastProjectionRow>().Add(new EnumCastProjectionRow { Id = 1, Tier = EnumCastTier.Silver });
        db.Table<EnumCastProjectionRow>().Add(new EnumCastProjectionRow { Id = 2, Tier = EnumCastTier.Gold });

        List<int> expected = new[]
            {
                (1, EnumCastTier.Silver),
                (2, EnumCastTier.Gold),
            }
            .Where(r => (long)r.Item2 == 2L)
            .Select(r => r.Item1)
            .ToList();
        Assert.Equal(new[] { 1 }, expected);

        List<int> actual = db.Table<EnumCastProjectionRow>()
            .Where(x => (long)x.Tier == 2L)
            .Select(x => x.Id)
            .ToList();

        Assert.Equal(expected, actual);
    }
}
