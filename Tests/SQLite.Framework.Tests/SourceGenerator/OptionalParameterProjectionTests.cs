using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H24mOptionalArgRows")]
public class H24mOptionalArgRow
{
    [Key]
    public int Id { get; set; }

    public string Name { get; set; } = "";
}

public static class H24mOptionalArgFunctions
{
    public static string Pad(string name, int width = 4)
    {
        return name.PadRight(width, '.');
    }

    public static string Tag(string name, string? suffix = null)
    {
        return name + "|" + (suffix ?? "none");
    }
}

public class OptionalParameterProjectionTests
{
    [Fact]
    public void OmittedNumericDefaultArgumentMatchesLinq()
    {
        using TestDatabase db = Setup();

        List<string> expected = Rows()
            .OrderBy(r => r.Id)
            .Select(r => H24mOptionalArgFunctions.Pad(r.Name))
            .ToList();

        List<string> actual = db.Table<H24mOptionalArgRow>()
            .OrderBy(r => r.Id)
            .Select(r => H24mOptionalArgFunctions.Pad(r.Name))
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void OmittedNullDefaultArgumentMatchesLinq()
    {
        using TestDatabase db = Setup();

        List<string> expected = Rows()
            .OrderBy(r => r.Id)
            .Select(r => H24mOptionalArgFunctions.Tag(r.Name))
            .ToList();

        List<string> actual = db.Table<H24mOptionalArgRow>()
            .OrderBy(r => r.Id)
            .Select(r => H24mOptionalArgFunctions.Tag(r.Name))
            .ToList();

        Assert.Equal(expected, actual);
    }

    private static List<H24mOptionalArgRow> Rows()
    {
        return
        [
            new H24mOptionalArgRow { Id = 1, Name = "a" },
            new H24mOptionalArgRow { Id = 2, Name = "bc" }
        ];
    }

    private static TestDatabase Setup()
    {
        TestDatabase db = new();
        db.Table<H24mOptionalArgRow>().Schema.CreateTable();
        db.Table<H24mOptionalArgRow>().AddRange(Rows());
        return db;
    }
}
