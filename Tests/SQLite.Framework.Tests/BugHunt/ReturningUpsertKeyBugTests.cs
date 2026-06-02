using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Attributes;
using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests.BugHunt;

[Table("UpsertReturningHuntRows")]
file sealed class UpsertReturningHuntRow
{
    [Key]
    [AutoIncrement]
    public int Id { get; set; }

    [Indexed(IsUnique = true)]
    public required string Sku { get; set; }

    public required int Qty { get; set; }
}

public class ReturningUpsertKeyBugTests
{
    [Fact]
    public void PlainUpsert_ConflictResolvesToUpdate_LeavesIncomingAutoIncrementKeyUnchanged()
    {
        using TestDatabase db = new();
        db.Table<UpsertReturningHuntRow>().Schema.CreateTable();
        db.Table<UpsertReturningHuntRow>().Add(new UpsertReturningHuntRow { Sku = "A", Qty = 1 });
        db.Table<UpsertReturningHuntRow>().Add(new UpsertReturningHuntRow { Sku = "B", Qty = 1 });

        UpsertReturningHuntRow incoming = new() { Id = 0, Sku = "A", Qty = 5 };
        db.Table<UpsertReturningHuntRow>().Upsert(incoming, c => c.OnConflict(x => x.Sku).DoUpdate(x => x.Qty));

        Assert.Equal(0, incoming.Id);
    }

    [Fact]
    public void ReturningUpsert_ConflictResolvesToUpdate_LeavesIncomingAutoIncrementKeyUnchanged()
    {
        using TestDatabase db = new();
        db.Table<UpsertReturningHuntRow>().Schema.CreateTable();
        db.Table<UpsertReturningHuntRow>().Add(new UpsertReturningHuntRow { Sku = "A", Qty = 1 });
        db.Table<UpsertReturningHuntRow>().Add(new UpsertReturningHuntRow { Sku = "B", Qty = 1 });

        UpsertReturningHuntRow incoming = new() { Id = 0, Sku = "A", Qty = 5 };
        db.Table<UpsertReturningHuntRow>()
            .Returning()
            .Upsert(incoming, c => c.OnConflict(x => x.Sku).DoUpdate(x => x.Qty));

        Assert.Equal(0, incoming.Id);
    }
}
