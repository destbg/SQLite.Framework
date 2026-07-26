using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("UnsetMemberTypeRows")]
public class UnsetMemberTypeRow
{
    [Key]
    public int Id { get; set; }

    public int A { get; set; }
}

public class UnsetMemberTypeSide
{
    public int X { get; set; }

    public int? MaybeNumber { get; set; }

    public string? Text { get; set; }
}

public class ProjectedUnsetNestedMemberTypeTests
{
    [Fact]
    public void UnsetNullableNumberMemberReadsAsNull()
    {
        using TestDatabase db = Setup(nameof(UnsetNullableNumberMemberReadsAsNull));

        List<int?> expected = Rows().OrderBy(r => r.Id)
            .Select(r => new { r.Id, N = new UnsetMemberTypeSide { X = r.A } })
            .Select(x => x.N.MaybeNumber)
            .ToList();

        List<int?> actual = db.Table<UnsetMemberTypeRow>().OrderBy(r => r.Id)
            .Select(r => new { r.Id, N = new UnsetMemberTypeSide { X = r.A } })
            .Select(x => x.N.MaybeNumber)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void UnsetTextMemberReadsAsNull()
    {
        using TestDatabase db = Setup(nameof(UnsetTextMemberReadsAsNull));

        List<string?> expected = Rows().OrderBy(r => r.Id)
            .Select(r => new { r.Id, N = new UnsetMemberTypeSide { X = r.A } })
            .Select(x => x.N.Text)
            .ToList();

        List<string?> actual = db.Table<UnsetMemberTypeRow>().OrderBy(r => r.Id)
            .Select(r => new { r.Id, N = new UnsetMemberTypeSide { X = r.A } })
            .Select(x => x.N.Text)
            .ToList();

        Assert.Equal(expected, actual);
    }

    private static List<UnsetMemberTypeRow> Rows()
    {
        return
        [
            new UnsetMemberTypeRow { Id = 1, A = 10 },
            new UnsetMemberTypeRow { Id = 2, A = 20 }
        ];
    }

    private static TestDatabase Setup(string methodName)
    {
        TestDatabase db = new(null, methodName);
        db.Table<UnsetMemberTypeRow>().Schema.CreateTable();
        db.Table<UnsetMemberTypeRow>().AddRange(Rows());
        return db;
    }
}
