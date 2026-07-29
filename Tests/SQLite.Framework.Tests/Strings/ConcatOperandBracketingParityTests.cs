using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H24lConcatBracketRows")]
public class H24lConcatBracketRow
{
    [Key]
    public int Id { get; set; }

    public string Name { get; set; } = "";
}

public class ConcatOperandBracketingParityTests
{
    [Fact]
    public void ConcatsAnIndexOfResultWithAConstant()
    {
        using TestDatabase db = Seed();
        List<H24lConcatBracketRow> local = Rows();

        List<string> expected = local
            .OrderBy(r => r.Id)
            .Select(r => string.Concat(r.Name.IndexOf("-"), "!"))
            .ToList();

        List<string> actual = db.Table<H24lConcatBracketRow>()
            .OrderBy(r => r.Id)
            .Select(r => string.Concat(r.Name.IndexOf("-"), "!"))
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void JoinsAnIndexOfResultWithAnotherColumn()
    {
        using TestDatabase db = Seed();
        List<H24lConcatBracketRow> local = Rows();

        List<string> expected = local
            .OrderBy(r => r.Id)
            .Select(r => string.Join("-", new object[] { r.Name.IndexOf("-"), r.Id }))
            .ToList();

        List<string> actual = db.Table<H24lConcatBracketRow>()
            .OrderBy(r => r.Id)
            .Select(r => string.Join("-", new object[] { r.Name.IndexOf("-"), r.Id }))
            .ToList();

        Assert.Equal(expected, actual);
    }

    private static List<H24lConcatBracketRow> Rows()
    {
        return
        [
            new H24lConcatBracketRow { Id = 5, Name = "ab-cd" },
            new H24lConcatBracketRow { Id = 6, Name = "a-bcd" }
        ];
    }

    private static TestDatabase Seed()
    {
        TestDatabase db = new();
        db.Table<H24lConcatBracketRow>().Schema.CreateTable();
        db.Table<H24lConcatBracketRow>().AddRange(Rows());
        return db;
    }
}
