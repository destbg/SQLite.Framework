using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Attributes;
using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("UpsertReturningRows")]
file sealed class UpsertReturningRow
{
    [Key]
    [AutoIncrement]
    public int Id { get; set; }

    [Indexed(IsUnique = true)]
    public required string Sku { get; set; }

    public required int Qty { get; set; }
}

public class ReturningUpsertAutoIncrementTests
{
    private static TestDatabase Seeded()
    {
        TestDatabase db = new();
        db.Table<UpsertReturningRow>().Schema.CreateTable();
        db.Table<UpsertReturningRow>().Add(new UpsertReturningRow { Sku = "A", Qty = 1 });
        db.Table<UpsertReturningRow>().Add(new UpsertReturningRow { Sku = "B", Qty = 1 });
        return db;
    }

    [Fact]
    public void ReturningUpsert_ResolvesToUpdate_MatchesPlainAndLeavesKeyUnchanged()
    {
        using TestDatabase plainDb = Seeded();
        UpsertReturningRow plain = new() { Id = 0, Sku = "A", Qty = 5 };
        plainDb.Table<UpsertReturningRow>().Upsert(plain, c => c.OnConflict(x => x.Sku).DoUpdate(x => x.Qty));

        using TestDatabase returningDb = Seeded();
        UpsertReturningRow returning = new() { Id = 0, Sku = "A", Qty = 5 };
        returningDb.Table<UpsertReturningRow>().Returning().Upsert(returning, c => c.OnConflict(x => x.Sku).DoUpdate(x => x.Qty));

        Assert.Equal(0, plain.Id);
        Assert.Equal(plain.Id, returning.Id);
    }

    [Fact]
    public void ReturningUpsert_ResolvesToInsert_BackfillsNewKeyLikePlain()
    {
        using TestDatabase plainDb = Seeded();
        UpsertReturningRow plain = new() { Id = 0, Sku = "C", Qty = 7 };
        plainDb.Table<UpsertReturningRow>().Upsert(plain, c => c.OnConflict(x => x.Sku).DoUpdate(x => x.Qty));

        using TestDatabase returningDb = Seeded();
        UpsertReturningRow returning = new() { Id = 0, Sku = "C", Qty = 7 };
        returningDb.Table<UpsertReturningRow>().Returning().Upsert(returning, c => c.OnConflict(x => x.Sku).DoUpdate(x => x.Qty));

        Assert.Equal(3, plain.Id);
        Assert.Equal(plain.Id, returning.Id);
    }

    [Fact]
    public void ReturningUpsert_ResolvesToUpdate_UpdatesStoredRowAndReturnsExistingRow()
    {
        using TestDatabase db = Seeded();

        UpsertReturningRow incoming = new() { Id = 0, Sku = "A", Qty = 99 };
        UpsertReturningRow? returned = db.Table<UpsertReturningRow>()
            .Returning()
            .Upsert(incoming, c => c.OnConflict(x => x.Sku).DoUpdate(x => x.Qty));

        Assert.NotNull(returned);
        Assert.Equal(1, returned.Id);
        Assert.Equal(99, returned.Qty);
        Assert.Equal(0, incoming.Id);

        UpsertReturningRow stored = db.Table<UpsertReturningRow>().Single(x => x.Sku == "A");
        Assert.Equal(99, stored.Qty);
    }

    [Fact]
    public void ReturningUpsertRange_MixedInsertAndUpdate_BackfillsOnlyInserts()
    {
        using TestDatabase db = Seeded();

        UpsertReturningRow update = new() { Id = 0, Sku = "A", Qty = 50 };
        UpsertReturningRow insert = new() { Id = 0, Sku = "Z", Qty = 60 };
        db.Table<UpsertReturningRow>().Returning().UpsertRange(
            new[] { update, insert },
            c => c.OnConflict(x => x.Sku).DoUpdate(x => x.Qty));

        int storedInsertId = db.Table<UpsertReturningRow>().Single(x => x.Sku == "Z").Id;
        int storedUpdateQty = db.Table<UpsertReturningRow>().Single(x => x.Sku == "A").Qty;

        Assert.Equal(0, update.Id);
        Assert.Equal(50, storedUpdateQty);
        Assert.NotEqual(0, insert.Id);
        Assert.Equal(storedInsertId, insert.Id);
    }
}
