using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H23aScalarConcatRows")]
public class H23aScalarConcatRow
{
    [Key]
    public int Id { get; set; }

    public string? First { get; set; }
}

public class ScalarProjectionParameterConcatParityTests
{
    [Fact]
    public void PlusOfAScalarProjectionParameterWithALiteralTreatsNullAsEmpty()
    {
        using TestDatabase db = Setup();

        List<string> expected = Rows()
            .OrderBy(r => r.Id)
            .Select(r => r.First)
            .Select(s => s + "!")
            .ToList();

        List<string> actual = db.Table<H23aScalarConcatRow>()
            .OrderBy(r => r.Id)
            .Select(r => r.First)
            .Select(s => s + "!")
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void StringConcatOfAScalarProjectionParameterWithALiteralTreatsNullAsEmpty()
    {
        using TestDatabase db = Setup();

        List<string> expected = Rows()
            .OrderBy(r => r.Id)
            .Select(r => r.First)
            .Select(s => string.Concat(s, "!"))
            .ToList();

        List<string> actual = db.Table<H23aScalarConcatRow>()
            .OrderBy(r => r.Id)
            .Select(r => r.First)
            .Select(s => string.Concat(s, "!"))
            .ToList();

        Assert.Equal(expected, actual);
    }

    private static List<H23aScalarConcatRow> Rows() =>
    [
        new H23aScalarConcatRow { Id = 1, First = "a" },
        new H23aScalarConcatRow { Id = 2, First = null },
        new H23aScalarConcatRow { Id = 3, First = "b" }
    ];

    private static TestDatabase Setup()
    {
        TestDatabase db = new();
        db.Table<H23aScalarConcatRow>().Schema.CreateTable();
        db.Table<H23aScalarConcatRow>().AddRange(Rows());
        return db;
    }
}
