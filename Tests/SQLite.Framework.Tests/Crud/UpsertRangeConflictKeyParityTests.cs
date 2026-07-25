using System.ComponentModel.DataAnnotations;
using SQLite.Framework.Attributes;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

file sealed class UpsertKeyRow
{
    [Key]
    [AutoIncrement]
    public int Id { get; set; }

    public int Value { get; set; }
}

public class UpsertRangeConflictKeyParityTests
{
    [Fact]
    public void UpsertRange_ConflictingUpdate_DoesNotChangeIncomingKey()
    {
        using TestDatabase db = new();
        db.Table<UpsertKeyRow>().Schema.CreateTable();
        db.Table<UpsertKeyRow>().Add(new UpsertKeyRow { Value = 10 });
        db.Table<UpsertKeyRow>().Add(new UpsertKeyRow { Value = 20 });

        UpsertKeyRow incoming = new() { Id = 1, Value = 999 };
        db.Table<UpsertKeyRow>().UpsertRange([incoming], c => c.OnConflict(x => x.Id).DoUpdate(x => x.Value));

        Assert.Equal(1, incoming.Id);
        Assert.Equal(999, db.Table<UpsertKeyRow>().Single(r => r.Id == 1).Value);
    }
}
