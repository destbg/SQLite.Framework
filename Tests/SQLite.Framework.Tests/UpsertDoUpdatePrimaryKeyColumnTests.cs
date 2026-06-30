using System.ComponentModel.DataAnnotations;
using SQLite.Framework.Attributes;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class UpsertDoUpdatePrimaryKeyColumnTests
{
    internal sealed class UpkRow
    {
        [Key]
        [AutoIncrement]
        public int Id { get; set; }

        [Indexed(IsUnique = true)]
        public string Code { get; set; } = "";

        public int Amount { get; set; }
    }

    private static TestDatabase Create()
    {
        TestDatabase db = new();
        db.Table<UpkRow>().Schema.CreateTable();
        db.Table<UpkRow>().Add(new UpkRow { Code = "a", Amount = 1 });
        return db;
    }

    [Fact]
    public void DoUpdateListingPrimaryKeyPreservesRowIdentity()
    {
        using TestDatabase db = Create();
        int originalId = db.Table<UpkRow>().Single().Id;

        db.Table<UpkRow>().Upsert(
            new UpkRow { Code = "a", Amount = 5 },
            c => c.OnConflict(b => b.Code).DoUpdate(b => b.Id, b => b.Amount));

        UpkRow row = db.Table<UpkRow>().Single();

        Assert.Equal(1, originalId);
        Assert.Equal(originalId, row.Id);
        Assert.Equal(5, row.Amount);
    }
}
