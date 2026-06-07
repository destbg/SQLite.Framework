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

        Assert.Equal("CREATE TABLE \"MbItem\" (\"Id\" INTEGER NOT NULL, \"Code\" INTEGER PRIMARY KEY, \"Name\" TEXT NOT NULL, \"Price\" REAL NOT NULL, \"Tax\" INTEGER NULL, \"Extra\" TEXT NULL)", TableSql(db, "MbItem"));
        Assert.Equal("CREATE TABLE \"MbItem\" (\"Id\" INTEGER NOT NULL, \"Code\" INTEGER PRIMARY KEY, \"Name\" TEXT NOT NULL, \"Price\" REAL NOT NULL, \"Tax\" INTEGER NULL, \"Extra\" TEXT NULL)", TableSql(db, "MbItem"));
    }

    [Fact]
    public void HasKey_Composite_SetsCompositePrimaryKey()
    {
        using ModelTestDatabase db = new(model => model.Entity<MbItem>().HasKey(m => new { m.Code, m.Id }));
        db.Schema.CreateTable<MbItem>();

        Assert.Equal("CREATE TABLE \"MbItem\" (\"Id\" INTEGER NOT NULL, \"Code\" INTEGER NOT NULL, \"Name\" TEXT NOT NULL, \"Price\" REAL NOT NULL, \"Tax\" INTEGER NULL, \"Extra\" TEXT NULL, PRIMARY KEY (\"Code\", \"Id\"))", TableSql(db, "MbItem"));
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

        Assert.Equal("CREATE TABLE \"MbItem\" (\"Id\" INTEGER PRIMARY KEY AUTOINCREMENT, \"Code\" INTEGER NOT NULL, \"Name\" TEXT NOT NULL, \"Price\" REAL NOT NULL, \"Tax\" INTEGER NULL, \"Extra\" TEXT NULL)", TableSql(db, "MbItem"));
    }

    [Fact]
    public void HasColumnName_RenamesColumnAndRoundTrips()
    {
        using ModelTestDatabase db = new(model => model.Entity<MbItem>().HasColumnName(m => m.Name, "item_name"));
        db.Schema.CreateTable<MbItem>();

        Assert.Equal("CREATE TABLE \"MbItem\" (\"Id\" INTEGER NOT NULL, \"Code\" INTEGER NOT NULL, \"item_name\" TEXT NOT NULL, \"Price\" REAL NOT NULL, \"Tax\" INTEGER NULL, \"Extra\" TEXT NULL)", TableSql(db, "MbItem"));
        Assert.Equal("CREATE TABLE \"MbItem\" (\"Id\" INTEGER NOT NULL, \"Code\" INTEGER NOT NULL, \"item_name\" TEXT NOT NULL, \"Price\" REAL NOT NULL, \"Tax\" INTEGER NULL, \"Extra\" TEXT NULL)", TableSql(db, "MbItem"));
        db.Table<MbItem>().Add(new MbItem { Code = 1, Name = "hello" });
        Assert.Equal("hello", db.Table<MbItem>().Single().Name);
    }

    [Fact]
    public void HasColumnType_OverridesType()
    {
        using ModelTestDatabase db = new(model => model.Entity<MbItem>().HasColumnType(m => m.Price, SQLiteColumnType.Text));
        db.Schema.CreateTable<MbItem>();

        Assert.Equal("CREATE TABLE \"MbItem\" (\"Id\" INTEGER NOT NULL, \"Code\" INTEGER NOT NULL, \"Name\" TEXT NOT NULL, \"Price\" TEXT NOT NULL, \"Tax\" INTEGER NULL, \"Extra\" TEXT NULL)", TableSql(db, "MbItem"));
    }

    [Fact]
    public void IsRequired_MakesColumnNotNull()
    {
        using ModelTestDatabase db = new(model => model.Entity<MbItem>().IsRequired(m => m.Tax));
        db.Schema.CreateTable<MbItem>();

        Assert.Equal("CREATE TABLE \"MbItem\" (\"Id\" INTEGER NOT NULL, \"Code\" INTEGER NOT NULL, \"Name\" TEXT NOT NULL, \"Price\" REAL NOT NULL, \"Tax\" INTEGER NOT NULL, \"Extra\" TEXT NULL)", TableSql(db, "MbItem"));
    }

    [Fact]
    public void IsRequired_False_MakesColumnNullable()
    {
        using ModelTestDatabase db = new(model => model.Entity<MbItem>().IsRequired(m => m.Name, required: false));
        db.Schema.CreateTable<MbItem>();

        Assert.Equal("CREATE TABLE \"MbItem\" (\"Id\" INTEGER NOT NULL, \"Code\" INTEGER NOT NULL, \"Name\" TEXT NULL, \"Price\" REAL NOT NULL, \"Tax\" INTEGER NULL, \"Extra\" TEXT NULL)", TableSql(db, "MbItem"));
    }

    [Fact]
    public void Ignore_DropsColumnFromModel()
    {
        using ModelTestDatabase db = new(model => model.Entity<MbItem>().Ignore(m => m.Extra));
        db.Schema.CreateTable<MbItem>();

        Assert.Equal("CREATE TABLE \"MbItem\" (\"Id\" INTEGER NOT NULL, \"Code\" INTEGER NOT NULL, \"Name\" TEXT NOT NULL, \"Price\" REAL NOT NULL, \"Tax\" INTEGER NULL)", TableSql(db, "MbItem"));
        db.Table<MbItem>().Add(new MbItem { Code = 1, Name = "a", Extra = "ignored" });
        Assert.Single(db.Table<MbItem>().ToList());
    }

    [Fact]
    public void WithoutRowId_EmitsClause()
    {
        using ModelTestDatabase db = new(model => model.Entity<MbItem>().HasKey(m => m.Code).WithoutRowId());
        db.Schema.CreateTable<MbItem>();

        Assert.Equal("CREATE TABLE \"MbItem\" (\"Id\" INTEGER NOT NULL, \"Code\" INTEGER PRIMARY KEY, \"Name\" TEXT NOT NULL, \"Price\" REAL NOT NULL, \"Tax\" INTEGER NULL, \"Extra\" TEXT NULL) WITHOUT ROWID", TableSql(db, "MbItem"));
    }

    [Fact]
    public void Strict_EmitsClause()
    {
        using ModelTestDatabase db = new(model => model.Entity<MbItem>().HasKey(m => m.Id).Strict());
        db.Schema.CreateTable<MbItem>();

        Assert.Equal("CREATE TABLE \"MbItem\" (\"Id\" INTEGER PRIMARY KEY, \"Code\" INTEGER NOT NULL, \"Name\" TEXT NOT NULL, \"Price\" REAL NOT NULL, \"Tax\" INTEGER NULL, \"Extra\" TEXT NULL) STRICT", TableSql(db, "MbItem"));
    }

    [Fact]
    public void ShadowColumn_Nullable_CreatedAndIgnoredByOrm()
    {
        using ModelTestDatabase db = new(model => model.Entity<MbItem>().Column("Blob", SQLiteColumnType.Text));
        db.Schema.CreateTable<MbItem>();

        Assert.Equal("CREATE TABLE \"MbItem\" (\"Id\" INTEGER NOT NULL, \"Code\" INTEGER NOT NULL, \"Name\" TEXT NOT NULL, \"Price\" REAL NOT NULL, \"Tax\" INTEGER NULL, \"Extra\" TEXT NULL, \"Blob\" TEXT)", TableSql(db, "MbItem"));
        db.Table<MbItem>().Add(new MbItem { Code = 1, Name = "a" });
        Assert.Single(db.Table<MbItem>().ToList());
        Assert.Null(db.ExecuteScalar<string>("SELECT \"Blob\" FROM \"MbItem\""));
    }

    [Fact]
    public void ShadowColumn_NotNullWithDefault_AppliesDefault()
    {
        using ModelTestDatabase db = new(model => model.Entity<MbItem>().Column("Version", SQLiteColumnType.Integer, nullable: false, defaultSql: "0"));
        db.Schema.CreateTable<MbItem>();

        Assert.Equal("CREATE TABLE \"MbItem\" (\"Id\" INTEGER NOT NULL, \"Code\" INTEGER NOT NULL, \"Name\" TEXT NOT NULL, \"Price\" REAL NOT NULL, \"Tax\" INTEGER NULL, \"Extra\" TEXT NULL, \"Version\" INTEGER NOT NULL DEFAULT 0)", TableSql(db, "MbItem"));
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
