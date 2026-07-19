using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("ClvRow")]
public class ClvRow
{
    [Key]
    public int Id { get; set; }

    public string? Name { get; set; }
}

public class ClvPart
{
    public int? MaybeNum { get; set; }

    public int Num { get; set; }

    public string? Label { get; set; }
}

public class ClientEvalLeafVariantTests
{
    private static List<ClvRow> Rows() =>
    [
        new ClvRow { Id = 1, Name = null },
        new ClvRow { Id = 2, Name = "n2" },
    ];

    private static TestDatabase Setup()
    {
        TestDatabase db = new();
        db.Table<ClvRow>().Schema.CreateTable();
        db.Table<ClvRow>().AddRange(Rows());
        return db;
    }

    [Fact]
    public void ClientCallOverLiftedConditionalMemberMatchesLinq()
    {
        using TestDatabase db = Setup();

        List<int> expected = Rows().OrderBy(r => r.Id)
            .Select(r => new { r.Id, Part = r.Id > 1 ? new ClvPart { MaybeNum = r.Id } : null })
            .Select(x => CmcClientFns.Pass(x.Part == null ? -1 : x.Part.MaybeNum ?? -2))
            .ToList();

        List<int> actual = db.Table<ClvRow>()
            .OrderBy(r => r.Id)
            .Select(r => new { r.Id, Part = r.Id > 1 ? new ClvPart { MaybeNum = r.Id } : null })
            .Select(x => CmcClientFns.Pass(x.Part == null ? -1 : x.Part.MaybeNum ?? -2))
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ClientCallOverSimpleMemberNullCheckMatchesLinq()
    {
        using TestDatabase db = Setup();

        List<string> expected = Rows().OrderBy(r => r.Id)
            .Select(x => CmcClientFns.Tag(null == x.Name ? "none" : x.Name))
            .ToList();

        List<string> actual = db.Table<ClvRow>()
            .OrderBy(r => r.Id)
            .Select(x => CmcClientFns.Tag(null == x.Name ? "none" : x.Name))
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ClientCallOverCapturedComputedMemberMatchesLinq()
    {
        using TestDatabase db = Setup();
        int baseline = 100;

        List<int> expected = Rows().OrderBy(r => r.Id)
            .Select(r => new { r.Id, Part = r.Id > 1 ? new ClvPart { Num = baseline + 1 } : null })
            .Select(x => CmcClientFns.Pass(x.Part == null ? -1 : x.Part.Num))
            .ToList();

        List<int> actual = db.Table<ClvRow>()
            .OrderBy(r => r.Id)
            .Select(r => new { r.Id, Part = r.Id > 1 ? new ClvPart { Num = baseline + 1 } : null })
            .Select(x => CmcClientFns.Pass(x.Part == null ? -1 : x.Part.Num))
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ChainedWholeRowNullCheckMatchesLinq()
    {
        using TestDatabase db = Setup();

        List<string> expected = Rows().OrderBy(r => r.Id)
            .Select(r => new { r.Id, r.Name })
            .Select(x => x == null ? "none" : x.Name ?? "null")
            .ToList();

        List<string> actual = db.Table<ClvRow>()
            .OrderBy(r => r.Id)
            .Select(r => new { r.Id, r.Name })
            .Select(x => x == null ? "none" : x.Name ?? "null")
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ReversedNamedConditionalMemberReadMatchesLinq()
    {
        using TestDatabase db = Setup();

        List<string> expected = Rows().OrderBy(r => r.Id)
            .Select(r => new { r.Id, Part = r.Id > 1 ? null : new ClvPart { Label = r.Name } })
            .Select(x => x.Part == null ? "none" : x.Part.Label ?? "null")
            .ToList();

        List<string> actual = db.Table<ClvRow>()
            .OrderBy(r => r.Id)
            .Select(r => new { r.Id, Part = r.Id > 1 ? null : new ClvPart { Label = r.Name } })
            .Select(x => x.Part == null ? "none" : x.Part.Label ?? "null")
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ProjectedBoundsArrayMemberWithColumnLengthMatchesLinq()
    {
        using TestDatabase db = Setup();

        List<int> expected = Rows().OrderBy(r => r.Id)
            .Select(r => new { r.Id, Arr = new string[r.Id] })
            .Select(x => x.Arr.Length)
            .ToList();

        var rows = db.Table<ClvRow>()
            .OrderBy(r => r.Id)
            .Select(r => new { r.Id, Arr = new string[r.Id] })
            .ToList();

        Assert.Equal(expected, rows.Select(x => x.Arr.Length).ToList());
    }

    [Fact]
    public void ClientCallResultMemberReadMatchesLinq()
    {
        using TestDatabase db = Setup();

        List<int> expected = Rows().OrderBy(r => r.Id)
            .Select(x => CmcClientFns.Tag(x.Name ?? "e").Length)
            .ToList();

        List<int> actual = db.Table<ClvRow>()
            .OrderBy(r => r.Id)
            .Select(x => CmcClientFns.Tag(x.Name ?? "e").Length)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ClientCallOverAnonymousBaseConditionalMemberMatchesLinq()
    {
        using TestDatabase db = Setup();

        List<string> expected = Rows().OrderBy(r => r.Id)
            .Select(r => new { r.Id, Wrap = new { L = CmcClientFns.Tag(r.Name ?? "e") } })
            .Select(x => CmcClientFns.Tag(x.Wrap.L))
            .ToList();

        List<string> actual = db.Table<ClvRow>()
            .OrderBy(r => r.Id)
            .Select(r => new { r.Id, Wrap = new { L = CmcClientFns.Tag(r.Name ?? "e") } })
            .Select(x => CmcClientFns.Tag(x.Wrap.L))
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void NegatedCapturedMemberInClientInitMatchesLinq()
    {
        using TestDatabase db = Setup();
        int baseline = 9;

        List<(int, string?)> expected = Rows().OrderBy(r => r.Id)
            .Select(r => new ClvPart { Num = -baseline, Label = CmcClientFns.Tag(r.Name ?? "e") })
            .Select(p => (p.Num, p.Label))
            .ToList();

        List<ClvPart> rows = db.Table<ClvRow>()
            .OrderBy(r => r.Id)
            .Select(r => new ClvPart { Num = -baseline, Label = CmcClientFns.Tag(r.Name ?? "e") })
            .ToList();

        Assert.Equal(expected, rows.Select(p => (p.Num, p.Label)).ToList());
    }

    [Fact]
    public void ChainedWholeDtoNullCheckMatchesLinq()
    {
        using TestDatabase db = Setup();

        List<string> expected = Rows().OrderBy(r => r.Id)
            .Select(r => new ClvPart { Label = r.Name })
            .Select(x => x == null ? "none" : x.Label ?? "null")
            .ToList();

        List<string> actual = db.Table<ClvRow>()
            .OrderBy(r => r.Id)
            .Select(r => new ClvPart { Label = r.Name })
            .Select(x => x == null ? "none" : x.Label ?? "null")
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ChainedWholeRowNullableMemberNullCheckMatchesLinq()
    {
        using TestDatabase db = Setup();

        List<string> expected = Rows().OrderBy(r => r.Id)
            .Select(r => new { r.Name })
            .Select(x => x == null ? "none" : x.Name ?? "null")
            .ToList();

        List<string> actual = db.Table<ClvRow>()
            .OrderBy(r => r.Id)
            .Select(r => new { r.Name })
            .Select(x => x == null ? "none" : x.Name ?? "null")
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ChainedMemberOverDtoNullCheckCollapsesAllNullRow()
    {
        using TestDatabase db = Setup();

        List<string> actual = db.Table<ClvRow>()
            .OrderBy(r => r.Id)
            .Select(r => new CmcWrap { Part = new CmcPart { Label = r.Name } })
            .Select(x => x.Part == null ? "none" : x.Part.Label ?? "null")
            .ToList();

        List<string> expected = db.Options.ReflectionFallbackDisabled ? ["null", "n2"] : ["none", "n2"];
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void GroupingPassedToClientMethodThrows()
    {
        using TestDatabase db = Setup();

        Func<List<int>> query = () => db.Table<ClvRow>()
            .GroupBy(r => r.Name)
            .Select(g => ClvClientFns.CountGroup(g))
            .ToList();

        NotSupportedException ex = Assert.Throws<NotSupportedException>(() => query());
        Assert.Equal(
            "Cannot pass `g` of type IGrouping`2 directly to a client-side method inside a Select projection. " +
            "IGrouping`2 has no parameterless constructor, so the framework cannot reconstruct it from the query result. " +
            "Project the members you need explicitly (e.g. `.Select(x => new { x.Id, x.Name })`) and call the method with those values, " +
            "or materialize first with `.ToListAsync()` and call the method client-side.",
            ex.Message);
    }
    [Fact]
    public void DelegateInvocationInsideConstructedMemberMatchesLinq()
    {
        using TestDatabase db = Setup();
        Func<string, string> wrap = v => "<" + v + ">";

        List<string> expected = Rows().OrderBy(r => r.Id)
            .Select(r => new { r.Id, Wrap = new { L = wrap(r.Name ?? "e") } })
            .Select(x => CmcClientFns.Tag(x.Wrap.L))
            .ToList();

        List<string> actual = db.Table<ClvRow>()
            .OrderBy(r => r.Id)
            .Select(r => new { r.Id, Wrap = new { L = wrap(r.Name ?? "e") } })
            .Select(x => CmcClientFns.Tag(x.Wrap.L))
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ComputedConstantConditionalMemberChainedReadThrows()
    {
        using TestDatabase db = Setup();
        int baseline = 9;

        Func<List<int>> query = () => db.Table<ClvRow>()
            .OrderBy(r => r.Id)
            .Select(r => new { r.Id, Part = r.Id > 1 ? new ClvPart { Num = -baseline } : null })
            .Select(x => CmcClientFns.Pass(x.Part == null ? -1 : x.Part.Num))
            .ToList();

        NotSupportedException ex = Assert.Throws<NotSupportedException>(() => query());
        string expectedMessage = db.Options.ReflectionFallbackDisabled
            ? "Type SQLite.Framework.Tests.ClvPart is not supported."
            : "The parameter expression 'x' is not supported.";
        Assert.Equal(expectedMessage, ex.Message);
    }

    [Fact]
    public void MixedBranchConditionalMemberNullCheckMatchesLinq()
    {
        using TestDatabase db = Setup();
        ClvPart fallback = new() { Label = "fb" };

        List<string> expected = Rows().OrderBy(r => r.Id)
            .Select(r => new { r.Id, Part = r.Id > 1 ? new ClvPart { Label = r.Name } : fallback })
            .Select(x => x.Part != null ? "some" : "none")
            .ToList();

        List<string> actual = db.Table<ClvRow>()
            .OrderBy(r => r.Id)
            .Select(r => new { r.Id, Part = r.Id > 1 ? new ClvPart { Label = r.Name } : fallback })
            .Select(x => x.Part != null ? "some" : "none")
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ChainedReadOfUnsetDtoMemberThrowsInitializeGuidance()
    {
        using TestDatabase db = Setup();

        NotSupportedException ex = Assert.Throws<NotSupportedException>(() => db.Table<ClvRow>()
            .OrderBy(r => r.Id)
            .Select(r => new ClvPart { Num = r.Id })
            .Select(x => x.Label)
            .ToList());

        Assert.Equal(
            "Chained Select cannot read 'Label': the inner projection did not initialize that member. " +
            "Include 'Label' in the inner projection or restructure the query.",
            ex.Message);
    }

    [Fact]
    public void ClientCallOverUnsetDtoMemberThrows()
    {
        using TestDatabase db = Setup();

        Func<List<string>> query = () => db.Table<ClvRow>()
            .OrderBy(r => r.Id)
            .Select(r => new ClvPart { Num = r.Id })
            .Select(x => CmcClientFns.Tag(x.Label ?? "u"))
            .ToList();

        if (db.Options.ReflectionFallbackDisabled)
        {
            Assert.Equal(["[u]", "[u]"], query());
            return;
        }

        NotSupportedException ex = Assert.Throws<NotSupportedException>(() => query());
        Assert.Equal("The parameter expression 'x' is not supported.", ex.Message);
    }

    [Fact]
    public void ChainedAnonymousMemberNullCheckMatchesLinq()
    {
        using TestDatabase db = Setup();

        List<string> expected = Rows().OrderBy(r => r.Id)
            .Select(r => new { r.Id, Part = new { L = r.Name } })
            .Select(x => x.Part == null ? "none" : "some")
            .ToList();

        List<string> actual = db.Table<ClvRow>()
            .OrderBy(r => r.Id)
            .Select(r => new { r.Id, Part = new { L = r.Name } })
            .Select(x => x.Part == null ? "none" : "some")
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ReversedMixedBranchConditionalMemberNullCheckMatchesLinq()
    {
        using TestDatabase db = Setup();
        ClvPart fallback = new() { Label = "fb" };

        List<string> expected = Rows().OrderBy(r => r.Id)
            .Select(r => new { r.Id, Part = r.Id > 1 ? fallback : new ClvPart { Label = r.Name } })
            .Select(x => x.Part != null ? "some" : "none")
            .ToList();

        List<string> actual = db.Table<ClvRow>()
            .OrderBy(r => r.Id)
            .Select(r => new { r.Id, Part = r.Id > 1 ? fallback : new ClvPart { Label = r.Name } })
            .Select(x => x.Part != null ? "some" : "none")
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void CapturedFallbackBranchMemberReadMatchesLinq()
    {
        using TestDatabase db = Setup();
        ClvPart fallback = new() { Num = 9, Label = "fb" };

        List<int> expected = Rows().OrderBy(r => r.Id)
            .Select(r => new { Part = r.Id > 1 ? new ClvPart { Num = r.Id } : fallback })
            .Select(x => x.Part.Num)
            .ToList();

        List<int> actual = db.Table<ClvRow>()
            .OrderBy(r => r.Id)
            .Select(r => new { Part = r.Id > 1 ? new ClvPart { Num = r.Id } : fallback })
            .Select(x => x.Part.Num)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void CapturedFallbackBranchReversedMemberReadMatchesLinq()
    {
        using TestDatabase db = Setup();
        ClvPart fallback = new() { Num = 9, Label = "fb" };

        List<string> expected = Rows().OrderBy(r => r.Id)
            .Select(r => new { Part = r.Id > 1 ? fallback : new ClvPart { Label = r.Name } })
            .Select(x => x.Part.Label ?? "empty")
            .ToList();

        List<string> actual = db.Table<ClvRow>()
            .OrderBy(r => r.Id)
            .Select(r => new { Part = r.Id > 1 ? fallback : new ClvPart { Label = r.Name } })
            .Select(x => x.Part.Label ?? "empty")
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ClientCallOverInnerLambdaMemberChainMatchesLinq()
    {
        using TestDatabase db = Setup();
        List<ClvPart> parts = [new ClvPart { Num = 1, Label = "aaa" }, new ClvPart { Num = 2, Label = "b" }];

        List<string> expected = Rows().OrderBy(r => r.Id)
            .Select(x => CmcClientFns.Tag(parts.First(c => c.Label!.Length > x.Id).Label!))
            .ToList();

        Func<List<string>> query = () => db.Table<ClvRow>()
            .OrderBy(r => r.Id)
            .Select(x => CmcClientFns.Tag(parts.First(c => c.Label!.Length > x.Id).Label!))
            .ToList();

        if (db.Options.ReflectionFallbackDisabled)
        {
            InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() => query());
            Assert.StartsWith("Select projection fell back to runtime reflection but ReflectionFallbackDisabled is set.", ex.Message);
            return;
        }

        Assert.Equal(expected, query());
    }

    [Fact]
    public void ClientCallOverClientBoundDtoMemberChainMatchesLinq()
    {
        using TestDatabase db = Setup();

        List<string> expected = Rows().OrderBy(r => r.Id)
            .Select(r => new { Part = new ClvPart { Num = r.Id, Label = CmcClientFns.Tag(r.Name ?? "x") } })
            .Select(y => CmcClientFns.Tag(y.Part.Label!))
            .ToList();

        Func<List<string>> query = () => db.Table<ClvRow>()
            .OrderBy(r => r.Id)
            .Select(r => new { Part = new ClvPart { Num = r.Id, Label = CmcClientFns.Tag(r.Name ?? "x") } })
            .Select(y => CmcClientFns.Tag(y.Part.Label!))
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

public static class ClvClientFns
{
    public static int CountGroup(IGrouping<string?, ClvRow> group)
    {
        return group.Count();
    }
}
