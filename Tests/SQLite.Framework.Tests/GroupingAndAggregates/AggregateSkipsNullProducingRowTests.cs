using System.ComponentModel.DataAnnotations;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class AggregateSkipsNullProducingRowTests
{
    internal sealed class DivRow
    {
        [Key]
        public int Id { get; set; }

        public double A { get; set; }

        public double B { get; set; }
    }

    [Fact]
    public void AverageOverDivideByZeroRowSkipsThatRow()
    {
        using TestDatabase db = Seed();

        double inMemory = Rows().Average(x => x.A / x.B);
        Assert.True(double.IsPositiveInfinity(inMemory));

        double actual = db.Table<DivRow>().Average(x => x.A / x.B);

        Assert.Equal(5.0, actual);
    }

    [Fact]
    public void MaxOverDivideByZeroRowSkipsThatRow()
    {
        using TestDatabase db = Seed();

        double inMemory = Rows().Max(x => x.A / x.B);
        Assert.True(double.IsPositiveInfinity(inMemory));

        double actual = db.Table<DivRow>().Max(x => x.A / x.B);

        Assert.Equal(5.0, actual);
    }

    private static List<DivRow> Rows() =>
    [
        new() { Id = 1, A = 10.0, B = 2.0 },
        new() { Id = 2, A = 10.0, B = 0.0 },
    ];

    private static TestDatabase Seed()
    {
        TestDatabase db = new();
        db.Table<DivRow>().Schema.CreateTable();
        db.Table<DivRow>().AddRange(Rows());
        return db;
    }
}
