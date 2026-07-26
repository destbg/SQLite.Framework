using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H22zConcatRows")]
public class H22zConcatRow
{
    [Key]
    public int Id { get; set; }

    public string? First { get; set; }

    public string? Second { get; set; }
}

public class ProjectedNullableMemberConcatParityTests
{
    [Fact]
    public void ConcatOfTwoProjectedNullableStringMembersTreatsNullAsEmpty()
    {
        using TestDatabase db = Setup();

        List<string> expected = Rows()
            .Select(r => new { r.Id, A = r.First, B = r.Second })
            .OrderBy(x => x.Id)
            .Select(x => x.A + x.B)
            .ToList();

        List<string> actual = db.Table<H22zConcatRow>()
            .Select(r => new { r.Id, A = r.First, B = r.Second })
            .OrderBy(x => x.Id)
            .Select(x => x.A + x.B)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ConcatOfAProjectedNullableStringMemberWithALiteralTreatsNullAsEmpty()
    {
        using TestDatabase db = Setup();

        List<string> expected = Rows()
            .Select(r => new { r.Id, A = r.First })
            .OrderBy(x => x.Id)
            .Select(x => x.A + "!")
            .ToList();

        List<string> actual = db.Table<H22zConcatRow>()
            .Select(r => new { r.Id, A = r.First })
            .OrderBy(x => x.Id)
            .Select(x => x.A + "!")
            .ToList();

        Assert.Equal(expected, actual);
    }

    private static List<H22zConcatRow> Rows() =>
    [
        new H22zConcatRow { Id = 1, First = "a", Second = "a" },
        new H22zConcatRow { Id = 2, First = null, Second = null },
        new H22zConcatRow { Id = 3, First = "b", Second = null },
        new H22zConcatRow { Id = 4, First = null, Second = "c" },
    ];

    private static TestDatabase Setup()
    {
        TestDatabase db = new();
        db.Table<H22zConcatRow>().Schema.CreateTable();
        db.Table<H22zConcatRow>().AddRange(Rows());
        return db;
    }
}
