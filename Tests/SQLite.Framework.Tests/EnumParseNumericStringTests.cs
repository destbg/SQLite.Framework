using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

file enum ParseColor
{
    Red = 1,
    Green = 2,
    Blue = 3,
}

[Table("EnumParseRows")]
file sealed class EnumParseRow
{
    [Key]
    public int Id { get; set; }

    public ParseColor Value { get; set; }

    public string Code { get; set; } = "";
}

public class EnumParseNumericStringTests
{
    private static readonly (int Id, int Value, string Code)[] Seed =
    [
        (1, 1, "Red"),
        (2, 2, "2"),
        (3, 3, "Blue"),
        (4, 1, "2"),
        (5, 2, "Red"),
    ];

    private static TestDatabase CreateDb()
    {
        TestDatabase db = new();
        db.Table<EnumParseRow>().Schema.CreateTable();
        foreach ((int id, int value, string code) in Seed)
        {
            db.Table<EnumParseRow>().Add(new EnumParseRow { Id = id, Value = (ParseColor)value, Code = code });
        }

        return db;
    }

    [Fact]
    public void EnumParse_FilterByParsedValue_MatchesDotNet()
    {
        using TestDatabase db = CreateDb();

        List<int> expected = Seed
            .Where(r => (ParseColor)r.Value == Enum.Parse<ParseColor>(r.Code))
            .Select(r => r.Id)
            .OrderBy(x => x)
            .ToList();
        List<int> actual = db.Table<EnumParseRow>()
            .Where(r => r.Value == Enum.Parse<ParseColor>(r.Code))
            .Select(r => r.Id)
            .OrderBy(x => x)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void EnumParse_ProjectParsedValue_MatchesDotNet()
    {
        using TestDatabase db = CreateDb();

        List<ParseColor> expected = Seed.OrderBy(r => r.Id).Select(r => Enum.Parse<ParseColor>(r.Code)).ToList();
        List<ParseColor> actual = db.Table<EnumParseRow>()
            .OrderBy(r => r.Id)
            .Select(r => Enum.Parse<ParseColor>(r.Code))
            .ToList();

        Assert.Equal(expected, actual);
    }
}
