using System.ComponentModel.DataAnnotations;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

internal enum EcColor { Red = 0, Green = 1, Blue = 2 }

internal enum EcByteFlag : byte { None = 0, A = 1, B = 2 }

internal enum EcLongCode : long { Zero = 0, One = 1, Big = 5000000000 }

internal sealed class EcRow
{
    [Key]
    public int Id { get; set; }
    public EcColor Color { get; set; }
    public EcByteFlag Flag { get; set; }
    public EcLongCode Code { get; set; }
}

public class CapturedNumericToEnumCastTests
{
    private static readonly EcRow[] Data =
    [
        new EcRow { Id = 1, Color = EcColor.Green, Flag = EcByteFlag.A, Code = EcLongCode.One },
        new EcRow { Id = 2, Color = EcColor.Blue, Flag = EcByteFlag.B, Code = EcLongCode.Big },
    ];

    private static TestDatabase CreateDb()
    {
        TestDatabase db = new();
        db.Table<EcRow>().Schema.CreateTable();
        db.Table<EcRow>().AddRange(Data);
        return db;
    }

    [Fact]
    public void CapturedInt_CastToEnum_InWhere()
    {
        using TestDatabase db = CreateDb();
        int code = 1;
        List<int> oracle = Data.Where(x => x.Color == (EcColor)code).Select(x => x.Id).OrderBy(i => i).ToList();
        List<int> actual = db.Table<EcRow>().Where(x => x.Color == (EcColor)code).Select(x => x.Id).OrderBy(i => i).ToList();
        Assert.Equal([1], oracle);
        Assert.Equal(oracle, actual);
    }

    [Fact]
    public void LiteralInt_CastToEnum_InWhere()
    {
        using TestDatabase db = CreateDb();
        List<int> oracle = Data.Where(x => x.Color == (EcColor)2).Select(x => x.Id).OrderBy(i => i).ToList();
        List<int> actual = db.Table<EcRow>().Where(x => x.Color == (EcColor)2).Select(x => x.Id).OrderBy(i => i).ToList();
        Assert.Equal([2], oracle);
        Assert.Equal(oracle, actual);
    }

    [Fact]
    public void CapturedInt_CastToNullableEnum_InWhere()
    {
        using TestDatabase db = CreateDb();
        int code = 1;
        List<int> oracle = Data.Where(x => x.Color == (EcColor?)code).Select(x => x.Id).OrderBy(i => i).ToList();
        List<int> actual = db.Table<EcRow>().Where(x => x.Color == (EcColor?)code).Select(x => x.Id).OrderBy(i => i).ToList();
        Assert.Equal([1], oracle);
        Assert.Equal(oracle, actual);
    }

    [Fact]
    public void CapturedInt_CastToByteBackedEnum_InWhere()
    {
        using TestDatabase db = CreateDb();
        int code = 1;
        List<int> oracle = Data.Where(x => x.Flag == (EcByteFlag)code).Select(x => x.Id).OrderBy(i => i).ToList();
        List<int> actual = db.Table<EcRow>().Where(x => x.Flag == (EcByteFlag)code).Select(x => x.Id).OrderBy(i => i).ToList();
        Assert.Equal([1], oracle);
        Assert.Equal(oracle, actual);
    }

    [Fact]
    public void CapturedLong_CastToLongBackedEnum_InWhere()
    {
        using TestDatabase db = CreateDb();
        long code = 5000000000L;
        List<int> oracle = Data.Where(x => x.Code == (EcLongCode)code).Select(x => x.Id).OrderBy(i => i).ToList();
        List<int> actual = db.Table<EcRow>().Where(x => x.Code == (EcLongCode)code).Select(x => x.Id).OrderBy(i => i).ToList();
        Assert.Equal([2], oracle);
        Assert.Equal(oracle, actual);
    }

    [Fact]
    public void CastedEnum_OnLeftSide_InWhere()
    {
        using TestDatabase db = CreateDb();
        int code = 2;
        List<int> oracle = Data.Where(x => (EcColor)code == x.Color).Select(x => x.Id).OrderBy(i => i).ToList();
        List<int> actual = db.Table<EcRow>().Where(x => (EcColor)code == x.Color).Select(x => x.Id).OrderBy(i => i).ToList();
        Assert.Equal([2], oracle);
        Assert.Equal(oracle, actual);
    }

    [Fact]
    public void CapturedInt_CastToEnum_InProjection()
    {
        using TestDatabase db = CreateDb();
        int code = 1;
        List<bool> oracle = Data.OrderBy(x => x.Id).Select(x => x.Color == (EcColor)code).ToList();
        List<bool> actual = db.Table<EcRow>().OrderBy(x => x.Id).Select(x => x.Color == (EcColor)code).ToList();
        Assert.Equal([true, false], oracle);
        Assert.Equal(oracle, actual);
    }

    [Fact]
    public void CrossEnumCast_InWhere()
    {
        using TestDatabase db = CreateDb();
        EcByteFlag flag = EcByteFlag.A;
        List<int> oracle = Data.Where(x => x.Color == (EcColor)flag).Select(x => x.Id).OrderBy(i => i).ToList();
        List<int> actual = db.Table<EcRow>().Where(x => x.Color == (EcColor)flag).Select(x => x.Id).OrderBy(i => i).ToList();
        Assert.Equal([1], oracle);
        Assert.Equal(oracle, actual);
    }

    [Fact]
    public void CapturedEnumVariable_NoCast_StillWorks()
    {
        using TestDatabase db = CreateDb();
        EcColor target = EcColor.Blue;
        List<int> oracle = Data.Where(x => x.Color == target).Select(x => x.Id).OrderBy(i => i).ToList();
        List<int> actual = db.Table<EcRow>().Where(x => x.Color == target).Select(x => x.Id).OrderBy(i => i).ToList();
        Assert.Equal([2], oracle);
        Assert.Equal(oracle, actual);
    }
}
