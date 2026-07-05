using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework;
using SQLite.Framework.Attributes;
using SQLite.Framework.Enums;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("HookOtherSeed")]
public class HookOtherSeedRow
{
    [Key]
    public int Id { get; set; }

    public string Name { get; set; } = "";
}

[Table("HookAutoItem")]
public class HookAutoItemRow
{
    [Key]
    [AutoIncrement]
    public int Id { get; set; }

    public string Name { get; set; } = "";
}

public class ReturningRerouteAutoIncrementWriteBackTests
{
    [Fact]
    public void ReturningAddRerouteToAddOrUpdateWritesKeyBack()
    {
        using TestDatabase db = new(b => b.OnAction((d, entity, action) =>
            entity is HookAutoItemRow && action == SQLiteAction.Add ? SQLiteAction.AddOrUpdate : action));
        db.Table<HookOtherSeedRow>().Schema.CreateTable();
        db.Table<HookAutoItemRow>().Schema.CreateTable();
        db.Table<HookOtherSeedRow>().Add(new HookOtherSeedRow { Id = 1, Name = "seed" });

        HookAutoItemRow item = new() { Name = "x" };
        db.Table<HookAutoItemRow>().Returning().Add(item);

        Assert.Equal(1, item.Id);
    }

    [Fact]
    public void PlainAddRerouteToAddOrUpdateWritesKeyBack()
    {
        using TestDatabase db = new(b => b.OnAction((d, entity, action) =>
            entity is HookAutoItemRow && action == SQLiteAction.Add ? SQLiteAction.AddOrUpdate : action));
        db.Table<HookOtherSeedRow>().Schema.CreateTable();
        db.Table<HookAutoItemRow>().Schema.CreateTable();
        db.Table<HookOtherSeedRow>().Add(new HookOtherSeedRow { Id = 1, Name = "seed" });

        HookAutoItemRow item = new() { Name = "x" };
        db.Table<HookAutoItemRow>().Add(item);

        Assert.Equal(1, item.Id);
    }
}
