using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Enums;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public enum H21cParseColor
{
    Red = 1,
    Green = 2,
    Blue = 3,
}

[Table("H21cParseRows")]
public class H21cParseRow
{
    [Key]
    public int Id { get; set; }

    public H21cParseColor Value { get; set; }

    public string Code { get; set; } = "";
}

public class EnumParseTextStorageComparisonTests
{
    [Fact]
    public void ParsedCodeComparedToEnumColumnMatchesDotNet()
    {
        using TestDatabase db = NewDb(out List<H21cParseRow> rows);

        List<int> expected = rows
            .Where(r => r.Value == Enum.Parse<H21cParseColor>(r.Code))
            .Select(r => r.Id)
            .OrderBy(id => id)
            .ToList();
        List<int> actual = db.Table<H21cParseRow>()
            .Where(r => r.Value == Enum.Parse<H21cParseColor>(r.Code))
            .Select(r => r.Id)
            .OrderBy(id => id)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ParsedCodeComparedToEnumConstantMatchesDotNet()
    {
        using TestDatabase db = NewDb(out List<H21cParseRow> rows);

        List<int> expected = rows
            .Where(r => Enum.Parse<H21cParseColor>(r.Code) == H21cParseColor.Red)
            .Select(r => r.Id)
            .OrderBy(id => id)
            .ToList();
        List<int> actual = db.Table<H21cParseRow>()
            .Where(r => Enum.Parse<H21cParseColor>(r.Code) == H21cParseColor.Red)
            .Select(r => r.Id)
            .OrderBy(id => id)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ParsedCodeProjectionMatchesDotNet()
    {
        using TestDatabase db = NewDb(out List<H21cParseRow> rows);

        List<H21cParseColor> expected = rows
            .OrderBy(r => r.Id)
            .Select(r => Enum.Parse<H21cParseColor>(r.Code))
            .ToList();
        List<H21cParseColor> actual = db.Table<H21cParseRow>()
            .OrderBy(r => r.Id)
            .Select(r => Enum.Parse<H21cParseColor>(r.Code))
            .ToList();

        Assert.Equal(expected, actual);
    }

    private static TestDatabase NewDb(out List<H21cParseRow> rows)
    {
        TestDatabase db = new(b => b.EnumStorage = EnumStorageMode.Text);
        db.Table<H21cParseRow>().Schema.CreateTable();
        rows =
        [
            new H21cParseRow { Id = 1, Value = H21cParseColor.Red, Code = "Red" },
            new H21cParseRow { Id = 2, Value = H21cParseColor.Green, Code = "2" },
            new H21cParseRow { Id = 3, Value = H21cParseColor.Blue, Code = "Blue" },
            new H21cParseRow { Id = 4, Value = H21cParseColor.Red, Code = "Green" },
            new H21cParseRow { Id = 5, Value = H21cParseColor.Green, Code = "Red" },
        ];
        db.Table<H21cParseRow>().AddRange(rows);
        return db;
    }
}
