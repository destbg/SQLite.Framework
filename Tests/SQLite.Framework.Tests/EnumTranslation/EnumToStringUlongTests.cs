using System.ComponentModel.DataAnnotations;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public enum UlongFlags : ulong
{
    Zero = 0,
    One = 1,
    AboveLongMax = 9223372036854775808,
}

public enum SignedNums : long
{
    Neg = -5,
    Pos = 5,
}

public sealed class UlongFlagsRow
{
    [Key]
    public int Id { get; set; }

    public UlongFlags Value { get; set; }
}

public sealed class SignedNumsRow
{
    [Key]
    public int Id { get; set; }

    public SignedNums Value { get; set; }
}

public class EnumToStringUlongTests
{
    [Fact]
    public void UlongEnum_ToString_MatchesDotNet()
    {
        UlongFlags[] values =
        [
            UlongFlags.One,
            UlongFlags.AboveLongMax,
            (UlongFlags)9999999999999999999UL,
            (UlongFlags)5UL,
            UlongFlags.Zero,
        ];
        using TestDatabase db = new();
        db.Table<UlongFlagsRow>().Schema.CreateTable();
        for (int i = 0; i < values.Length; i++)
        {
            db.Table<UlongFlagsRow>().Add(new UlongFlagsRow { Id = i + 1, Value = values[i] });
        }

        List<string> expected = values.Select(v => v.ToString()).ToList();
        List<string> actual = db.Table<UlongFlagsRow>().OrderBy(r => r.Id).Select(r => r.Value.ToString()).ToList();

        Assert.Equal(["One", "AboveLongMax", "9999999999999999999", "5", "Zero"], expected);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void SignedEnum_ToString_UndefinedNegative_StaysSigned()
    {
        SignedNums[] values =
        [
            SignedNums.Neg,
            SignedNums.Pos,
            (SignedNums)(-100),
            (SignedNums)42,
        ];
        using TestDatabase db = new();
        db.Table<SignedNumsRow>().Schema.CreateTable();
        for (int i = 0; i < values.Length; i++)
        {
            db.Table<SignedNumsRow>().Add(new SignedNumsRow { Id = i + 1, Value = values[i] });
        }

        List<string> expected = values.Select(v => v.ToString()).ToList();
        List<string> actual = db.Table<SignedNumsRow>().OrderBy(r => r.Id).Select(r => r.Value.ToString()).ToList();

        Assert.Equal(["Neg", "Pos", "-100", "42"], expected);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void UlongEnum_ToString_InWhere_MatchesDotNet()
    {
        using TestDatabase db = new();
        db.Table<UlongFlagsRow>().Schema.CreateTable();
        db.Table<UlongFlagsRow>().Add(new UlongFlagsRow { Id = 1, Value = (UlongFlags)9999999999999999999UL });
        db.Table<UlongFlagsRow>().Add(new UlongFlagsRow { Id = 2, Value = UlongFlags.One });

        List<int> actual = db.Table<UlongFlagsRow>()
            .Where(r => r.Value.ToString() == "9999999999999999999")
            .Select(r => r.Id)
            .ToList();

        Assert.Equal([1], actual);
    }
}
