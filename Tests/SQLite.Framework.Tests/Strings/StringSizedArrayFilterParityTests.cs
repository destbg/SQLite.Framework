using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H21dSizedArrayRows")]
public class H21dSizedArrayRow
{
    [Key]
    public int Id { get; set; }

    public string Name { get; set; } = "";
}

public class StringSizedArrayFilterParityTests
{
    private static List<H21dSizedArrayRow> Rows()
    {
        return
        [
            new H21dSizedArrayRow { Id = 1, Name = "a" },
            new H21dSizedArrayRow { Id = 2, Name = "b" }
        ];
    }

    private static TestDatabase Setup()
    {
        TestDatabase db = new();
        db.Table<H21dSizedArrayRow>().Schema.CreateTable();
        db.Table<H21dSizedArrayRow>().AddRange(Rows());
        return db;
    }

    [Fact]
    public void WhereConcatSizedIntArrayMatchesDotNet()
    {
        using TestDatabase db = Setup();

        List<int> expected = Rows()
            .Where(r => string.Concat(new int[2]) == "00")
            .Select(r => r.Id)
            .ToList();

        List<int> actual = db.Table<H21dSizedArrayRow>()
            .Where(r => string.Concat(new int[2]) == "00")
            .Select(r => r.Id)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void WhereJoinSizedStringArrayMatchesDotNet()
    {
        using TestDatabase db = Setup();

        List<int> expected = Rows()
            .Where(r => string.Join("-", new string[2]) == "-")
            .Select(r => r.Id)
            .ToList();

        List<int> actual = db.Table<H21dSizedArrayRow>()
            .Where(r => string.Join("-", new string[2]) == "-")
            .Select(r => r.Id)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void OrderByThenConcatSizedIntArrayMatchesDotNet()
    {
        using TestDatabase db = Setup();

        List<string> expected = Rows()
            .OrderBy(r => string.Concat(new int[2]))
            .ThenBy(r => r.Id)
            .Select(r => r.Name)
            .ToList();

        List<string> actual = db.Table<H21dSizedArrayRow>()
            .OrderBy(r => string.Concat(new int[2]))
            .ThenBy(r => r.Id)
            .Select(r => r.Name)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void WhereConcatSizedStringArrayMatchesDotNet()
    {
        using TestDatabase db = Setup();

        List<int> expected = Rows()
            .Where(r => string.Concat(new string[2]) == "")
            .Select(r => r.Id)
            .ToList();

        List<int> actual = db.Table<H21dSizedArrayRow>()
            .Where(r => string.Concat(new string[2]) == "")
            .Select(r => r.Id)
            .ToList();

        Assert.Equal(expected, actual);
    }
}
