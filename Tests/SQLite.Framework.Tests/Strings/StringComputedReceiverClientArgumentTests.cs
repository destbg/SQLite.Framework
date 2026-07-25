using System.ComponentModel.DataAnnotations;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class StringComputedRow
{
    [Key]
    public int Id { get; set; }

    public string Name { get; set; } = "";
}

public class StringComputedReceiverClientArgumentTests
{
    private static List<StringComputedRow> Rows() =>
    [
        new() { Id = 1, Name = "abcd" },
        new() { Id = 2, Name = "xyz" },
    ];

    private static TestDatabase Seed()
    {
        TestDatabase db = new();
        db.Table<StringComputedRow>().Schema.CreateTable();
        db.Table<StringComputedRow>().AddRange(Rows());
        return db;
    }

    private static string PatternFor(int id)
    {
        return id == 1 ? "cd" : "q";
    }

    private static char TrimCharFor(int id)
    {
        return id == 1 ? 'e' : 'q';
    }

    [Fact]
    public void ReplaceStaticHelperAfterSubstringInSelectProjects()
    {
        using TestDatabase db = Seed();

        List<string> expected = Rows()
            .OrderBy(r => r.Id)
            .Select(r => r.Name.Substring(1).Replace(PatternFor(r.Id), "!"))
            .ToList();
        Assert.Equal(["b!", "yz"], expected);

        List<string> actual = db.Table<StringComputedRow>()
            .OrderBy(r => r.Id)
            .Select(r => r.Name.Substring(1).Replace(PatternFor(r.Id), "!"))
            .ToList();
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void TrimStaticHelperCharsAfterSubstringInSelectProjects()
    {
        using TestDatabase db = Seed();

        List<string> expected = Rows()
            .OrderBy(r => r.Id)
            .Select(r => r.Name.Substring(1).Trim(TrimCharFor(r.Id), 'd'))
            .ToList();
        Assert.Equal(["bc", "yz"], expected);

        List<string> actual = db.Table<StringComputedRow>()
            .OrderBy(r => r.Id)
            .Select(r => r.Name.Substring(1).Trim(TrimCharFor(r.Id), 'd'))
            .ToList();
        Assert.Equal(expected, actual);
    }
}
