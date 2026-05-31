using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Attributes;
using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("UpsertAutoRows")]
file sealed class UpsertAutoRow
{
    [Key]
    [AutoIncrement]
    public int Id { get; set; }
    [Indexed(IsUnique = true)]
    public required string Sku { get; set; }
    public required int Qty { get; set; }
}

public class UpsertAutoIncrementBugTests
{
    [Fact]
    public void UpsertConflictUpdateDoesNotMisassignAutoIncrementKey()
    {
        using TestDatabase db = new();
        db.Table<UpsertAutoRow>().Schema.CreateTable();
        db.Table<UpsertAutoRow>().Add(new UpsertAutoRow { Sku = "A", Qty = 1 });
        db.Table<UpsertAutoRow>().Add(new UpsertAutoRow { Sku = "B", Qty = 1 });

        UpsertAutoRow incoming = new() { Sku = "A", Qty = 5 };
        db.Table<UpsertAutoRow>().Upsert(incoming, c => c.OnConflict(x => x.Sku).DoUpdate(x => x.Qty));

        UpsertAutoRow merged = db.Table<UpsertAutoRow>().Single(x => x.Sku == "A");
        Assert.Equal(1, merged.Id);
        Assert.Equal(5, merged.Qty);
        Assert.NotEqual(2, incoming.Id);
    }
}
