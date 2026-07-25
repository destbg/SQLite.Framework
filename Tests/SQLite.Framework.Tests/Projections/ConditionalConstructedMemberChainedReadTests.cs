using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("CcmRow")]
public class CcmRow
{
    [Key]
    public int Id { get; set; }

    public string? Name { get; set; }
}

public class CcmOuter
{
    public int Id { get; set; }

    public CcmInner? Inner { get; set; }
}

public class CcmInner
{
    public string? Name { get; set; }
}

public class ConditionalConstructedMemberChainedReadTests
{
    private static List<CcmRow> Rows() =>
    [
        new CcmRow { Id = 1, Name = null },
        new CcmRow { Id = 2, Name = "n2" },
        new CcmRow { Id = 3, Name = "n3" },
    ];

    private static TestDatabase Setup()
    {
        TestDatabase db = new();
        db.Table<CcmRow>().Schema.CreateTable();
        db.Table<CcmRow>().AddRange(Rows());
        return db;
    }

    [Fact]
    public void AnonymousConditionalInnerThenMemberReadMatchesLinq()
    {
        using TestDatabase db = Setup();

        List<string> expected = Rows().OrderBy(r => r.Id)
            .Select(r => new { r.Id, Inner = r.Id > 1 ? new { r.Name } : null })
            .Select(x => x.Inner == null ? "none" : x.Inner.Name ?? "null")
            .ToList();

        List<string> actual = db.Table<CcmRow>()
            .OrderBy(r => r.Id)
            .Select(r => new { r.Id, Inner = r.Id > 1 ? new { r.Name } : null })
            .Select(x => x.Inner == null ? "none" : x.Inner.Name ?? "null")
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void NamedConditionalInnerThenMemberReadMatchesLinq()
    {
        using TestDatabase db = Setup();

        List<string> expected = Rows().OrderBy(r => r.Id)
            .Select(r => new CcmOuter { Id = r.Id, Inner = r.Id > 1 ? new CcmInner { Name = r.Name } : null })
            .Select(x => x.Inner == null ? "none" : x.Inner.Name ?? "null")
            .ToList();

        List<string> actual = db.Table<CcmRow>()
            .OrderBy(r => r.Id)
            .Select(r => new CcmOuter { Id = r.Id, Inner = r.Id > 1 ? new CcmInner { Name = r.Name } : null })
            .Select(x => x.Inner == null ? "none" : x.Inner.Name ?? "null")
            .ToList();

        Assert.Equal(expected, actual);
    }
}
