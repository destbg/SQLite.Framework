using System.ComponentModel.DataAnnotations;
using System.Text;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Flags]
internal enum CePerm { None = 0, Read = 1, Write = 2 }

internal sealed class CeRow
{
    [Key] public int Id { get; set; }
    public string Name { get; set; } = "";
    public int Value { get; set; }
    public double Price { get; set; }
    public DateTime Moment { get; set; }
    public CePerm Perm { get; set; }

    [System.ComponentModel.DataAnnotations.Schema.NotMapped]
    public string Tagged => Name + "#" + Value;
}

internal sealed class CeChild
{
    [Key] public int Id { get; set; }
    public int Fk { get; set; }
    public string Label { get; set; } = "";
}

internal sealed class CeJoinDto
{
    public string Combined { get; set; } = "";
    public string Formatted { get; set; } = "";
}

public class ClientEvalProjectionTests
{
    private static readonly CeRow[] Data =
    [
        new CeRow { Id = 1, Name = "café", Value = 9, Price = 1.55, Moment = new DateTime(2021, 5, 10, 14, 30, 45), Perm = CePerm.Read | CePerm.Write },
        new CeRow { Id = 2, Name = "abc", Value = 16, Price = 2.25, Moment = new DateTime(2020, 1, 2, 3, 4, 5), Perm = CePerm.Read },
    ];

    private static TestDatabase Db()
    {
        TestDatabase db = new();
        db.Table<CeRow>().Schema.CreateTable();
        db.Table<CeRow>().AddRange(Data);
        return db;
    }

