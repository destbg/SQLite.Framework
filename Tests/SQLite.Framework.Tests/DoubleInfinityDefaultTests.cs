using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Attributes;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("InfinityDefaultRows")]
file sealed class InfinityDefaultRow
{
    [Key]
    [AutoIncrement]
    public int Id { get; set; }

    public required string Name { get; set; }

    [DefaultValue(double.PositiveInfinity)]
    public double PosInf { get; set; }

    [DefaultValue(double.NegativeInfinity)]
    public double NegInf { get; set; }
}

public class DoubleInfinityDefaultTests
{
    [Fact]
    public void InfinityDefaultsRoundTrip()
    {
        using TestDatabase db = new();
        db.Table<InfinityDefaultRow>().Schema.CreateTable();
        db.Table<InfinityDefaultRow>().Add(new InfinityDefaultRow { Name = "x" });

        InfinityDefaultRow stored = db.Table<InfinityDefaultRow>().Single();

        Assert.Equal(double.PositiveInfinity, stored.PosInf);
        Assert.Equal(double.NegativeInfinity, stored.NegInf);
    }
}
