using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework;
using SQLite.Framework.Attributes;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("HookDefaultInsert")]
public class HookDefaultInsertRow
{
    [Key]
    [AutoIncrement]
    public int Id { get; set; }

    public int Count { get; set; }
}

public class ColumnHookDatabaseDefaultInsertTests
{
    [Fact]
    public void AddAppliesDatabaseDefaultWhenHookSetsNoColumns()
    {
        using ModelTestDatabase db = new(
            m => m.Entity<HookDefaultInsertRow>().Default(r => r.Count, 5),
            o => o.OnAdd<HookDefaultInsertRow>((_, _, _) => true));
        db.Table<HookDefaultInsertRow>().Schema.CreateTable();

        HookDefaultInsertRow item = new();
        int affected = db.Table<HookDefaultInsertRow>().Add(item);

        Assert.Equal(1, affected);
        Assert.Equal(1, item.Id);
        Assert.Equal(5, db.ExecuteScalar<long>("SELECT \"Count\" FROM \"HookDefaultInsert\""));
    }

    [Fact]
    public void AddRangeAppliesDatabaseDefaultWhenHookSetsNoColumns()
    {
        using ModelTestDatabase db = new(
            m => m.Entity<HookDefaultInsertRow>().Default(r => r.Count, 5),
            o => o.OnAdd<HookDefaultInsertRow>((_, _, _) => true));
        db.Table<HookDefaultInsertRow>().Schema.CreateTable();

        int affected = db.Table<HookDefaultInsertRow>().AddRange([new HookDefaultInsertRow(), new HookDefaultInsertRow()]);

        Assert.Equal(2, affected);
        Assert.Equal(10, db.ExecuteScalar<long>("SELECT SUM(\"Count\") FROM \"HookDefaultInsert\""));
    }

    [Fact]
    public void ReturningAddAppliesDatabaseDefaultWhenHookSetsNoColumns()
    {
        using ModelTestDatabase db = new(
            m => m.Entity<HookDefaultInsertRow>().Default(r => r.Count, 5),
            o => o.OnAdd<HookDefaultInsertRow>((_, _, _) => true));
        db.Table<HookDefaultInsertRow>().Schema.CreateTable();

        HookDefaultInsertRow? returned = db.Table<HookDefaultInsertRow>().Returning().Add(new HookDefaultInsertRow());

        Assert.NotNull(returned);
        Assert.Equal(5, returned.Count);
    }
}
