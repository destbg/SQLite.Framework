using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H23aTupleConcatRows")]
public class H23aTupleConcatRow
{
    [Key]
    public int Id { get; set; }

    public string? First { get; set; }

    public string? Second { get; set; }
}

public class ProjectedTupleMemberConcatParityTests
{
    [Fact]
    public void PlusOfTwoTupleProjectedNullableStringMembersTreatsNullAsEmpty()
    {
        using TestDatabase db = Setup();

        List<string> expected = Rows()
            .OrderBy(r => r.Id)
            .Select(r => new ValueTuple<string?, string?>(r.First, r.Second))
            .Select(t => t.Item1 + t.Item2)
            .ToList();

        List<string> actual = db.Table<H23aTupleConcatRow>()
            .OrderBy(r => r.Id)
            .Select(r => new ValueTuple<string?, string?>(r.First, r.Second))
            .Select(t => t.Item1 + t.Item2)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void StringConcatOfTwoTupleProjectedNullableStringMembersTreatsNullAsEmpty()
    {
        using TestDatabase db = Setup();

        List<string> expected = Rows()
            .OrderBy(r => r.Id)
            .Select(r => new ValueTuple<string?, string?>(r.First, r.Second))
            .Select(t => string.Concat(t.Item1, t.Item2))
            .ToList();

        List<string> actual = db.Table<H23aTupleConcatRow>()
            .OrderBy(r => r.Id)
            .Select(r => new ValueTuple<string?, string?>(r.First, r.Second))
            .Select(t => string.Concat(t.Item1, t.Item2))
            .ToList();

        Assert.Equal(expected, actual);
    }

    private static List<H23aTupleConcatRow> Rows() =>
    [
        new H23aTupleConcatRow { Id = 1, First = "a", Second = "a" },
        new H23aTupleConcatRow { Id = 2, First = null, Second = null },
        new H23aTupleConcatRow { Id = 3, First = "b", Second = null },
        new H23aTupleConcatRow { Id = 4, First = null, Second = "c" }
    ];

    private static TestDatabase Setup()
    {
        TestDatabase db = new();
        db.Table<H23aTupleConcatRow>().Schema.CreateTable();
        db.Table<H23aTupleConcatRow>().AddRange(Rows());
        return db;
    }
}
