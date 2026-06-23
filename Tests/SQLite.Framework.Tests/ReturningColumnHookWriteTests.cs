using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using SQLite.Framework.Enums;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

internal sealed class ReturningHookKeyedItem
{
    [Key]
    public int Id { get; set; }

    public string Name { get; set; } = "";
}

public class ReturningColumnHookWriteTests
{
    [Fact]
    public void ReturningUpdate_RunsColumnHook()
    {
        using ModelTestDatabase db = new(
            model => model.Entity<ReturningHookItem>().Column("CreatedAt", SQLiteColumnType.Integer, nullable: true),
            o => o.OnUpdate<ReturningHookItem>((_, _, columns) =>
            {
                columns["CreatedAt"] = 7L;
                return true;
            }));
        db.Schema.CreateTable<ReturningHookItem>();
        ReturningHookItem item = new() { Name = "a" };
        db.Table<ReturningHookItem>().Add(item);

        db.Table<ReturningHookItem>().Returning().Update(item);

        Assert.Equal(7L, db.ExecuteScalar<long>("SELECT \"CreatedAt\" FROM \"ReturningHookItem\""));
    }

    [Fact]
    public void ReturningUpdate_ColumnHookCancels_ReturnsDefault()
    {
        using ModelTestDatabase db = new(
            model => model.Entity<ReturningHookItem>().Column("CreatedAt", SQLiteColumnType.Integer, nullable: true),
            o => o.OnUpdate<ReturningHookItem>((_, _, _) => false));
        db.Schema.CreateTable<ReturningHookItem>();
        ReturningHookItem item = new() { Name = "a" };
        db.Table<ReturningHookItem>().Add(item);

        ReturningHookItem? result = db.Table<ReturningHookItem>().Returning().Update(item);

        Assert.Null(result);
    }

    [Fact]
    public void ReturningAddRange_RunsColumnHook()
    {
        using ModelTestDatabase db = new(
            model => model.Entity<ReturningHookItem>().Column("CreatedAt", SQLiteColumnType.Integer, nullable: true),
            o => o.OnAdd<ReturningHookItem>((_, _, columns) =>
            {
                columns["CreatedAt"] = 9L;
                return true;
            }));
        db.Schema.CreateTable<ReturningHookItem>();

        db.Table<ReturningHookItem>().Returning().AddRange(new List<ReturningHookItem>
        {
            new() { Name = "a" },
            new() { Name = "b" },
        });

        Assert.Equal(2L, db.ExecuteScalar<long>("SELECT COUNT(*) FROM \"ReturningHookItem\" WHERE \"CreatedAt\" = 9"));
    }

    [Fact]
    public void ReturningAdd_ColumnHookCancels_ReturnsDefault()
    {
        using ModelTestDatabase db = new(
            model => model.Entity<ReturningHookItem>().Column("CreatedAt", SQLiteColumnType.Integer, nullable: true),
            o => o.OnAdd<ReturningHookItem>((_, _, _) => false));
        db.Schema.CreateTable<ReturningHookItem>();

        ReturningHookItem? result = db.Table<ReturningHookItem>().Returning().Add(new ReturningHookItem { Name = "a" });

        Assert.Null(result);
        Assert.Equal(0L, db.ExecuteScalar<long>("SELECT COUNT(*) FROM \"ReturningHookItem\""));
    }

    [Fact]
    public void ReturningAdd_ColumnHookOnNonAutoIncrementTable_Works()
    {
        using ModelTestDatabase db = new(
            model => model.Entity<ReturningHookKeyedItem>().Column("CreatedAt", SQLiteColumnType.Integer, nullable: true),
            o => o.OnAdd<ReturningHookKeyedItem>((_, _, columns) =>
            {
                columns["CreatedAt"] = 5L;
                return true;
            }));
        db.Schema.CreateTable<ReturningHookKeyedItem>();

        db.Table<ReturningHookKeyedItem>().Returning().Add(new ReturningHookKeyedItem { Id = 1, Name = "a" });

        Assert.Equal(5L, db.ExecuteScalar<long>("SELECT \"CreatedAt\" FROM \"ReturningHookKeyedItem\""));
    }

    [Fact]
    public void ReturningAdd_ColumnHookWithActionSkip_DoesNotInsert()
    {
        using ModelTestDatabase db = new(
            model => model.Entity<ReturningHookItem>().Column("CreatedAt", SQLiteColumnType.Integer, nullable: true),
            o =>
            {
                o.OnAdd<ReturningHookItem>((_, _, columns) =>
                {
                    columns["CreatedAt"] = 1L;
                    return true;
                });
                o.OnAction((_, _, _) => SQLiteAction.Skip);
            });
        db.Schema.CreateTable<ReturningHookItem>();

        ReturningHookItem? result = db.Table<ReturningHookItem>().Returning().Add(new ReturningHookItem { Name = "a" });

        Assert.Null(result);
        Assert.Equal(0L, db.ExecuteScalar<long>("SELECT COUNT(*) FROM \"ReturningHookItem\""));
    }
}
