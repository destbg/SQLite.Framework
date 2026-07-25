using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.CompilerServices;
using SQLite.Framework.Attributes;
using SQLite.Framework.Enums;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class HookColumnsTests
{
    private static ModelTestDatabase NewDatabase(Action<SQLiteOptionsBuilder> options, [CallerMemberName] string? methodName = null)
    {
        ModelTestDatabase db = new(
            model => model.Entity<HookItem>()
                .Column("CreatedAt", SQLiteColumnType.Integer, nullable: true)
                .Column("Tag", SQLiteColumnType.Text, nullable: true),
            options,
            methodName);
        db.Schema.CreateTable<HookItem>();
        return db;
    }

    [Fact]
    public void OnAdd_SetsShadowColumn_AndBackfillsAutoIncrement()
    {
        using ModelTestDatabase db = NewDatabase(o => o.OnAdd<HookItem>((_, item, columns) =>
        {
            columns["CreatedAt"] = 123L;
            return true;
        }));

        HookItem item = new() { Name = "a" };
        db.Table<HookItem>().Add(item);

        Assert.True(item.Id > 0);
        Assert.Equal(123L, db.ExecuteScalar<long>("SELECT \"CreatedAt\" FROM \"HookItem\""));
    }

    [Fact]
    public void OnAdd_SetsShadowColumn_NonAutoIncrement()
    {
        using ModelTestDatabase db = new(
            model => model.Entity<WcItem>().Column("Tag", SQLiteColumnType.Text, nullable: true),
            o => o.OnAdd<WcItem>((_, item, columns) =>
            {
                columns["Tag"] = "hooked";
                return true;
            }));
        db.Schema.CreateTable<WcItem>();

        db.Table<WcItem>().Add(new WcItem { Id = 1, Name = "a" });

        Assert.Equal("hooked", db.ExecuteScalar<string>("SELECT \"Tag\" FROM \"WcItem\""));
    }

    [Fact]
    public void OnUpdate_SetsShadowColumn()
    {
        using ModelTestDatabase db = NewDatabase(o => o.OnUpdate<HookItem>((_, item, columns) =>
        {
            columns["CreatedAt"] = 7L;
            return true;
        }));

        HookItem item = new() { Name = "a" };
        db.Table<HookItem>().Add(item);
        db.Table<HookItem>().Update(item);

        Assert.Equal(7L, db.ExecuteScalar<long>("SELECT \"CreatedAt\" FROM \"HookItem\""));
    }

    [Fact]
    public void OnAdd_AddRange_SetsShadowColumns()
    {
        using ModelTestDatabase db = NewDatabase(o => o.OnAdd<HookItem>((_, item, columns) =>
        {
            columns["CreatedAt"] = 5L;
            return true;
        }));

        db.Table<HookItem>().AddRange([new HookItem { Name = "a" }, new HookItem { Name = "b" }]);

        Assert.Equal(2, db.ExecuteScalar<long>("SELECT COUNT(*) FROM \"HookItem\" WHERE \"CreatedAt\" = 5"));
    }

    [Fact]
    public void OnAdd_AddRange_WithoutTransaction()
    {
        using ModelTestDatabase db = NewDatabase(o => o.OnAdd<HookItem>((_, item, columns) =>
        {
            columns["CreatedAt"] = 6L;
            return true;
        }));

        db.Table<HookItem>().AddRange([new HookItem { Name = "a" }, new HookItem { Name = "b" }], runInTransaction: false);

        Assert.Equal(2, db.ExecuteScalar<long>("SELECT COUNT(*) FROM \"HookItem\" WHERE \"CreatedAt\" = 6"));
    }

    [Fact]
    public void OnUpdate_UpdateRange_SetsShadowColumns()
    {
        using ModelTestDatabase db = NewDatabase(o => o.OnUpdate<HookItem>((_, item, columns) =>
        {
            columns["CreatedAt"] = 9L;
            return true;
        }));

        HookItem a = new() { Name = "a" };
        HookItem b = new() { Name = "b" };
        db.Table<HookItem>().AddRange([a, b]);
        db.Table<HookItem>().UpdateRange([a, b]);

        Assert.Equal(2, db.ExecuteScalar<long>("SELECT COUNT(*) FROM \"HookItem\" WHERE \"CreatedAt\" = 9"));
    }

    [Fact]
    public void OnAdd_OverridesMappedColumn()
    {
        using ModelTestDatabase db = NewDatabase(o => o.OnAdd<HookItem>((_, item, columns) =>
        {
            columns["Name"] = "forced";
            return true;
        }));

        db.Table<HookItem>().Add(new HookItem { Name = "ignored" });

        Assert.Equal("forced", db.Table<HookItem>().First().Name);
    }

    [Fact]
    public void OnUpdate_OverridesMappedColumn()
    {
        using ModelTestDatabase db = NewDatabase(o => o.OnUpdate<HookItem>((_, item, columns) =>
        {
            columns["Name"] = "forced";
            return true;
        }));

        HookItem item = new() { Name = "a" };
        db.Table<HookItem>().Add(item);
        item.Name = "b";
        db.Table<HookItem>().Update(item);

        Assert.Equal("forced", db.Table<HookItem>().First().Name);
    }

    [Fact]
    public void OnAdd_AlsoMutatesEntity()
    {
        using ModelTestDatabase db = NewDatabase(o => o.OnAdd<HookItem>((_, item, columns) =>
        {
            item.Name = item.Name.ToUpperInvariant();
            columns["Tag"] = "t";
            return true;
        }));

        HookItem item = new() { Name = "abc" };
        db.Table<HookItem>().Add(item);

        Assert.Equal("ABC", db.Table<HookItem>().First().Name);
        Assert.Equal("t", db.ExecuteScalar<string>("SELECT \"Tag\" FROM \"HookItem\""));
    }

    [Fact]
    public void OnAdd_HookCancels_SkipsInsert()
    {
        using ModelTestDatabase db = NewDatabase(o => o.OnAdd<HookItem>((_, item, columns) =>
        {
            columns["CreatedAt"] = 1L;
            return false;
        }));

        int affected = db.Table<HookItem>().Add(new HookItem { Name = "a" });

        Assert.Equal(0, affected);
        Assert.Equal(0, db.ExecuteScalar<long>("SELECT COUNT(*) FROM \"HookItem\""));
    }

    [Fact]
    public void OnAdd_OnActionSkip_SkipsInsert()
    {
        using ModelTestDatabase db = NewDatabase(o => o
            .OnAdd<HookItem>((_, item, columns) =>
            {
                columns["CreatedAt"] = 1L;
                return true;
            })
            .OnAction((_, _, _) => SQLiteAction.Skip));

        db.Table<HookItem>().Add(new HookItem { Name = "a" });

        Assert.Equal(0, db.ExecuteScalar<long>("SELECT COUNT(*) FROM \"HookItem\""));
    }

    [Fact]
    public void OnUpdate_OnActionSkip_SkipsUpdate()
    {
        using ModelTestDatabase db = NewDatabase(o => o
            .OnUpdate<HookItem>((_, item, columns) =>
            {
                columns["CreatedAt"] = 1L;
                return true;
            })
            .OnAction((_, _, action) => action == SQLiteAction.Update ? SQLiteAction.Skip : action));

        HookItem item = new() { Name = "a" };
        db.Table<HookItem>().Add(item);
        db.Table<HookItem>().Update(item);

        Assert.Null(db.ExecuteScalar<long?>("SELECT \"CreatedAt\" FROM \"HookItem\""));
    }

    [Fact]
    public void OnAdd_AddRange_OnActionSkip_SkipsAll()
    {
        using ModelTestDatabase db = NewDatabase(o => o
            .OnAdd<HookItem>((_, item, columns) =>
            {
                columns["CreatedAt"] = 1L;
                return true;
            })
            .OnAction((_, _, _) => SQLiteAction.Skip));

        db.Table<HookItem>().AddRange([new HookItem { Name = "a" }, new HookItem { Name = "b" }]);

        Assert.Equal(0, db.ExecuteScalar<long>("SELECT COUNT(*) FROM \"HookItem\""));
    }

    [Fact]
    public void OnUpdate_HookCancels_SkipsUpdate()
    {
        using ModelTestDatabase db = NewDatabase(o => o.OnUpdate<HookItem>((_, item, columns) =>
        {
            columns["CreatedAt"] = 1L;
            return false;
        }));

        HookItem item = new() { Name = "a" };
        db.Table<HookItem>().Add(item);
        int affected = db.Table<HookItem>().Update(item);

        Assert.Equal(0, affected);
        Assert.Null(db.ExecuteScalar<long?>("SELECT \"CreatedAt\" FROM \"HookItem\""));
    }

    [Fact]
    public void OnAdd_PreservesExplicitAutoIncrementKey()
    {
        using ModelTestDatabase db = NewDatabase(o => o
            .PreserveExplicitAutoIncrementKeys()
            .OnAdd<HookItem>((_, item, columns) =>
            {
                columns["CreatedAt"] = 1L;
                return true;
            }));

        HookItem assigned = new() { Name = "a" };
        HookItem explicitId = new() { Id = 50, Name = "b" };
        db.Table<HookItem>().Add(assigned);
        db.Table<HookItem>().Add(explicitId);

        Assert.True(assigned.Id > 0 && assigned.Id != 50);
        Assert.Equal(50, db.Table<HookItem>().First(x => x.Name == "b").Id);
    }

    [Fact]
    public void OnAdd_AddRange_CancelsOneItem()
    {
        using ModelTestDatabase db = NewDatabase(o => o.OnAdd<HookItem>((_, item, columns) =>
        {
            columns["CreatedAt"] = 1L;
            return item.Name != "skip";
        }));

        db.Table<HookItem>().AddRange([new HookItem { Name = "a" }, new HookItem { Name = "skip" }, new HookItem { Name = "b" }]);

        Assert.Equal(2, db.ExecuteScalar<long>("SELECT COUNT(*) FROM \"HookItem\""));
    }
}

[Table("HookItem")]
public class HookItem
{
    [Key]
    [AutoIncrement]
    public int Id { get; set; }
    public required string Name { get; set; }
}
