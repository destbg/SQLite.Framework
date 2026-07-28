using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H23mFoldedConstantRows")]
public class H23mFoldedConstantRow
{
    [Key]
    public int Id { get; set; }

    public string Name { get; set; } = "";
}

public static class H23mFoldedConstantFunctions
{
    public static string CombineNumber(string name, int number)
    {
        return name + "#" + number;
    }

    public static string CombineText(string name, string suffix)
    {
        return name + "#" + suffix;
    }

    public static string CombineLong(string name, long number)
    {
        return name + "#" + number;
    }
}

public class FoldedConstantProjectionArgumentTests
{
    private const string TextPart = "P";

    private const int Step = 3;

    [Fact]
    public void LiteralArithmeticArgumentMatchesLinq()
    {
        using TestDatabase db = Setup();

        List<string> expected = Rows()
            .ConvertAll(r => H23mFoldedConstantFunctions.CombineNumber(r.Name, 1 + 2));

        List<string> actual = db.Table<H23mFoldedConstantRow>()
            .OrderBy(r => r.Id)
            .Select(r => H23mFoldedConstantFunctions.CombineNumber(r.Name, 1 + 2))
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ConstantTextConcatenationArgumentMatchesLinq()
    {
        using TestDatabase db = Setup();

        List<string> expected = Rows()
            .ConvertAll(r => H23mFoldedConstantFunctions.CombineText(r.Name, TextPart + "-"));

        List<string> actual = db.Table<H23mFoldedConstantRow>()
            .OrderBy(r => r.Id)
            .Select(r => H23mFoldedConstantFunctions.CombineText(r.Name, TextPart + "-"))
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void WidenedConstantArgumentMatchesLinq()
    {
        using TestDatabase db = Setup();

        List<string> expected = Rows()
            .ConvertAll(r => H23mFoldedConstantFunctions.CombineLong(r.Name, Step));

        List<string> actual = db.Table<H23mFoldedConstantRow>()
            .OrderBy(r => r.Id)
            .Select(r => H23mFoldedConstantFunctions.CombineLong(r.Name, Step))
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void NameOfArgumentMatchesLinq()
    {
        using TestDatabase db = Setup();

        List<string> expected = Rows()
            .ConvertAll(r => H23mFoldedConstantFunctions.CombineText(r.Name, nameof(H23mFoldedConstantRow.Name)));

        List<string> actual = db.Table<H23mFoldedConstantRow>()
            .OrderBy(r => r.Id)
            .Select(r => H23mFoldedConstantFunctions.CombineText(r.Name, nameof(H23mFoldedConstantRow.Name)))
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void DefaultValueExpressionArgumentMatchesLinq()
    {
        using TestDatabase db = Setup();

        List<string> expected = Rows()
            .ConvertAll(r => H23mFoldedConstantFunctions.CombineNumber(r.Name, default(int)));

        List<string> actual = db.Table<H23mFoldedConstantRow>()
            .OrderBy(r => r.Id)
            .Select(r => H23mFoldedConstantFunctions.CombineNumber(r.Name, default(int)))
            .ToList();

        Assert.Equal(expected, actual);
    }

    private static List<H23mFoldedConstantRow> Rows()
    {
        return
        [
            new H23mFoldedConstantRow { Id = 1, Name = "a" },
            new H23mFoldedConstantRow { Id = 2, Name = "b" }
        ];
    }

    private static TestDatabase Setup()
    {
        TestDatabase db = new();
        db.Table<H23mFoldedConstantRow>().Schema.CreateTable();
        db.Table<H23mFoldedConstantRow>().AddRange(Rows());
        return db;
    }
}