    private static void Same<T>(Func<IEnumerable<CeRow>, IEnumerable<T>> oracle, Func<IQueryable<CeRow>, IQueryable<T>> sql)
    {
        using TestDatabase db = Db();
        List<T> expected = oracle(Data.OrderBy(x => x.Id)).ToList();
        List<T> actual = sql(db.Table<CeRow>().OrderBy(x => x.Id)).ToList();
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void StringNormalize_ClientEvaluates()
        => Same(q => q.Select(x => x.Name.Normalize(NormalizationForm.FormD)),
                q => q.Select(x => x.Name.Normalize(NormalizationForm.FormD)));

    [Fact]
    public void DateTimeToStringFormat_ClientEvaluates()
        => Same(q => q.Select(x => x.Moment.ToString("yyyy-MM-dd HH:mm")),
                q => q.Select(x => x.Moment.ToString("yyyy-MM-dd HH:mm")));

    [Fact]
    public void FlagsEnumToString_ClientEvaluates()
        => Same(q => q.Select(x => x.Perm.ToString()),
                q => q.Select(x => x.Perm.ToString()));

    [Fact]
    public void InterpolatedString_ClientEvaluates()
        => Same(q => q.Select(x => $"{x.Name}#{x.Value}"),
                q => q.Select(x => $"{x.Name}#{x.Value}"));

    [Fact]
    public void MixedAnonymous_SqlColumnAndClientMethod()
    {
        using TestDatabase db = Db();
        var expected = Data.OrderBy(x => x.Id).Select(x => new { x.Id, x.Value, N = x.Name.Normalize(NormalizationForm.FormD) }).ToList();
        var actual = db.Table<CeRow>().OrderBy(x => x.Id).Select(x => new { x.Id, x.Value, N = x.Name.Normalize(NormalizationForm.FormD) }).ToList();
        Assert.Equal(expected.Count, actual.Count);
        for (int i = 0; i < expected.Count; i++)
        {
            Assert.Equal(expected[i].Id, actual[i].Id);
            Assert.Equal(expected[i].Value, actual[i].Value);
            Assert.Equal(expected[i].N, actual[i].N);
        }
    }

    [Fact]
    public void MixedCompoundSqlMemberAndClientMember()
    {
        using TestDatabase db = Db();
        var expected = Data.OrderBy(x => x.Id).Select(x => new { C = x.Name + " " + x.Value, N = x.Name.Normalize(NormalizationForm.FormD) }).ToList();
        var actual = db.Table<CeRow>().OrderBy(x => x.Id).Select(x => new { C = x.Name + " " + x.Value, N = x.Name.Normalize(NormalizationForm.FormD) }).ToList();
        Assert.Equal(expected.Count, actual.Count);
        for (int i = 0; i < expected.Count; i++)
        {
            Assert.Equal(expected[i].C, actual[i].C);
            Assert.Equal(expected[i].N, actual[i].N);
        }
    }

    [Fact]
    public void ClientEval_NumericToStringWithFormat()
        => Same(q => q.Select(x => x.Value.ToString("X4")),
                q => q.Select(x => x.Value.ToString("X4")));

    [Fact]
    public void ClientEval_OverTranslatableSubexpression()
        => Same(q => q.Select(x => (x.Value * 2).ToString("X4")),
                q => q.Select(x => (x.Value * 2).ToString("X4")));

    [Fact]
    public void ClientEval_WithCapturedVariable()
    {
        string suffix = "!";
        Same(q => q.Select(x => x.Name.Normalize(NormalizationForm.FormD) + suffix),
             q => q.Select(x => x.Name.Normalize(NormalizationForm.FormD) + suffix));
    }

    [Fact]
    public void ChainedClientEval()
        => Same(q => q.Select(x => x.Name.Normalize(NormalizationForm.FormD).PadLeft(8, '.')),
                q => q.Select(x => x.Name.Normalize(NormalizationForm.FormD).PadLeft(8, '.')));

    [Fact]
    public void ClientEval_OnConcatReceiver()
        => Same(q => q.Select(x => (x.Name + "z").Normalize(NormalizationForm.FormD)),
                q => q.Select(x => (x.Name + "z").Normalize(NormalizationForm.FormD)));

    [Fact]
    public void ClientEval_OnMethodReceiver()
        => Same(q => q.Select(x => x.Name.Trim().Normalize(NormalizationForm.FormD)),
                q => q.Select(x => x.Name.Trim().Normalize(NormalizationForm.FormD)));

    [Fact]
    public void ClientEval_OnConditionalReceiver()
        => Same(q => q.Select(x => (x.Id > 0 ? x.Name : "y").Normalize(NormalizationForm.FormD)),
                q => q.Select(x => (x.Id > 0 ? x.Name : "y").Normalize(NormalizationForm.FormD)));

    [Fact]
    public void ClientEval_OnComputedMemberReceiver()
        => Same(q => q.Select(x => x.Moment.DayOfWeek.ToString().Normalize(NormalizationForm.FormD)),
                q => q.Select(x => x.Moment.DayOfWeek.ToString().Normalize(NormalizationForm.FormD)));

    [Fact]
    public void ClientEval_OnStaticConcatReceiver()
        => Same(q => q.Select(x => string.Concat(x.Name, "!").Normalize(NormalizationForm.FormD)),
                q => q.Select(x => string.Concat(x.Name, "!").Normalize(NormalizationForm.FormD)));

    [Fact]
    public void ClientEval_OnUnmappedComputedMemberReceiver()
        => Same(q => q.Select(x => x.Tagged.Normalize(NormalizationForm.FormD)),
                q => q.Select(x => x.Tagged.Normalize(NormalizationForm.FormD)));

    [Fact]
    public void ClientEval_OnChainedScalarColumnParameter()
        => Same(q => q.Select(x => x.Name).Select(s => s.Normalize(NormalizationForm.FormD)),
                q => q.Select(x => x.Name).Select(s => s.Normalize(NormalizationForm.FormD)));

    [Fact]
    public void Join_MemberInitProjection_WithClientMember()
    {
        using TestDatabase db = Db();
        db.Table<CeChild>().Schema.CreateTable();
        CeChild[] children =
        [
            new CeChild { Id = 1, Fk = 1, Label = "L" },
            new CeChild { Id = 2, Fk = 2, Label = "M" },
        ];
        db.Table<CeChild>().AddRange(children);

        List<CeJoinDto> expected = (
            from r in Data.OrderBy(x => x.Id)
            join c in children on r.Id equals c.Fk
            select new CeJoinDto { Combined = r.Name + "#" + c.Label, Formatted = r.Moment.ToString("yyyy") }).ToList();

        List<CeJoinDto> actual = (
            from r in db.Table<CeRow>().OrderBy(x => x.Id)
            join c in db.Table<CeChild>() on r.Id equals c.Fk
            select new CeJoinDto { Combined = r.Name + "#" + c.Label, Formatted = r.Moment.ToString("yyyy") }).ToList();

        Assert.Equal(expected.Count, actual.Count);
        for (int i = 0; i < expected.Count; i++)
        {
            Assert.Equal(expected[i].Combined, actual[i].Combined);
            Assert.Equal(expected[i].Formatted, actual[i].Formatted);
        }
    }

    [Fact]
    public void Negative_UntranslatableInWhere_Throws()
    {
        using TestDatabase db = Db();
        Assert.Throws<NotSupportedException>(() =>
            db.Table<CeRow>().Where(x => x.Name.Normalize(NormalizationForm.FormD) == "x").ToList());
    }

    [Fact]
    public void Negative_UntranslatableInOrderBy_Throws()
    {
        using TestDatabase db = Db();
        Assert.Throws<NotSupportedException>(() =>
            db.Table<CeRow>().OrderBy(x => x.Name.Normalize(NormalizationForm.FormD)).Select(x => x.Id).ToList());
    }

    [Fact]
    public void Negative_SetOperationOperandScalar_StillThrows()
    {
        using TestDatabase db = Db();
        Assert.Throws<NotSupportedException>(() =>
            db.Table<CeRow>().Select(x => x.Name)
                .Concat(db.Table<CeRow>().Select(x => x.Name.Normalize(NormalizationForm.FormD)))
                .ToList());
    }

    [Fact]
    public void Negative_SubqueryInternalScalar_StillThrows()
    {
        using TestDatabase db = Db();
        db.Table<CeChild>().Schema.CreateTable();
        db.Table<CeChild>().Add(new CeChild { Id = 1, Fk = 1, Label = "x" });

        Assert.Throws<NotSupportedException>(() =>
            db.Table<CeRow>()
                .Select(x => db.Table<CeChild>().Where(c => c.Fk == x.Id).Select(c => c.Label.Normalize(NormalizationForm.FormD)).First())
                .ToList());
    }
}
