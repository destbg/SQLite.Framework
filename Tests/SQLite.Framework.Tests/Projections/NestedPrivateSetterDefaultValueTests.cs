using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H22nNestedNoteRows")]
public class H22nNestedNoteRow
{
    [Key]
    public int Id { get; set; }

    public string Name { get; set; } = "";
}

public class H22nNestedNoteBox
{
    public string Label { get; set; } = "";

    public string Note { get; private set; } = "unset";

    public int Weight { get; private set; } = 5;
}

public class H22nNestedNoteHolder
{
    public int Id { get; set; }

    public H22nNestedNoteBox? Inner { get; set; }
}

public class NestedPrivateSetterDefaultValueTests
{
    [Fact]
    public void UnassignedNestedStringWithANonPublicSetterKeepsItsDeclaredDefault()
    {
        using TestDatabase db = Setup();

        List<string> expected = Rows()
            .OrderBy(r => r.Id)
            .Select(r => new H22nNestedNoteHolder { Id = r.Id, Inner = new H22nNestedNoteBox { Label = r.Name } })
            .Select(h => h.Inner!.Note)
            .ToList();

        List<string> actual = db.Table<H22nNestedNoteRow>()
            .OrderBy(r => r.Id)
            .Select(r => new H22nNestedNoteHolder { Id = r.Id, Inner = new H22nNestedNoteBox { Label = r.Name } })
            .ToList()
            .Select(h => h.Inner!.Note)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void UnassignedNestedNumberWithANonPublicSetterKeepsItsDeclaredDefault()
    {
        using TestDatabase db = Setup();

        List<int> expected = Rows()
            .OrderBy(r => r.Id)
            .Select(r => new H22nNestedNoteHolder { Id = r.Id, Inner = new H22nNestedNoteBox { Label = r.Name } })
            .Select(h => h.Inner!.Weight)
            .ToList();

        List<int> actual = db.Table<H22nNestedNoteRow>()
            .OrderBy(r => r.Id)
            .Select(r => new H22nNestedNoteHolder { Id = r.Id, Inner = new H22nNestedNoteBox { Label = r.Name } })
            .ToList()
            .Select(h => h.Inner!.Weight)
            .ToList();

        Assert.Equal(expected, actual);
    }

    private static List<H22nNestedNoteRow> Rows()
    {
        return
        [
            new H22nNestedNoteRow { Id = 1, Name = "a" },
            new H22nNestedNoteRow { Id = 2, Name = "b" }
        ];
    }

    private static TestDatabase Setup()
    {
        TestDatabase db = new();
        db.Table<H22nNestedNoteRow>().Schema.CreateTable();
        db.Table<H22nNestedNoteRow>().AddRange(Rows());
        return db;
    }
}
