using System.ComponentModel.DataAnnotations;
using SQLite.Framework.Enums;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

internal enum Shade
{
    Red = 0,
    Green = 1,
    Blue = 2
}

internal sealed class TypedKeyRow
{
    [Key]
    public int Id { get; set; }

    public Shade Color { get; set; }

    public byte Rank { get; set; }

    public short Score { get; set; }
}

internal sealed class TypedKeyItem
{
    public Shade Color { get; set; }

    public byte Rank { get; set; }

    public short Score { get; set; }
}

public class AnyLocalListTypedKeyColumnTests
{
    private static readonly TypedKeyRow[] Data =
    [
        new TypedKeyRow { Id = 1, Color = Shade.Red, Rank = 10, Score = 100 },
        new TypedKeyRow { Id = 2, Color = Shade.Green, Rank = 20, Score = 200 },
        new TypedKeyRow { Id = 3, Color = Shade.Blue, Rank = 10, Score = 300 },
        new TypedKeyRow { Id = 4, Color = Shade.Green, Rank = 30, Score = 100 },
    ];

    private static TestDatabase CreateDb(Action<SQLiteOptionsBuilder>? configure = null)
    {
        TestDatabase db = configure == null ? new() : new(configure);
        db.Table<TypedKeyRow>().Schema.CreateTable();
        foreach (TypedKeyRow r in Data)
        {
            db.Table<TypedKeyRow>().Add(r);
        }

        return db;
    }

    [Fact]
    public void EnumSingleKeyColumn()
    {
        using TestDatabase db = CreateDb();
        List<TypedKeyItem> list = [new TypedKeyItem { Color = Shade.Green }, new TypedKeyItem { Color = Shade.Blue }];

        List<int> expected = Data.Where(a => list.Any(f => f.Color == a.Color)).Select(r => r.Id).OrderBy(i => i).ToList();
        List<int> actual = db.Table<TypedKeyRow>().Where(a => list.Any(f => f.Color == a.Color)).Select(r => r.Id).OrderBy(i => i).ToList();

        Assert.Equal([2, 3, 4], expected);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void EnumKeyOnLeftValueOnRight()
    {
        using TestDatabase db = CreateDb();
        List<TypedKeyItem> list = [new TypedKeyItem { Color = Shade.Red }];

        List<int> expected = Data.Where(a => list.Any(f => a.Color == f.Color)).Select(r => r.Id).OrderBy(i => i).ToList();
        List<int> actual = db.Table<TypedKeyRow>().Where(a => list.Any(f => a.Color == f.Color)).Select(r => r.Id).OrderBy(i => i).ToList();

        Assert.Equal([1], expected);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void EnumWithByteCompositeKey()
    {
        using TestDatabase db = CreateDb();
        List<TypedKeyItem> list =
        [
            new TypedKeyItem { Color = Shade.Green, Rank = 20 },
            new TypedKeyItem { Color = Shade.Blue, Rank = 10 },
        ];

        List<int> expected = Data.Where(a => list.Any(f => f.Color == a.Color && f.Rank == a.Rank)).Select(r => r.Id).OrderBy(i => i).ToList();
        List<int> actual = db.Table<TypedKeyRow>().Where(a => list.Any(f => f.Color == a.Color && f.Rank == a.Rank)).Select(r => r.Id).OrderBy(i => i).ToList();

        Assert.Equal([2, 3], expected);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ByteSingleKeyColumn()
    {
        using TestDatabase db = CreateDb();
        List<TypedKeyItem> list = [new TypedKeyItem { Rank = 10 }, new TypedKeyItem { Rank = 30 }];

        List<int> expected = Data.Where(a => list.Any(f => f.Rank == a.Rank)).Select(r => r.Id).OrderBy(i => i).ToList();
        List<int> actual = db.Table<TypedKeyRow>().Where(a => list.Any(f => f.Rank == a.Rank)).Select(r => r.Id).OrderBy(i => i).ToList();

        Assert.Equal([1, 3, 4], expected);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ShortSingleKeyColumn()
    {
        using TestDatabase db = CreateDb();
        List<TypedKeyItem> list = [new TypedKeyItem { Score = 100 }];

        List<int> expected = Data.Where(a => list.Any(f => f.Score == a.Score)).Select(r => r.Id).OrderBy(i => i).ToList();
        List<int> actual = db.Table<TypedKeyRow>().Where(a => list.Any(f => f.Score == a.Score)).Select(r => r.Id).OrderBy(i => i).ToList();

        Assert.Equal([1, 4], expected);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void EnumSingleKeyColumnUnderTextStorage()
    {
        using TestDatabase db = CreateDb(b => b.EnumStorage = EnumStorageMode.Text);
        List<TypedKeyItem> list = [new TypedKeyItem { Color = Shade.Green }, new TypedKeyItem { Color = Shade.Blue }];

        List<int> expected = Data.Where(a => list.Any(f => f.Color == a.Color)).Select(r => r.Id).OrderBy(i => i).ToList();
        List<int> actual = db.Table<TypedKeyRow>().Where(a => list.Any(f => f.Color == a.Color)).Select(r => r.Id).OrderBy(i => i).ToList();

        Assert.Equal([2, 3, 4], expected);
        Assert.Equal(expected, actual);
    }
}
