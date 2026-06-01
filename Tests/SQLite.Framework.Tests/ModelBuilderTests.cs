using System.ComponentModel.DataAnnotations;
using System.Runtime.CompilerServices;
using SQLite.Framework.Enums;
using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class ModelBuilderTests
{
    private static string? TableSql(TestDatabase db, string name) =>
        db.ExecuteScalar<string>($"SELECT sql FROM sqlite_master WHERE type = 'table' AND name = '{name}'");

    [Fact]
    public void ToTable_ChangesTableName()
    {
        using ModelTestDatabase db = new(model => model.Entity<MbItem>().ToTable("CustomItems"));
        db.Schema.CreateTable<MbItem>();

        Assert.NotNull(TableSql(db, "CustomItems"));
        db.Table<MbItem>().Add(new MbItem { Code = 1, Name = "a" });
        Assert.Single(db.Table<MbItem>().ToList());
    }

    [Fact]
    public void HasKey_Single_SetsPrimaryKey()
    {
        using ModelTestDatabase db = new(model => model.Entity<MbItem>().HasKey(m => m.Code));
        db.Schema.CreateTable<MbItem>();

        Assert.Contains("\"Code\" INTEGER", TableSql(db, "MbItem"));
        Assert.Contains("PRIMARY KEY", TableSql(db, "MbItem"));
    }

    [Fact]
    public void HasKey_Composite_SetsCompositePrimaryKey()
    {
        using ModelTestDatabase db = new(model => model.Entity<MbItem>().HasKey(m => new { m.Code, m.Id }));
        db.Schema.CreateTable<MbItem>();

        Assert.Contains("PRIMARY KEY (\"Code\", \"Id\")", TableSql(db, "MbItem"));
    }

    [Fact]
    public void HasKey_UnmappedProperty_Throws()
    {
        using ModelTestDatabase db = new(model => model.Entity<MbItem>().Ignore(m => m.Extra).HasKey(m => m.Extra));

        Assert.Throws<ArgumentException>(() => db.Schema.CreateTable<MbItem>());
    }

    [Fact]
    public void AutoIncrement_EmitsAutoincrement()
    {
        using ModelTestDatabase db = new(model => model.Entity<MbItem>().HasKey(m => m.Id).AutoIncrement(m => m.Id));
        db.Schema.CreateTable<MbItem>();

        Assert.Contains("AUTOINCREMENT", TableSql(db, "MbItem"));
    }

    [Fact]
    public void HasColumnName_RenamesColumnAndRoundTrips()
    {
        using ModelTestDatabase db = new(model => model.Entity<MbItem>().HasColumnName(m => m.Name, "item_name"));
        db.Schema.CreateTable<MbItem>();

        Assert.Contains("\"item_name\"", TableSql(db, "MbItem"));
        Assert.DoesNotContain("\"Name\"", TableSql(db, "MbItem"));
        db.Table<MbItem>().Add(new MbItem { Code = 1, Name = "hello" });
        Assert.Equal("hello", db.Table<MbItem>().Single().Name);
    }

    [Fact]
    public void HasColumnType_OverridesType()
    {
        using ModelTestDatabase db = new(model => model.Entity<MbItem>().HasColumnType(m => m.Price, SQLiteColumnType.Text));
        db.Schema.CreateTable<MbItem>();

        Assert.Contains("\"Price\" TEXT", TableSql(db, "MbItem"));
    }

    [Fact]
    public void IsRequired_MakesColumnNotNull()
    {
        using ModelTestDatabase db = new(model => model.Entity<MbItem>().IsRequired(m => m.Tax));
        db.Schema.CreateTable<MbItem>();

        Assert.Contains("\"Tax\" INTEGER NOT NULL", TableSql(db, "MbItem"));
    }

    [Fact]
    public void IsRequired_False_MakesColumnNullable()
    {
        using ModelTestDatabase db = new(model => model.Entity<MbItem>().IsRequired(m => m.Name, required: false));
        db.Schema.CreateTable<MbItem>();

        Assert.DoesNotContain("\"Name\" TEXT NOT NULL", TableSql(db, "MbItem"));
    }

    [Fact]
    public void Ignore_DropsColumnFromModel()
    {
        using ModelTestDatabase db = new(model => model.Entity<MbItem>().Ignore(m => m.Extra));
        db.Schema.CreateTable<MbItem>();

        Assert.DoesNotContain("\"Extra\"", TableSql(db, "MbItem"));
        db.Table<MbItem>().Add(new MbItem { Code = 1, Name = "a", Extra = "ignored" });
        Assert.Single(db.Table<MbItem>().ToList());
    }

    [Fact]
    public void WithoutRowId_EmitsClause()
    {
        using ModelTestDatabase db = new(model => model.Entity<MbItem>().HasKey(m => m.Code).WithoutRowId());
        db.Schema.CreateTable<MbItem>();

        Assert.Contains("WITHOUT ROWID", TableSql(db, "MbItem"));
    }

    [Fact]
    public void Strict_EmitsClause()
    {
        using ModelTestDatabase db = new(model => model.Entity<MbItem>().HasKey(m => m.Id).Strict());
        db.Schema.CreateTable<MbItem>();

        Assert.Contains("STRICT", TableSql(db, "MbItem"));
    }

    [Fact]
    public void ShadowColumn_Nullable_CreatedAndIgnoredByOrm()
    {
        using ModelTestDatabase db = new(model => model.Entity<MbItem>().Column("Blob", SQLiteColumnType.Text));
        db.Schema.CreateTable<MbItem>();

        Assert.Contains("\"Blob\" TEXT", TableSql(db, "MbItem"));
        db.Table<MbItem>().Add(new MbItem { Code = 1, Name = "a" });
        Assert.Single(db.Table<MbItem>().ToList());
        Assert.Null(db.ExecuteScalar<string>("SELECT \"Blob\" FROM \"MbItem\""));
    }

    [Fact]
    public void ShadowColumn_NotNullWithDefault_AppliesDefault()
    {
        using ModelTestDatabase db = new(model => model.Entity<MbItem>().Column("Version", SQLiteColumnType.Integer, nullable: false, defaultSql: "0"));
        db.Schema.CreateTable<MbItem>();

        Assert.Contains("\"Version\" INTEGER NOT NULL DEFAULT 0", TableSql(db, "MbItem"));
        db.Table<MbItem>().Add(new MbItem { Code = 1, Name = "a" });
        Assert.Equal(0, db.ExecuteScalar<long>("SELECT \"Version\" FROM \"MbItem\""));
    }

    [Fact]
    public void Trigger_EmptyBody_Throws()
    {
        using ModelTestDatabase db = new(model => model.Entity<MbItem>()
            .Trigger("trg_empty", SQLiteTriggerTiming.After, SQLiteTriggerEvent.Insert, t => { }));

        Assert.Throws<ArgumentException>(() => db.Schema.CreateTable<MbItem>());
    }

    [Fact]
    public void Trigger_IsCreatedAndFires()
    {
        using TriggerModelDatabase db = new();
        db.Schema.CreateTable<MbItem>();
        db.Schema.CreateTable<MbAudit>();

        Assert.NotNull(db.ExecuteScalar<string>("SELECT sql FROM sqlite_master WHERE type = 'trigger' AND name = 'trg_MbItem_ins'"));
        db.Execute("INSERT INTO \"MbItem\" (\"Id\", \"Code\", \"Name\", \"Price\") VALUES (7, 1, 'a', 0)");
        Assert.Equal(7, db.Table<MbAudit>().Single().ItemId);
    }
}

public sealed class TriggerModelDatabase : TestDatabase
{
    public TriggerModelDatabase([CallerMemberName] string? methodName = null)
        : base(methodName)
    {
    }

    protected override void OnModelCreating(SQLiteModelBuilder builder)
    {
        builder.Entity<MbAudit>().HasKey(a => a.Id);
        builder.Entity<MbItem>()
            .HasKey(m => m.Id)
            .Trigger("trg_MbItem_ins", SQLiteTriggerTiming.After, SQLiteTriggerEvent.Insert,
                t => t.Insert(Table<MbAudit>(), s => s.Set(a => a.ItemId, _ => t.New.Id)));
    }
}

public class MbItem
{
    public int Id { get; set; }
    public int Code { get; set; }
    public string Name { get; set; } = "";
    public double Price { get; set; }
    public int? Tax { get; set; }
    public string? Extra { get; set; }
}

public class MbAudit
{
    [Key]
    public int Id { get; set; }
    public int ItemId { get; set; }
}
