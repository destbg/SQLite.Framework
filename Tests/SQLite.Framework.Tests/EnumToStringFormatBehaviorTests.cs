using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class EnumToStringFormatBehaviorTests
{
    [Fact]
    public void NameNoFormat()
    {
        Run(q => q.Select(r => r.Color.ToString()));
    }

    [Fact]
    public void NameGeneralFormat()
    {
        Run(q => q.Select(r => r.Color.ToString("G")));
    }

    [Fact]
    public void DecimalByte()
    {
        Run(q => q.Select(r => r.ByteVal.ToString("D")));
    }

    [Fact]
    public void DecimalSByte()
    {
        Run(q => q.Select(r => r.SByteVal.ToString("D")));
    }

    [Fact]
    public void DecimalShort()
    {
        Run(q => q.Select(r => r.ShortVal.ToString("D")));
    }

    [Fact]
    public void DecimalUShort()
    {
        Run(q => q.Select(r => r.UShortVal.ToString("D")));
    }

    [Fact]
    public void DecimalInt()
    {
        Run(q => q.Select(r => r.IntVal.ToString("D")));
    }

    [Fact]
    public void DecimalUInt()
    {
        Run(q => q.Select(r => r.UIntVal.ToString("D")));
    }

    [Fact]
    public void DecimalLong()
    {
        Run(q => q.Select(r => r.LongVal.ToString("D")));
    }

    [Fact]
    public void DecimalULong()
    {
        Run(q => q.Select(r => r.ULongVal.ToString("D")));
    }

    [Fact]
    public void HexUpperByte()
    {
        Run(q => q.Select(r => r.ByteVal.ToString("X")));
    }

    [Fact]
    public void HexUpperSByte()
    {
        Run(q => q.Select(r => r.SByteVal.ToString("X")));
    }

    [Fact]
    public void HexUpperShort()
    {
        Run(q => q.Select(r => r.ShortVal.ToString("X")));
    }

    [Fact]
    public void HexUpperUShort()
    {
        Run(q => q.Select(r => r.UShortVal.ToString("X")));
    }

    [Fact]
    public void HexUpperInt()
    {
        Run(q => q.Select(r => r.IntVal.ToString("X")));
    }

    [Fact]
    public void HexUpperUInt()
    {
        Run(q => q.Select(r => r.UIntVal.ToString("X")));
    }

    [Fact]
    public void HexUpperLong()
    {
        Run(q => q.Select(r => r.LongVal.ToString("X")));
    }

    [Fact]
    public void HexUpperULong()
    {
        Run(q => q.Select(r => r.ULongVal.ToString("X")));
    }

    [Fact]
    public void HexLowerInt()
    {
        Run(q => q.Select(r => r.IntVal.ToString("x")));
    }

    [Fact]
    public void HexLowerLong()
    {
        Run(q => q.Select(r => r.LongVal.ToString("x")));
    }

    [Fact]
    public void CapturedFormat_ClientEvaluates()
    {
        using TestDatabase db = new();
        db.Table<EnumFmtRow>().Schema.CreateTable();
        db.Table<EnumFmtRow>().AddRange(Data());

        string fmt = "X";
        List<string> oracle = Data().AsQueryable().OrderBy(r => r.Id).Select(r => r.IntVal.ToString(fmt)).ToList();
        List<string> actual = db.Table<EnumFmtRow>().OrderBy(r => r.Id).Select(r => r.IntVal.ToString(fmt)).ToList();

        Assert.Equal(oracle, actual);
    }

    [Fact]
    public void MultiCharFormat_ThrowsLikeDotNet()
    {
        using TestDatabase db = new();
        db.Table<EnumFmtRow>().Schema.CreateTable();
        db.Table<EnumFmtRow>().AddRange(Data());

        Assert.ThrowsAny<FormatException>(() => Data().AsQueryable().Select(r => r.IntVal.ToString("DD")).ToList());

        Exception ex = Assert.ThrowsAny<Exception>(() => db.Table<EnumFmtRow>().Select(r => r.IntVal.ToString("DD")).ToList());
        Assert.True(ex is FormatException || ex.InnerException is FormatException);
    }

    [Fact]
    public void FlagsFormat_ClientEvaluates()
    {
        Run(q => q.Select(r => r.IntVal.ToString("F")));
    }

    [Fact]
    public void FlagsEnumName_ClientEvaluates()
    {
        Run(q => q.Select(r => r.Flags.ToString()));
    }

    [Fact]
    public void EmptyFormat_ClientEvaluates()
    {
        Run(q => q.Select(r => r.Color.ToString("")));
    }

    [Fact]
    public void NonConstantFormatInWhere_Throws()
    {
        using TestDatabase db = new();
        db.Table<EnumFmtRow>().Schema.CreateTable();
        db.Table<EnumFmtRow>().AddRange(Data());

        Assert.Throws<NotSupportedException>(() =>
            db.Table<EnumFmtRow>().Where(r => r.IntVal.ToString(r.Fmt) == "x").ToList());
    }

    private static void Run(Func<IQueryable<EnumFmtRow>, IQueryable<string>> project)
    {
        using TestDatabase db = new();
        db.Table<EnumFmtRow>().Schema.CreateTable();
        db.Table<EnumFmtRow>().AddRange(Data());

        List<string> oracle = project(Data().AsQueryable().OrderBy(r => r.Id)).ToList();
        List<string> actual = project(db.Table<EnumFmtRow>().OrderBy(r => r.Id)).ToList();

        Assert.Equal(oracle, actual);
    }


    private static List<EnumFmtRow> Data()
    {
        return new List<EnumFmtRow>
        {
            new()
            {
                Id = 1, ByteVal = EfByteEnum.Five, SByteVal = EfSByteEnum.NegOne, ShortVal = EfShortEnum.NegOne,
                UShortVal = EfUShortEnum.Max, IntVal = EfIntEnum.NegOne, UIntVal = EfUIntEnum.Max,
                LongVal = EfLongEnum.NegOne, ULongVal = EfULongEnum.Max, Color = EfColorEnum.Green,
                Flags = EfFlagsEnum.A, Fmt = "D"
            },
            new()
            {
                Id = 2, ByteVal = EfByteEnum.Big, SByteVal = EfSByteEnum.Max, ShortVal = EfShortEnum.Big,
                UShortVal = EfUShortEnum.Zero, IntVal = EfIntEnum.Big, UIntVal = EfUIntEnum.Zero,
                LongVal = EfLongEnum.Big, ULongVal = EfULongEnum.Huge, Color = EfColorEnum.Blue,
                Flags = EfFlagsEnum.B, Fmt = "X"
            },
            new()
            {
                Id = 3, ByteVal = EfByteEnum.Zero, SByteVal = EfSByteEnum.Zero, ShortVal = EfShortEnum.Mid,
                UShortVal = EfUShortEnum.Max, IntVal = EfIntEnum.Two, UIntVal = EfUIntEnum.Max,
                LongVal = EfLongEnum.Zero, ULongVal = EfULongEnum.Mid, Color = EfColorEnum.Red,
                Flags = EfFlagsEnum.None, Fmt = "G"
            },
        };
    }
}

