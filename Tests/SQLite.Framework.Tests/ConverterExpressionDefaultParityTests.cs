using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("CwDefRows")]
public class CwDefRow
{
    [Key]
    public int Id { get; set; }

    public CwPoints Pts { get; set; }
}

public class ConverterExpressionDefaultParityTests
{
    [Fact]
    public void ExpressionDefaultAppliesConverterWriteWrap()
    {
        CwPoints five = new(5);
        using ModelTestDatabase db = new(
            mb => mb.Entity<CwDefRow>().Default(r => r.Pts, () => five),
            b => b.AddTypeConverter<CwPoints>(new CwPointsConverter()));
        db.Table<CwDefRow>().Schema.CreateTable();

        db.Table<CwDefRow>().Add(new CwDefRow { Id = 1 });

        int actual = db.Table<CwDefRow>().Select(r => r.Pts).Single().N;

        Assert.Equal(5, actual);
    }

    [Fact]
    public void ConstantDefaultAppliesConverterWriteWrap()
    {
        using ModelTestDatabase db = new(
            mb => mb.Entity<CwDefRow>().Default(r => r.Pts, new CwPoints(5)),
            b => b.AddTypeConverter<CwPoints>(new CwPointsConverter()));
        db.Table<CwDefRow>().Schema.CreateTable();

        db.Table<CwDefRow>().Add(new CwDefRow { Id = 1 });

        int actual = db.Table<CwDefRow>().Select(r => r.Pts).Single().N;

        Assert.Equal(5, actual);
    }

}
