using System.ComponentModel.DataAnnotations;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests.BugHunt;

internal enum ParseColorCi
{
    Red = 1,
    Green = 2,
}

internal enum UlongEnum : ulong
{
    A = 1,
}

internal sealed class EnumParseCiRow
{
    [Key]
    public int Id { get; set; }

    public ParseColorCi Color { get; set; }

    public string Code { get; set; } = "";
}

internal sealed class UlongEnumRow
{
    [Key]
    public int Id { get; set; }

    public UlongEnum Value { get; set; }

    public string Code { get; set; } = "";
}

public class EnumBugTests
{
    [Fact]
    public void EnumParse_IgnoreCase_Where_MatchesLinqToObjects()
    {
        using TestDatabase db = new();
        db.Table<EnumParseCiRow>().Schema.CreateTable();
        db.Table<EnumParseCiRow>().Add(new EnumParseCiRow { Id = 1, Color = ParseColorCi.Green, Code = "green" });

        (int Id, ParseColorCi Color, string Code)[] seed = [(1, ParseColorCi.Green, "green")];
        List<int> expected = seed.Where(r => r.Color == Enum.Parse<ParseColorCi>(r.Code, true)).Select(r => r.Id).ToList();
        List<int> actual = db.Table<EnumParseCiRow>().Where(r => r.Color == Enum.Parse<ParseColorCi>(r.Code, true)).Select(r => r.Id).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void EnumParse_IgnoreCase_Projection_MatchesLinqToObjects()
    {
        using TestDatabase db = new();
        db.Table<EnumParseCiRow>().Schema.CreateTable();
        db.Table<EnumParseCiRow>().Add(new EnumParseCiRow { Id = 1, Color = ParseColorCi.Green, Code = "green" });

        ParseColorCi expected = Enum.Parse<ParseColorCi>("green", true);
        ParseColorCi actual = db.Table<EnumParseCiRow>().Select(r => Enum.Parse<ParseColorCi>(r.Code, true)).First();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void EnumToString_UlongUndefinedValue_MatchesDotNet()
    {
        using TestDatabase db = new();
        db.Table<UlongEnumRow>().Schema.CreateTable();
        db.Table<UlongEnumRow>().Add(new UlongEnumRow { Id = 1, Value = (UlongEnum)9999999999999999999UL });

        string expected = ((UlongEnum)9999999999999999999UL).ToString();
        string actual = db.Table<UlongEnumRow>().Select(r => r.Value.ToString()).First();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void EnumParse_FromColumn_UlongValue_MatchesLinqToObjects()
    {
        const string s = "9999999999999999999";
        ulong parsed = ulong.Parse(s);

        using TestDatabase db = new();
        db.Table<UlongEnumRow>().Schema.CreateTable();
        db.Table<UlongEnumRow>().Add(new UlongEnumRow { Id = 1, Value = (UlongEnum)parsed, Code = s });

        (int Id, UlongEnum Value, string Code)[] seed = [(1, (UlongEnum)parsed, s)];
        List<ulong> expected = seed.Select(r => (ulong)Enum.Parse<UlongEnum>(r.Code)).ToList();
        List<ulong> actual = db.Table<UlongEnumRow>().Select(r => (ulong)Enum.Parse<UlongEnum>(r.Code)).ToList();

        Assert.Equal(expected, actual);
    }
}
