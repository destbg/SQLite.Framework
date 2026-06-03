using System.ComponentModel.DataAnnotations;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public enum ParseFruit
{
    Apple = 1,
    Banana = 2,
    Cherry = 3,
}

public sealed class ParseFruitRow
{
    [Key]
    public int Id { get; set; }

    public ParseFruit Fruit { get; set; }

    public string Code { get; set; } = "";
}

public class EnumParseIgnoreCaseTests
{
    private static (TestDatabase db, ParseFruitRow[] seed) Seed(params (int id, ParseFruit fruit, string code)[] rows)
    {
        TestDatabase db = new();
        db.Table<ParseFruitRow>().Schema.CreateTable();
        ParseFruitRow[] seed = rows.Select(r => new ParseFruitRow { Id = r.id, Fruit = r.fruit, Code = r.code }).ToArray();
        foreach (ParseFruitRow r in seed)
        {
            db.Table<ParseFruitRow>().Add(r);
        }

        return (db, seed);
    }

    [Fact]
    public void Parse_IgnoreCase_Projection_MatchesDotNet()
    {
        (TestDatabase db, ParseFruitRow[] seed) = Seed(
            (1, ParseFruit.Banana, "banana"),
            (2, ParseFruit.Cherry, "CHERRY"),
            (3, ParseFruit.Apple, "ApPlE"));
        using TestDatabase _ = db;

        List<ParseFruit> expected = seed.OrderBy(r => r.Id).Select(r => Enum.Parse<ParseFruit>(r.Code, true)).ToList();
        List<ParseFruit> actual = db.Table<ParseFruitRow>().OrderBy(r => r.Id).Select(r => Enum.Parse<ParseFruit>(r.Code, true)).ToList();

        Assert.Equal([ParseFruit.Banana, ParseFruit.Cherry, ParseFruit.Apple], expected);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Parse_IgnoreCase_Where_MatchesDotNet()
    {
        (TestDatabase db, ParseFruitRow[] seed) = Seed(
            (1, ParseFruit.Banana, "banana"),
            (2, ParseFruit.Apple, "cherry"),
            (3, ParseFruit.Cherry, "cherry"));
        using TestDatabase _ = db;

        List<int> expected = seed.Where(r => r.Fruit == Enum.Parse<ParseFruit>(r.Code, true)).Select(r => r.Id).OrderBy(i => i).ToList();
        List<int> actual = db.Table<ParseFruitRow>().Where(r => r.Fruit == Enum.Parse<ParseFruit>(r.Code, true)).Select(r => r.Id).OrderBy(i => i).ToList();

        Assert.Equal([1, 3], expected);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Parse_CaseSensitive_ExactMatch_MatchesDotNet()
    {
        (TestDatabase db, ParseFruitRow[] seed) = Seed((1, ParseFruit.Banana, "Banana"));
        using TestDatabase _ = db;

        ParseFruit expected = Enum.Parse<ParseFruit>("Banana", false);
        ParseFruit actual = db.Table<ParseFruitRow>().Select(r => Enum.Parse<ParseFruit>(r.Code, false)).First();

        Assert.Equal(ParseFruit.Banana, expected);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Parse_NoFlag_ExactMatch_MatchesDotNet()
    {
        (TestDatabase db, ParseFruitRow[] seed) = Seed((1, ParseFruit.Cherry, "Cherry"));
        using TestDatabase _ = db;

        ParseFruit expected = Enum.Parse<ParseFruit>("Cherry");
        ParseFruit actual = db.Table<ParseFruitRow>().Select(r => Enum.Parse<ParseFruit>(r.Code)).First();

        Assert.Equal(ParseFruit.Cherry, expected);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Parse_NonGeneric_IgnoreCase_MatchesDotNet()
    {
        (TestDatabase db, ParseFruitRow[] seed) = Seed((1, ParseFruit.Apple, "apple"));
        using TestDatabase _ = db;

        ParseFruit expected = (ParseFruit)Enum.Parse(typeof(ParseFruit), "apple", true);
        ParseFruit actual = db.Table<ParseFruitRow>().Select(r => (ParseFruit)Enum.Parse(typeof(ParseFruit), r.Code, true)).First();

        Assert.Equal(ParseFruit.Apple, expected);
        Assert.Equal(expected, actual);
    }
}