public enum EfByteEnum : byte { Zero = 0, Five = 5, Big = 200 }

public enum EfSByteEnum : sbyte { NegOne = -1, Zero = 0, Max = 127 }

public enum EfShortEnum : short { NegOne = -1, Mid = 256, Big = 32000 }

public enum EfUShortEnum : ushort { Zero = 0, Max = 65535 }

public enum EfIntEnum { NegOne = -1, Zero = 0, Two = 2, Big = 1000000 }

public enum EfUIntEnum : uint { Zero = 0, Max = 4294967295 }

public enum EfLongEnum : long { NegOne = -1, Zero = 0, Big = 9000000000 }

public enum EfULongEnum : ulong { Zero = 0, Mid = 5, Huge = 10000000000000000000, Max = 18446744073709551615 }

public enum EfColorEnum { Red = 1, Green = 2, Blue = 4 }

[System.Flags]
public enum EfFlagsEnum { None = 0, A = 1, B = 2 }

public class EnumFmtRow
{
    [System.ComponentModel.DataAnnotations.Key] public int Id { get; set; }
    public EfByteEnum ByteVal { get; set; }
    public EfSByteEnum SByteVal { get; set; }
    public EfShortEnum ShortVal { get; set; }
    public EfUShortEnum UShortVal { get; set; }
    public EfIntEnum IntVal { get; set; }
    public EfUIntEnum UIntVal { get; set; }
    public EfLongEnum LongVal { get; set; }
    public EfULongEnum ULongVal { get; set; }
    public EfColorEnum Color { get; set; }
    public EfFlagsEnum Flags { get; set; }
    public string Fmt { get; set; } = "D";
}
