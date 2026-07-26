using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H22nInitNoteRows")]
public class H22nInitNoteRow
{
    [Key]
    public int Id { get; set; }

    public string Name { get; set; } = "";
}

public class H22nInitNoteView
{
    public int Id { get; set; }

    public string Note { get; init; } = "unset";

    public int Score { get; init; } = 7;
}

public class InitOnlyPropertyDefaultValueTests
{
    [Fact]
    public void UnassignedInitOnlyStringKeepsItsDeclaredDefault()
    {
        using TestDatabase db = Setup();

        List<string> expected = Rows()
            .OrderBy(r => r.Id)
            .Select(r => new H22nInitNoteView { Id = r.Id })
            .Select(v => v.Note)
            .ToList();

        List<string> actual = db.Table<H22nInitNoteRow>()
            .OrderBy(r => r.Id)
            .Select(r => new H22nInitNoteView { Id = r.Id })
            .ToList()
            .Select(v => v.Note)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void UnassignedInitOnlyNumberKeepsItsDeclaredDefault()
    {
        using TestDatabase db = Setup();

        List<int> expected = Rows()
            .OrderBy(r => r.Id)
            .Select(r => new H22nInitNoteView { Id = r.Id })
            .Select(v => v.Score)
            .ToList();

        List<int> actual = db.Table<H22nInitNoteRow>()
            .OrderBy(r => r.Id)
            .Select(r => new H22nInitNoteView { Id = r.Id })
            .ToList()
            .Select(v => v.Score)
            .ToList();

        Assert.Equal(expected, actual);
    }

    private static List<H22nInitNoteRow> Rows()
    {
        return
        [
            new H22nInitNoteRow { Id = 1, Name = "a" },
            new H22nInitNoteRow { Id = 2, Name = "b" }
        ];
    }

    private static TestDatabase Setup()
    {
        TestDatabase db = new();
        db.Table<H22nInitNoteRow>().Schema.CreateTable();
        db.Table<H22nInitNoteRow>().AddRange(Rows());
        return db;
    }
}
