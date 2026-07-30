using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H25zListInitRows")]
public class H25zListInitRow
{
    [Key]
    public int Id { get; set; }

    public string Name { get; set; } = "";
}

public class InlineListInitializerCountTests
{
    [Fact]
    public void CountOfAnInlineIntegerListLiteralMatchesInMemory()
    {
        using TestDatabase db = Setup(nameof(CountOfAnInlineIntegerListLiteralMatchesInMemory));

        List<int> expected = Rows()
            .OrderBy(r => r.Id)
            .Select(r => new List<int> { r.Id, r.Id + 1 }.Count)
            .ToList();

        List<int> actual = db.Table<H25zListInitRow>()
            .OrderBy(r => r.Id)
            .Select(r => new List<int> { r.Id, r.Id + 1 }.Count)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void CountOfAThreeElementInlineIntegerListLiteralMatchesInMemory()
    {
        using TestDatabase db = Setup(nameof(CountOfAThreeElementInlineIntegerListLiteralMatchesInMemory));

        List<int> expected = Rows()
            .OrderBy(r => r.Id)
            .Select(r => new List<int> { r.Id, r.Id + 1, r.Id + 2 }.Count)
            .ToList();

        List<int> actual = db.Table<H25zListInitRow>()
            .OrderBy(r => r.Id)
            .Select(r => new List<int> { r.Id, r.Id + 1, r.Id + 2 }.Count)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void CountOfAnInlineStringListLiteralMatchesInMemory()
    {
        using TestDatabase db = Setup(nameof(CountOfAnInlineStringListLiteralMatchesInMemory));

        List<int> expected = Rows()
            .OrderBy(r => r.Id)
            .Select(r => new List<string> { r.Name, "z" }.Count)
            .ToList();

        List<int> actual = db.Table<H25zListInitRow>()
            .OrderBy(r => r.Id)
            .Select(r => new List<string> { r.Name, "z" }.Count)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void LengthOfAnInlineArrayLiteralMatchesInMemory()
    {
        using TestDatabase db = Setup(nameof(LengthOfAnInlineArrayLiteralMatchesInMemory));

        List<int> expected = Rows()
            .OrderBy(r => r.Id)
            .Select(r => new[] { r.Id, r.Id + 1 }.Length)
            .ToList();

        List<int> actual = db.Table<H25zListInitRow>()
            .OrderBy(r => r.Id)
            .Select(r => new[] { r.Id, r.Id + 1 }.Length)
            .ToList();

        Assert.Equal(expected, actual);
    }

    private static List<H25zListInitRow> Rows()
    {
        return
        [
            new H25zListInitRow { Id = 1, Name = "alpha" },
            new H25zListInitRow { Id = 2, Name = "beta" },
            new H25zListInitRow { Id = 3, Name = "gamma" }
        ];
    }

    private static TestDatabase Setup(string methodName)
    {
        TestDatabase db = new(null, methodName);
        db.Table<H25zListInitRow>().Schema.CreateTable();
        db.Table<H25zListInitRow>().AddRange(Rows());
        return db;
    }
}
