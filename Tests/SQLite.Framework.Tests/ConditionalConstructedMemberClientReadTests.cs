using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("CmcRow")]
public class CmcRow
{
    [Key]
    public int Id { get; set; }

    public string? Name { get; set; }

    public int? MaybeCount { get; set; }
}

public class CmcPart
{
    public int Num { get; set; }

    public int? MaybeNum { get; set; }

    public string? Label { get; set; }
}

public class CmcWrap
{
    public CmcPart? Part { get; set; }
}

public static class CmcClientFns
{
    public static string Tag(string value)
    {
        return "[" + value + "]";
    }

    public static int Pass(int value)
    {
        return value;
    }
}

public class ConditionalConstructedMemberClientReadTests
{
    private static List<CmcRow> Rows() =>
    [
        new CmcRow { Id = 1, Name = null, MaybeCount = null },
        new CmcRow { Id = 2, Name = "n2", MaybeCount = 5 },
        new CmcRow { Id = 3, Name = "n3", MaybeCount = 7 },
    ];

    private static TestDatabase Setup()
    {
        TestDatabase db = new();
        db.Table<CmcRow>().Schema.CreateTable();
        db.Table<CmcRow>().AddRange(Rows());
        return db;
    }

    [Fact]
    public void ClientCallOverConditionalMemberNullCheckMatchesLinq()
    {
        using TestDatabase db = Setup();

        List<string> expected = Rows().OrderBy(r => r.Id)
            .Select(r => new { r.Id, Part = r.Id > 1 ? new CmcPart { Label = r.Name } : null })
            .Select(x => CmcClientFns.Tag(x.Part == null ? "none" : x.Part.Label ?? "null"))
            .ToList();

        List<string> actual = db.Table<CmcRow>()
            .OrderBy(r => r.Id)
            .Select(r => new { r.Id, Part = r.Id > 1 ? new CmcPart { Label = r.Name } : null })
            .Select(x => CmcClientFns.Tag(x.Part == null ? "none" : x.Part.Label ?? "null"))
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ClientCallOverConditionalMemberNotNullCheckMatchesLinq()
    {
        using TestDatabase db = Setup();

        List<string> expected = Rows().OrderBy(r => r.Id)
            .Select(r => new { r.Id, Part = r.Id > 1 ? new CmcPart { Label = r.Name } : null })
            .Select(x => CmcClientFns.Tag(x.Part != null ? x.Part.Label ?? "null" : "none"))
            .ToList();

        List<string> actual = db.Table<CmcRow>()
            .OrderBy(r => r.Id)
            .Select(r => new { r.Id, Part = r.Id > 1 ? new CmcPart { Label = r.Name } : null })
            .Select(x => CmcClientFns.Tag(x.Part != null ? x.Part.Label ?? "null" : "none"))
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ClientCallOverReversedConditionalMemberMatchesLinq()
    {
        using TestDatabase db = Setup();

        List<string> expected = Rows().OrderBy(r => r.Id)
            .Select(r => new { r.Id, Part = r.Id > 1 ? null : new CmcPart { Label = r.Name } })
            .Select(x => CmcClientFns.Tag(null == x.Part ? "none" : x.Part.Label ?? "null"))
            .ToList();

        List<string> actual = db.Table<CmcRow>()
            .OrderBy(r => r.Id)
            .Select(r => new { r.Id, Part = r.Id > 1 ? null : new CmcPart { Label = r.Name } })
            .Select(x => CmcClientFns.Tag(null == x.Part ? "none" : x.Part.Label ?? "null"))
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ClientCallOverAlwaysBuiltMemberNullCheckMatchesLinq()
    {
        using TestDatabase db = Setup();

        List<string> expected = Rows().OrderBy(r => r.Id)
            .Select(r => new { r.Id, Part = new CmcPart { Label = r.Name } })
            .Select(x => CmcClientFns.Tag(x.Part == null ? "none" : x.Part.Label ?? "null"))
            .ToList();

        List<string> actual = db.Table<CmcRow>()
            .OrderBy(r => r.Id)
            .Select(r => new { r.Id, Part = new CmcPart { Label = r.Name } })
            .Select(x => CmcClientFns.Tag(x.Part == null ? "none" : x.Part.Label ?? "null"))
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ClientCallOverConditionalValueMemberDefaultMatchesLinq()
    {
        using TestDatabase db = Setup();

        List<int> expected = Rows().OrderBy(r => r.Id)
            .Select(r => new { r.Id, Part = r.Id > 1 ? new CmcPart { Num = r.Id * 10 } : null })
            .Select(x => CmcClientFns.Pass(x.Part == null ? -1 : x.Part.Num))
            .ToList();

        List<int> actual = db.Table<CmcRow>()
            .OrderBy(r => r.Id)
            .Select(r => new { r.Id, Part = r.Id > 1 ? new CmcPart { Num = r.Id * 10 } : null })
            .Select(x => CmcClientFns.Pass(x.Part == null ? -1 : x.Part.Num))
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ClientCallOverConditionalNullableMemberMatchesLinq()
    {
        using TestDatabase db = Setup();

        List<int> expected = Rows().OrderBy(r => r.Id)
            .Select(r => new { r.Id, Part = r.Id > 1 ? new CmcPart { MaybeNum = r.MaybeCount } : null })
            .Select(x => CmcClientFns.Pass(x.Part == null ? -1 : x.Part.MaybeNum ?? -2))
            .ToList();

        List<int> actual = db.Table<CmcRow>()
            .OrderBy(r => r.Id)
            .Select(r => new { r.Id, Part = r.Id > 1 ? new CmcPart { MaybeNum = r.MaybeCount } : null })
            .Select(x => CmcClientFns.Pass(x.Part == null ? -1 : x.Part.MaybeNum ?? -2))
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ClientCallOverBothBranchesConstructedMemberMatchesLinq()
    {
        using TestDatabase db = Setup();

        List<string> expected = Rows().OrderBy(r => r.Id)
            .Select(r => new { r.Id, Part = r.Id > 1 ? new CmcPart { Label = r.Name } : new CmcPart { Label = "z" } })
            .Select(x => CmcClientFns.Tag(x.Part == null ? "none" : x.Part.Label ?? "null"))
            .ToList();

        List<string> actual = db.Table<CmcRow>()
            .OrderBy(r => r.Id)
            .Select(r => new { r.Id, Part = r.Id > 1 ? new CmcPart { Label = r.Name } : new CmcPart { Label = "z" } })
            .Select(x => CmcClientFns.Tag(x.Part == null ? "none" : x.Part.Label ?? "null"))
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ClientCallOverTwoHopConditionalMemberMatchesLinq()
    {
        using TestDatabase db = Setup();

        List<string> expected = Rows().OrderBy(r => r.Id)
            .Select(r => new { r.Id, Wrap = r.Id > 1 ? new CmcWrap { Part = new CmcPart { Label = r.Name } } : null })
            .Select(x => CmcClientFns.Tag(x.Wrap == null ? "none" : x.Wrap.Part!.Label ?? "null"))
            .ToList();

        Func<List<string>> query = () => db.Table<CmcRow>()
            .OrderBy(r => r.Id)
            .Select(r => new { r.Id, Wrap = r.Id > 1 ? new CmcWrap { Part = new CmcPart { Label = r.Name } } : null })
            .Select(x => CmcClientFns.Tag(x.Wrap == null ? "none" : x.Wrap.Part!.Label ?? "null"))
            .ToList();

        if (db.Options.ReflectionFallbackDisabled)
        {
            InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() => query());
            Assert.StartsWith("Select projection fell back to runtime reflection but ReflectionFallbackDisabled is set.", ex.Message);
            return;
        }

        Assert.Equal(expected, query());
    }
}
