using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework;
using SQLite.Framework.Enums;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("BigCounter")]
public class BigCounterRow
{
    [Key]
    public int Id { get; set; }

    public ulong Big { get; set; }
}

[Table("PricePairEntry")]
public class PricePairEntryRow
{
    [Key]
    public int Id { get; set; }

    public decimal Price { get; set; }

    public decimal Cost { get; set; }
}

public class FunctionUnsignedAndTextDecimalTests
{
    [Fact]
    public void BetweenOverAUlongColumnMatchesTheOperators()
    {
        using TestDatabase db = new();
        db.Table<BigCounterRow>().Schema.CreateTable();
        db.Table<BigCounterRow>().Add(new BigCounterRow { Id = 1, Big = (1UL << 63) + 5 });

        int viaOperators = db.Table<BigCounterRow>().Count(r => r.Big >= 2UL && r.Big <= ulong.MaxValue);
        int viaBetween = db.Table<BigCounterRow>().Count(r => SQLiteFunctions.Between(r.Big, 2UL, ulong.MaxValue));

        Assert.Equal(viaOperators, viaBetween);
    }

    [Fact]
    public void ScalarMaxOverAUlongColumnUsesUnsignedOrder()
    {
        using TestDatabase db = new();
        db.Table<BigCounterRow>().Schema.CreateTable();
        db.Table<BigCounterRow>().Add(new BigCounterRow { Id = 1, Big = 1UL << 63 });

        ulong actual = db.Table<BigCounterRow>().Select(r => SQLiteFunctions.Max(r.Big, 10UL)).First();

        Assert.Equal(Math.Max(1UL << 63, 10UL), actual);
    }

    [Fact]
    public void ScalarMinOverTextDecimalsComparesNumerically()
    {
        using TestDatabase db = new(b => b.UseDecimalStorage(DecimalStorageMode.Text));
        db.Table<PricePairEntryRow>().Schema.CreateTable();
        db.Table<PricePairEntryRow>().Add(new PricePairEntryRow { Id = 1, Price = 9.5m, Cost = 10m });

        decimal actual = db.Table<PricePairEntryRow>().Select(b => SQLiteFunctions.Min(b.Price, b.Cost)).First();

        Assert.Equal(9.5m, actual);
    }

    [Fact]
    public void NullifOverATextDecimalTreatsEqualValuesAsEqual()
    {
        using TestDatabase db = new(b => b.UseDecimalStorage(DecimalStorageMode.Text));
        db.Table<PricePairEntryRow>().Schema.CreateTable();
        db.Table<PricePairEntryRow>().Add(new PricePairEntryRow { Id = 1, Price = 10m, Cost = 0m });
        decimal? nothing = null;

        Assert.Equal(1, db.Table<PricePairEntryRow>().Count(b => (decimal?)SQLiteFunctions.Nullif(b.Price, 10m) == nothing));
    }
}
