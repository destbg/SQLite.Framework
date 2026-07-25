using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Enums;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class WithColumnsTests
{
    [Fact]
    public void WithColumns_Add_ShadowColumn_Literal()
    {
        using ModelTestDatabase db = new(m => m.Entity<WcItem>().Column("Tag", SQLiteColumnType.Text, nullable: true));
        db.Schema.CreateTable<WcItem>();

        db.Table<WcItem>()
            .WithColumns(c => c.Set(x => SQLiteColumn.Of<string>(x, "Tag"), "manual"))
            .Add(new WcItem { Id = 1, Name = "a" });

        Assert.Equal("manual", db.ExecuteScalar<string>("SELECT \"Tag\" FROM \"WcItem\""));
        Assert.Equal("a", db.Table<WcItem>().Single().Name);
    }

    [Fact]
    public void WithColumns_Add_ShadowColumn_Expression()
    {
        using ModelTestDatabase db = new(m => m.Entity<WcItem>().Column("Stamp", SQLiteColumnType.Integer, nullable: true));
        db.Schema.CreateTable<WcItem>();

        db.Table<WcItem>()
            .WithColumns(c => c.Set(x => SQLiteColumn.Of<long>(x, "Stamp"), _ => SQLiteFunctions.UnixEpoch()))
            .Add(new WcItem { Id = 1, Name = "a" });

        Assert.True(db.ExecuteScalar<long>("SELECT \"Stamp\" FROM \"WcItem\"") > 0);
    }

    [Fact]
    public void WithColumns_Update_ShadowColumn_FromRow()
    {
        using ModelTestDatabase db = new(m => m.Entity<WcItem>().Column("Stamp", SQLiteColumnType.Integer, nullable: true));
        db.Schema.CreateTable<WcItem>();
        WcItem item = new() { Id = 1, Name = "a", Version = 5 };
        db.Table<WcItem>().Add(item);

        db.Table<WcItem>()
            .WithColumns(c => c.Set(x => SQLiteColumn.Of<long>(x, "Stamp"), x => x.Version * 10))
            .Update(item);

        Assert.Equal(50, db.ExecuteScalar<long>("SELECT \"Stamp\" FROM \"WcItem\""));
    }

    [Fact]
    public void WithColumns_Update_OverridesMappedColumn()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<WcItem>();
        WcItem item = new() { Id = 1, Name = "a", Version = 1 };
        db.Table<WcItem>().Add(item);

        db.Table<WcItem>()
            .WithColumns(c => c.Set(x => x.Version, x => x.Version + 1))
            .Update(item);

        Assert.Equal(2, db.Table<WcItem>().Single().Version);
    }

    [Fact]
    public void WithColumns_Add_OverridesMappedColumn()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<WcItem>();

        db.Table<WcItem>()
            .WithColumns(c => c.Set(x => x.Name, "overridden"))
            .Add(new WcItem { Id = 1, Name = "ignored" });

        Assert.Equal("overridden", db.Table<WcItem>().Single().Name);
    }

    [Fact]
    public void WithColumns_AddRange_ShadowColumn()
    {
        using ModelTestDatabase db = new(m => m.Entity<WcItem>().Column("Tag", SQLiteColumnType.Text, nullable: true));
        db.Schema.CreateTable<WcItem>();

        db.Table<WcItem>()
            .WithColumns(c => c.Set(x => SQLiteColumn.Of<string>(x, "Tag"), "batch"))
            .AddRange([new WcItem { Id = 1, Name = "a" }, new WcItem { Id = 2, Name = "b" }]);

        Assert.Equal(2, db.ExecuteScalar<long>("SELECT COUNT(*) FROM \"WcItem\" WHERE \"Tag\" = 'batch'"));
    }

    [Fact]
    public void WithColumns_AddOrUpdate_ShadowColumn()
    {
        using ModelTestDatabase db = new(m => m.Entity<WcItem>().Column("Tag", SQLiteColumnType.Text, nullable: true));
        db.Schema.CreateTable<WcItem>();

        db.Table<WcItem>()
            .WithColumns(c => c.Set(x => SQLiteColumn.Of<string>(x, "Tag"), "ins"))
            .AddOrUpdate(new WcItem { Id = 1, Name = "a" });

        Assert.Equal("ins", db.ExecuteScalar<string>("SELECT \"Tag\" FROM \"WcItem\""));
    }

    [Fact]
    public void WithColumns_Upsert_ShadowColumn()
    {
        using ModelTestDatabase db = new(m => m.Entity<WcItem>().Column("Tag", SQLiteColumnType.Text, nullable: true));
        db.Schema.CreateTable<WcItem>();

        db.Table<WcItem>()
            .WithColumns(c => c.Set(x => SQLiteColumn.Of<string>(x, "Tag"), "up"))
            .Upsert(new WcItem { Id = 1, Name = "a" }, c => c.OnConflict(x => x.Id).DoNothing());

        Assert.Equal("up", db.ExecuteScalar<string>("SELECT \"Tag\" FROM \"WcItem\""));
    }

    [Fact]
    public void WithColumns_Upsert_OverridesMappedColumn()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<WcItem>();

        db.Table<WcItem>()
            .WithColumns(c => c.Set(x => x.Name, "forced"))
            .Upsert(new WcItem { Id = 1, Name = "ignored" }, c => c.OnConflict(x => x.Id).DoNothing());

        Assert.Equal("forced", db.Table<WcItem>().Single().Name);
    }

    [Fact]
    public void WithColumns_Returning_Add()
    {
        using ModelTestDatabase db = new(m => m.Entity<WcItem>().Column("Tag", SQLiteColumnType.Text, nullable: true));
        db.Schema.CreateTable<WcItem>();

        WcItem? added = db.Table<WcItem>()
            .WithColumns(c => c.Set(x => SQLiteColumn.Of<string>(x, "Tag"), "ret"))
            .Returning()
            .Add(new WcItem { Id = 1, Name = "a" });

        Assert.NotNull(added);
        Assert.Equal("a", added.Name);
        Assert.Equal("ret", db.ExecuteScalar<string>("SELECT \"Tag\" FROM \"WcItem\""));
    }
}

[Table("WcItem")]
public class WcItem
{
    [Key]
    public int Id { get; set; }
    public required string Name { get; set; }
    public int Version { get; set; }
}
