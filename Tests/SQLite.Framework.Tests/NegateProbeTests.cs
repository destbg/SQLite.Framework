using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class NegateProbeTests
{
    [Fact]
    public void SingleNegateInt()
    {
        Run(q => q.Select(r => -r.I));
    }

    [Fact]
    public void DoubleNegateInt()
    {
        Run(q => q.Select(r => -(-r.I)));
    }

    [Fact]
    public void TripleNegateInt()
    {
        Run(q => q.Select(r => -(-(-r.I))));
    }

    [Fact]
    public void NegateInArithmetic()
    {
        Run(q => q.Select(r => -r.I + 5));
    }

    [Fact]
    public void SubtractOfNegate()
    {
        Run(q => q.Select(r => 5 - -r.I));
    }

    [Fact]
    public void NegateLong()
    {
        Run(q => q.Select(r => -r.L));
    }

    [Fact]
    public void NegateDouble()
    {
        Run(q => q.Select(r => -r.D));
    }

    [Fact]
    public void NegateDecimal()
    {
        Run(q => q.Select(r => -r.M));
    }

    [Fact]
    public void NegateNullableInt()
    {
        Run(q => q.Select(r => -r.NI));
    }

    [Fact]
    public void DoubleNegateNullableInt()
    {
        Run(q => q.Select(r => -(-r.NI)));
    }

    [Fact]
    public void NegateInWhere()
    {
        RunWhere(q => q.Where(r => -r.I < 0));
    }

    private static void Run<T>(Func<IQueryable<NegRow>, IQueryable<T>> project)
    {
        using TestDatabase db = new();
        db.Table<NegRow>().Schema.CreateTable();
        db.Table<NegRow>().AddRange(Data());

        List<T> oracle = project(Data().AsQueryable().OrderBy(r => r.Id)).ToList();
        List<T> actual = project(db.Table<NegRow>().OrderBy(r => r.Id)).ToList();

        Assert.Equal(oracle, actual);
    }

    private static void RunWhere(Func<IQueryable<NegRow>, IQueryable<NegRow>> filter)
    {
        using TestDatabase db = new();
        db.Table<NegRow>().Schema.CreateTable();
        db.Table<NegRow>().AddRange(Data());

        List<int> oracle = filter(Data().AsQueryable()).Select(r => r.Id).OrderBy(i => i).ToList();
        List<int> actual = filter(db.Table<NegRow>()).Select(r => r.Id).OrderBy(i => i).ToList();

        Assert.Equal(oracle, actual);
    }

    private static List<NegRow> Data()
    {
        return new List<NegRow>
        {
            new() { Id = 1, I = 5, L = 100, D = 2.5, M = 1.5m, NI = null },
            new() { Id = 2, I = -3, L = -7, D = -4.0, M = -2.25m, NI = 8 },
            new() { Id = 3, I = 0, L = 0, D = 0.0, M = 0m, NI = -2 },
        };
    }
}

public class NegRow
{
    [System.ComponentModel.DataAnnotations.Key] public int Id { get; set; }
    public int I { get; set; }
    public long L { get; set; }
    public double D { get; set; }
    public decimal M { get; set; }
    public int? NI { get; set; }
}
