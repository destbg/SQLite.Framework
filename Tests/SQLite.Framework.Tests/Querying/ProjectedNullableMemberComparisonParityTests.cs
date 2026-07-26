using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H22zPairRows")]
public class H22zPairRow
{
    [Key]
    public int Id { get; set; }

    public string? First { get; set; }

    public string? Second { get; set; }
}

public class ProjectedNullableMemberComparisonParityTests
{
    [Fact]
    public void EqualityOfTwoProjectedNullableStringMembersMatchesNullToNull()
    {
        using TestDatabase db = Setup();

        List<int> expected = Rows()
            .Select(r => new { r.Id, A = r.First, B = r.Second })
            .Where(x => x.A == x.B)
            .Select(x => x.Id)
            .OrderBy(v => v)
            .ToList();

        List<int> actual = db.Table<H22zPairRow>()
            .Select(r => new { r.Id, A = r.First, B = r.Second })
            .Where(x => x.A == x.B)
            .Select(x => x.Id)
            .AsEnumerable()
            .OrderBy(v => v)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void InequalityOfTwoProjectedNullableStringMembersTreatsNullToNullAsEqual()
    {
        using TestDatabase db = Setup();

        List<int> expected = Rows()
            .Select(r => new { r.Id, A = r.First, B = r.Second })
            .Where(x => x.A != x.B)
            .Select(x => x.Id)
            .OrderBy(v => v)
            .ToList();

        List<int> actual = db.Table<H22zPairRow>()
            .Select(r => new { r.Id, A = r.First, B = r.Second })
            .Where(x => x.A != x.B)
            .Select(x => x.Id)
            .AsEnumerable()
            .OrderBy(v => v)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void EqualityOfADtoProjectedNullableStringMemberMatchesNullToNull()
    {
        using TestDatabase db = Setup();

        List<int> expected = Rows()
            .Select(r => new H22zPairRow { Id = r.Id, First = r.First, Second = r.Second })
            .Where(x => x.First == x.Second)
            .Select(x => x.Id)
            .OrderBy(v => v)
            .ToList();

        List<int> actual = db.Table<H22zPairRow>()
            .Select(r => new H22zPairRow { Id = r.Id, First = r.First, Second = r.Second })
            .Where(x => x.First == x.Second)
            .Select(x => x.Id)
            .AsEnumerable()
            .OrderBy(v => v)
            .ToList();

        Assert.Equal(expected, actual);
    }

    private static List<H22zPairRow> Rows() =>
    [
        new H22zPairRow { Id = 1, First = "a", Second = "a" },
        new H22zPairRow { Id = 2, First = null, Second = null },
        new H22zPairRow { Id = 3, First = "b", Second = null },
        new H22zPairRow { Id = 4, First = null, Second = "c" },
    ];

    private static TestDatabase Setup()
    {
        TestDatabase db = new();
        db.Table<H22zPairRow>().Schema.CreateTable();
        db.Table<H22zPairRow>().AddRange(Rows());
        return db;
    }
}
