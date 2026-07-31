using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H26qConcatRows")]
public class H26qConcatRow
{
    [Key]
    public int Id { get; set; }

    public string Name { get; set; } = "";

    public int Amount { get; set; }
}

public class ConcatArrayComparisonElementBracketingTests
{
    [Fact]
    public void JoinsAComparisonElementIntoTheSeparatedText()
    {
        using TestDatabase db = Setup(nameof(JoinsAComparisonElementIntoTheSeparatedText));

        List<string> expected = Rows()
            .OrderBy(r => r.Id)
            .Select(r => string.Join("-", new object[] { r.Name, r.Amount > 5 ? 1 : 0 }))
            .ToList();

        List<string> actual = db.Table<H26qConcatRow>()
            .OrderBy(r => r.Id)
            .Select(r => string.Join("-", new object[] { r.Name, r.Amount > 5 }))
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ConcatenatesAComparisonElementIntoTheText()
    {
        using TestDatabase db = Setup(nameof(ConcatenatesAComparisonElementIntoTheText));

        List<string> expected = Rows()
            .OrderBy(r => r.Id)
            .Select(r => string.Concat(new object?[] { r.Name, r.Amount > 5 ? 1 : 0 }))
            .ToList();

        List<string> actual = db.Table<H26qConcatRow>()
            .OrderBy(r => r.Id)
            .Select(r => string.Concat(new object?[] { r.Name, r.Amount > 5 }))
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ConcatenatesATwoSidedRangeElementIntoTheText()
    {
        using TestDatabase db = Setup(nameof(ConcatenatesATwoSidedRangeElementIntoTheText));

        List<string> expected = Rows()
            .OrderBy(r => r.Id)
            .Select(r => string.Concat(new object?[] { r.Name, r.Amount > 5 && r.Amount < 100 ? 1 : 0 }))
            .ToList();

        List<string> actual = db.Table<H26qConcatRow>()
            .OrderBy(r => r.Id)
            .Select(r => string.Concat(new object?[] { r.Name, r.Amount > 5 && r.Amount < 100 }))
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ConcatenatesAPrefixTestElementIntoTheText()
    {
        using TestDatabase db = Setup(nameof(ConcatenatesAPrefixTestElementIntoTheText));

        List<string> expected = Rows()
            .OrderBy(r => r.Id)
            .Select(r => string.Concat(new object?[] { r.Name, r.Name.StartsWith("a") ? 1 : 0 }))
            .ToList();

        List<string> actual = db.Table<H26qConcatRow>()
            .OrderBy(r => r.Id)
            .Select(r => string.Concat(new object?[] { r.Name, r.Name.StartsWith("a") }))
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void FiltersOnTextThatConcatenatesAComparisonElement()
    {
        using TestDatabase db = Setup(nameof(FiltersOnTextThatConcatenatesAComparisonElement));

        List<int> expected = Rows()
            .Where(r => string.Concat(new object?[] { r.Name, r.Amount > 5 }).StartsWith("alpha"))
            .Select(r => r.Id)
            .OrderBy(i => i)
            .ToList();

        List<int> actual = db.Table<H26qConcatRow>()
            .Where(r => string.Concat(new object?[] { r.Name, r.Amount > 5 }).StartsWith("alpha"))
            .Select(r => r.Id)
            .OrderBy(i => i)
            .ToList();

        Assert.Equal(expected, actual);
    }

    private static List<H26qConcatRow> Rows()
    {
        return
        [
            new H26qConcatRow { Id = 1, Name = "alpha", Amount = 3 },
            new H26qConcatRow { Id = 2, Name = "beta", Amount = 42 }
        ];
    }

    private static TestDatabase Setup(string methodName)
    {
        TestDatabase db = new(null, methodName);
        db.Table<H26qConcatRow>().Schema.CreateTable();
        db.Table<H26qConcatRow>().AddRange(Rows());
        return db;
    }
}
