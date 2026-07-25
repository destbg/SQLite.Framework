using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H21bCteCalcRow")]
public class H21bCteCalcRow
{
    [Key]
    public int Id { get; set; }

    public int A { get; set; }

    public int B { get; set; }
}

public class CteComputedMemberBesideClientMemberTests
{
    private static List<H21bCteCalcRow> Rows() =>
    [
        new H21bCteCalcRow { Id = 1, A = 10, B = 100 },
        new H21bCteCalcRow { Id = 2, A = 20, B = 200 },
        new H21bCteCalcRow { Id = 3, A = 30, B = 300 },
    ];

    private static TestDatabase Setup()
    {
        TestDatabase db = new();
        db.Table<H21bCteCalcRow>().Schema.CreateTable();
        db.Table<H21bCteCalcRow>().AddRange(Rows());
        return db;
    }

    [Fact]
    public void CteBodyComputedMemberBesideArrayMemberReadsComputedValue()
    {
        using TestDatabase db = Setup();

        List<int> expected = Rows()
            .Select(r => new { Sum = r.A + r.B, Arr = new[] { r.A, r.B } })
            .Select(x => x.Sum)
            .OrderBy(v => v)
            .ToList();

        List<int> actual = db.With(() => db.Table<H21bCteCalcRow>()
                .Select(r => new { Sum = r.A + r.B, Arr = new[] { r.A, r.B } }))
            .Select(x => x.Sum)
            .AsEnumerable()
            .OrderBy(v => v)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void CteBodyComputedMemberBesideArrayMemberFiltersOnComputedValue()
    {
        using TestDatabase db = Setup();

        List<int> expected = Rows()
            .Select(r => new { Sum = r.A + r.B, Arr = new[] { r.A, r.B } })
            .Where(x => x.Sum > 150)
            .Select(x => x.Sum)
            .OrderBy(v => v)
            .ToList();

        List<int> actual = db.With(() => db.Table<H21bCteCalcRow>()
                .Select(r => new { Sum = r.A + r.B, Arr = new[] { r.A, r.B } }))
            .Where(x => x.Sum > 150)
            .Select(x => x.Sum)
            .AsEnumerable()
            .OrderBy(v => v)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void CteBodyComputedMemberBesideBoundsArrayMemberReadsComputedValue()
    {
        using TestDatabase db = Setup();

        List<int> expected = Rows()
            .Select(r => new { Sum = r.A + r.B, Arr = new int[r.A] })
            .Select(x => x.Sum)
            .OrderBy(v => v)
            .ToList();

        List<int> actual = db.With(() => db.Table<H21bCteCalcRow>()
                .Select(r => new { Sum = r.A + r.B, Arr = new int[r.A] }))
            .Select(x => x.Sum)
            .AsEnumerable()
            .OrderBy(v => v)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void CteBodyComputedMemberBesideObjectArrayMemberReadsComputedValue()
    {
        using TestDatabase db = Setup();

        List<int> expected = Rows()
            .Select(r => new { Sum = r.A + r.B, Arr = new object[] { r.A, r.B } })
            .Select(x => x.Sum)
            .OrderBy(v => v)
            .ToList();

        List<int> actual = db.With(() => db.Table<H21bCteCalcRow>()
                .Select(r => new { Sum = r.A + r.B, Arr = new object[] { r.A, r.B } }))
            .Select(x => x.Sum)
            .AsEnumerable()
            .OrderBy(v => v)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void CteBodyComputedMemberBesideArrayMemberInNamedShapeReadsComputedValue()
    {
        using TestDatabase db = Setup();

        List<int> expected = Rows()
            .Select(r => new H21bCteCalcShape { Id = r.Id, Doubled = r.A * 2, Arr = [r.A, r.B] })
            .Select(x => x.Doubled)
            .OrderBy(v => v)
            .ToList();

        List<int> actual = db.With(() => db.Table<H21bCteCalcRow>()
                .Select(r => new H21bCteCalcShape { Id = r.Id, Doubled = r.A * 2, Arr = new[] { r.A, r.B } }))
            .Select(x => x.Doubled)
            .AsEnumerable()
            .OrderBy(v => v)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void CteBodyComputedMemberWithoutClientMemberReadsComputedValue()
    {
        using TestDatabase db = Setup();

        List<int> expected = Rows()
            .Select(r => new { Sum = r.A + r.B })
            .Select(x => x.Sum)
            .OrderBy(v => v)
            .ToList();

        List<int> actual = db.With(() => db.Table<H21bCteCalcRow>()
                .Select(r => new { Sum = r.A + r.B }))
            .Select(x => x.Sum)
            .AsEnumerable()
            .OrderBy(v => v)
            .ToList();

        Assert.Equal(expected, actual);
    }
}

public class H21bCteCalcShape
{
    public int Id { get; set; }

    public int Doubled { get; set; }

    public int[] Arr { get; set; } = [];
}
