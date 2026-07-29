using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H24lWhiteSpaceIndexRows")]
public class H24lWhiteSpaceIndexRow
{
    [Key]
    public int Id { get; set; }

    public string Text { get; set; } = "";
}

public class IsWhiteSpaceAtStringIndexParityTests
{
    [Fact]
    public void ProjectsWhetherTheCharacterAtAnIndexIsWhiteSpace()
    {
        using TestDatabase db = Seed();
        List<H24lWhiteSpaceIndexRow> local = Rows();

        List<bool> expected = local
            .OrderBy(r => r.Id)
            .Select(r => char.IsWhiteSpace(r.Text, 3))
            .ToList();

        List<bool> actual = db.Table<H24lWhiteSpaceIndexRow>()
            .OrderBy(r => r.Id)
            .Select(r => char.IsWhiteSpace(r.Text, 3))
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void FiltersOnWhetherTheCharacterAtAnIndexIsWhiteSpace()
    {
        using TestDatabase db = Seed();
        List<H24lWhiteSpaceIndexRow> local = Rows();

        List<int> expected = local
            .Where(r => char.IsWhiteSpace(r.Text, 3))
            .Select(r => r.Id)
            .OrderBy(id => id)
            .ToList();

        List<int> actual = db.Table<H24lWhiteSpaceIndexRow>()
            .Where(r => char.IsWhiteSpace(r.Text, 3))
            .Select(r => r.Id)
            .OrderBy(id => id)
            .ToList();

        Assert.Equal(expected, actual);
    }

    private static List<H24lWhiteSpaceIndexRow> Rows()
    {
        return
        [
            new H24lWhiteSpaceIndexRow { Id = 1, Text = "abc def" },
            new H24lWhiteSpaceIndexRow { Id = 2, Text = "abcdefg" }
        ];
    }

    private static TestDatabase Seed()
    {
        TestDatabase db = new();
        db.Table<H24lWhiteSpaceIndexRow>().Schema.CreateTable();
        db.Table<H24lWhiteSpaceIndexRow>().AddRange(Rows());
        return db;
    }
}
