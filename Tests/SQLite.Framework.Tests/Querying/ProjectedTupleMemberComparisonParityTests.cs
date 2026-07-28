using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H23aTuplePairRows")]
public class H23aTuplePairRow
{
    [Key]
    public int Id { get; set; }

    public string? First { get; set; }

    public string? Second { get; set; }
}

public class ProjectedTupleMemberComparisonParityTests
{
    [Fact]
    public void EqualityOfTwoTupleProjectedNullableStringMembersMatchesNullToNull()
    {
        using TestDatabase db = Setup();

        List<string?> expected = Rows()
            .Select(r => new ValueTuple<string?, string?>(r.First, r.Second))
            .Where(t => t.Item1 == t.Item2)
            .Select(t => t.Item1)
            .OrderBy(v => v)
            .ToList();

        List<string?> actual = db.Table<H23aTuplePairRow>()
            .Select(r => new ValueTuple<string?, string?>(r.First, r.Second))
            .Where(t => t.Item1 == t.Item2)
            .AsEnumerable()
            .Select(t => t.Item1)
            .OrderBy(v => v)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void InequalityOfTwoTupleProjectedNullableStringMembersTreatsNullToNullAsEqual()
    {
        using TestDatabase db = Setup();

        List<string?> expected = Rows()
            .Select(r => new ValueTuple<string?, string?>(r.First, r.Second))
            .Where(t => t.Item1 != t.Item2)
            .Select(t => t.Item1)
            .OrderBy(v => v)
            .ToList();

        List<string?> actual = db.Table<H23aTuplePairRow>()
            .Select(r => new ValueTuple<string?, string?>(r.First, r.Second))
            .Where(t => t.Item1 != t.Item2)
            .AsEnumerable()
            .Select(t => t.Item1)
            .OrderBy(v => v)
            .ToList();

        Assert.Equal(expected, actual);
    }

    private static List<H23aTuplePairRow> Rows() =>
    [
        new H23aTuplePairRow { Id = 1, First = "a", Second = "a" },
        new H23aTuplePairRow { Id = 2, First = null, Second = null },
        new H23aTuplePairRow { Id = 3, First = "b", Second = null },
        new H23aTuplePairRow { Id = 4, First = null, Second = "c" }
    ];

    private static TestDatabase Setup()
    {
        TestDatabase db = new();
        db.Table<H23aTuplePairRow>().Schema.CreateTable();
        db.Table<H23aTuplePairRow>().AddRange(Rows());
        return db;
    }
}
