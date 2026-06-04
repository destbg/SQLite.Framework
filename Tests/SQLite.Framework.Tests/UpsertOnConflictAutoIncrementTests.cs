using System.ComponentModel.DataAnnotations;
using SQLite.Framework.Attributes;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

internal sealed class UpcRow
{
    [Key]
    [AutoIncrement]
    public int Id { get; set; }
    public int Value { get; set; }
}

public class UpsertOnConflictAutoIncrementTests
{
    private static TestDatabase SeedOne(out int id, int value = 10)
    {
        TestDatabase db = new();
        db.Table<UpcRow>().Schema.CreateTable();
        UpcRow row = new() { Value = value };
        db.Table<UpcRow>().Add(row);
        id = row.Id;
        return db;
    }

    [Fact]
    public void OnConflictId_DoNothing_KeepsExistingRow()
    {
        using TestDatabase db = SeedOne(out int id);
        db.Table<UpcRow>().Upsert(new UpcRow { Id = id, Value = 999 }, c => c.OnConflict(x => x.Id).DoNothing());

        Assert.Equal(1, db.Table<UpcRow>().Count());
        Assert.Equal(10, db.Table<UpcRow>().Single().Value);
    }

    [Fact]
    public void OnConflictId_DoUpdate_UpdatesExistingRow()
    {
        using TestDatabase db = SeedOne(out int id);
        db.Table<UpcRow>().Upsert(new UpcRow { Id = id, Value = 999 }, c => c.OnConflict(x => x.Id).DoUpdate(x => x.Value));

        Assert.Equal(1, db.Table<UpcRow>().Count());
        Assert.Equal(999, db.Table<UpcRow>().Single().Value);
    }

    [Fact]
    public void OnConflictId_DoUpdateAll_UpdatesExistingRow()
    {
        using TestDatabase db = SeedOne(out int id);
        db.Table<UpcRow>().Upsert(new UpcRow { Id = id, Value = 777 }, c => c.OnConflict(x => x.Id).DoUpdateAll());

        Assert.Equal(1, db.Table<UpcRow>().Count());
        Assert.Equal(777, db.Table<UpcRow>().Single().Value);
    }

    [Fact]
    public void OnConflictId_DoUpdateSet_AppliesExpression()
    {
        using TestDatabase db = SeedOne(out int id);
        db.Table<UpcRow>().Upsert(new UpcRow { Id = id, Value = 5 },
            c => c.OnConflict(x => x.Id).DoUpdate(set => set.Set(x => x.Value, (existing, excluded) => existing.Value + excluded.Value)));

        Assert.Equal(1, db.Table<UpcRow>().Count());
        Assert.Equal(15, db.Table<UpcRow>().Single().Value);
    }

    [Fact]
    public void OnConflictId_NonExistingId_InsertsAtExplicitId()
    {
        using TestDatabase db = new();
        db.Table<UpcRow>().Schema.CreateTable();
        db.Table<UpcRow>().Upsert(new UpcRow { Id = 5, Value = 7 }, c => c.OnConflict(x => x.Id).DoNothing());

        Assert.Equal(1, db.Table<UpcRow>().Count());
        UpcRow row = db.Table<UpcRow>().Single();
        Assert.Equal(5, row.Id);
        Assert.Equal(7, row.Value);
    }

    [Fact]
    public void OnConflictId_IdUnset_AutoAssignsAndInserts()
    {
        using TestDatabase db = new();
        db.Table<UpcRow>().Schema.CreateTable();
        db.Table<UpcRow>().Upsert(new UpcRow { Value = 7 }, c => c.OnConflict(x => x.Id).DoNothing());

        Assert.Equal(1, db.Table<UpcRow>().Count());
        UpcRow row = db.Table<UpcRow>().Single();
        Assert.True(row.Id > 0);
        Assert.Equal(7, row.Value);
    }

    [Fact]
    public void UpsertRange_OnConflictId_UpdatesAndInserts()
    {
        using TestDatabase db = new();
        db.Table<UpcRow>().Schema.CreateTable();
        db.Table<UpcRow>().Add(new UpcRow { Value = 10 });
        db.Table<UpcRow>().Add(new UpcRow { Value = 20 });

        db.Table<UpcRow>().UpsertRange(
            [new UpcRow { Id = 1, Value = 111 }, new UpcRow { Id = 3, Value = 333 }],
            c => c.OnConflict(x => x.Id).DoUpdate(x => x.Value));

        List<(int Id, int Value)> rows = db.Table<UpcRow>().OrderBy(x => x.Id).Select(x => new { x.Id, x.Value }).ToList()
            .Select(x => (x.Id, x.Value)).ToList();

        Assert.Equal([(1, 111), (2, 20), (3, 333)], rows);
    }

    [Fact]
    public void UpsertRange_UnsetIds_AutoAssignsDistinctKeys()
    {
        using TestDatabase db = new();
        db.Table<UpcRow>().Schema.CreateTable();

        db.Table<UpcRow>().UpsertRange(
            [new UpcRow { Value = 100 }, new UpcRow { Value = 200 }],
            c => c.OnConflict(x => x.Id).DoNothing());

        List<(int Id, int Value)> rows = db.Table<UpcRow>().OrderBy(x => x.Id).Select(x => new { x.Id, x.Value }).ToList()
            .Select(x => (x.Id, x.Value)).ToList();

        Assert.Equal([(1, 100), (2, 200)], rows);
    }

    [Fact]
    public void UpsertRange_MixedSetAndUnsetIds_UpdatesAndAutoAssigns()
    {
        using TestDatabase db = SeedOne(out int id);

        db.Table<UpcRow>().UpsertRange(
            [new UpcRow { Id = id, Value = 111 }, new UpcRow { Value = 222 }],
            c => c.OnConflict(x => x.Id).DoUpdate(x => x.Value));

        List<(int Id, int Value)> rows = db.Table<UpcRow>().OrderBy(x => x.Id).Select(x => new { x.Id, x.Value }).ToList()
            .Select(x => (x.Id, x.Value)).ToList();

        Assert.Equal([(1, 111), (2, 222)], rows);
    }
}
