using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.CompilerServices;
using SQLite.Framework.Attributes;
using SQLite.Framework.Enums;
using SQLite.Framework.Exceptions;
using SQLite.Framework.Extensions;
using SQLite.Framework.Models;
using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class MigrateTests
{
    private static string? TableSql(TestDatabase db, string name) =>
        db.ExecuteScalar<string>($"SELECT sql FROM sqlite_master WHERE type = 'table' AND name = '{name}'");

    private static string? IndexSql(TestDatabase db, string name) =>
        db.ExecuteScalar<string>($"SELECT sql FROM sqlite_master WHERE type = 'index' AND name = '{name}'");

    private static string? TriggerSql(TestDatabase db, string name) =>
        db.ExecuteScalar<string>($"SELECT sql FROM sqlite_master WHERE type = 'trigger' AND name = '{name}'");

    [Theory]
    [InlineData(MigrateMode.InPlace)]
    [InlineData(MigrateMode.Rebuild)]
    public void Migrate_CreatesMissingTable(MigrateMode mode)
    {
        using TestDatabase db = new();

        db.Schema.Table<MigSimple>().Migrate(mode);

        Assert.True(db.Schema.TableExists<MigSimple>());
    }

    [Theory]
    [InlineData(MigrateMode.InPlace)]
    [InlineData(MigrateMode.Rebuild)]
    public void Migrate_NoDrift_LeavesTableAndData(MigrateMode mode)
    {
        using ModelTestDatabase db = new(model => model.Entity<MigIndexed>().Index(m => m.Name, name: "IX_MigIndexed_Name"));
        db.Schema.CreateTable<MigIndexed>();
        db.Table<MigIndexed>().Add(new MigIndexed { Id = 1, Name = "a", Age = 1 });
        string? before = TableSql(db, "MigIndexed");

        db.Table<MigIndexed>().Schema.Migrate(mode);

        Assert.Single(db.Table<MigIndexed>().ToList());
        MigrateModeExtensions.AssertSchemaEquivalent(before, TableSql(db, "MigIndexed"));
        Assert.NotNull(IndexSql(db, "IX_MigIndexed_Name"));
    }

    [Theory]
    [InlineData(MigrateMode.InPlace)]
    [InlineData(MigrateMode.Rebuild)]
    public void Migrate_AddsColumn_PreservesAllRows(MigrateMode mode)
    {
        using TestDatabase db = new();
        db.Execute("CREATE TABLE \"MigSimple\" (\"Id\" INTEGER PRIMARY KEY, \"Name\" TEXT NOT NULL)");
        db.Execute("INSERT INTO \"MigSimple\" (\"Id\", \"Name\") VALUES (1, 'a'), (2, 'b'), (3, 'c')");

        db.Schema.Table<MigSimple>().Migrate(mode);

        List<MigSimple> rows = db.Table<MigSimple>().OrderBy(m => m.Id).ToList();
        Assert.Equal(3, rows.Count);
        Assert.Equal(["a", "b", "c"], rows.Select(r => r.Name));
        Assert.All(rows, r => Assert.Null(r.Extra));
        MigrateModeExtensions.AssertSchemaEquivalent("CREATE TABLE \"MigSimple\" (\"Id\" INTEGER PRIMARY KEY, \"Name\" TEXT NOT NULL, \"Extra\" INTEGER NULL)", TableSql(db, "MigSimple"));
    }

    [Theory]
    [InlineData(MigrateMode.InPlace)]
    [InlineData(MigrateMode.Rebuild)]
    public void Migrate_TypeChange_PreservesData(MigrateMode mode)
    {
        using TestDatabase db = new();
        db.Execute("CREATE TABLE \"MigSimple\" (\"Id\" INTEGER PRIMARY KEY, \"Name\" TEXT NOT NULL, \"Extra\" TEXT)");
        db.Execute("INSERT INTO \"MigSimple\" (\"Id\", \"Name\", \"Extra\") VALUES (1, 'a', '5')");

        db.Schema.Table<MigSimple>().Migrate(mode);

        MigrateModeExtensions.AssertSchemaEquivalent("CREATE TABLE \"MigSimple\" (\"Id\" INTEGER PRIMARY KEY, \"Name\" TEXT NOT NULL, \"Extra\" INTEGER NULL)", TableSql(db, "MigSimple"));
        Assert.Equal(5, db.Table<MigSimple>().Single().Extra);
    }

    [Theory]
    [InlineData(MigrateMode.InPlace)]
    [InlineData(MigrateMode.Rebuild)]
    public void Migrate_RemovesColumn_PreservesOtherData(MigrateMode mode)
    {
        using TestDatabase db = new();
        db.Execute("CREATE TABLE \"MigSimple\" (\"Id\" INTEGER PRIMARY KEY, \"Name\" TEXT NOT NULL, \"Extra\" INTEGER, \"Old\" TEXT)");
        db.Execute("INSERT INTO \"MigSimple\" (\"Id\", \"Name\", \"Extra\", \"Old\") VALUES (1, 'a', 5, 'gone')");

        db.Schema.Table<MigSimple>().Migrate(mode);

        MigrateModeExtensions.AssertSchemaEquivalent("CREATE TABLE \"MigSimple\" (\"Id\" INTEGER PRIMARY KEY, \"Name\" TEXT NOT NULL, \"Extra\" INTEGER NULL)", TableSql(db, "MigSimple"));
        MigSimple row = db.Table<MigSimple>().Single();
        Assert.Equal("a", row.Name);
        Assert.Equal(5, row.Extra);
    }

    [Theory]
    [InlineData(MigrateMode.InPlace)]
    [InlineData(MigrateMode.Rebuild)]
    public void Migrate_Computed_RebuildsAndComputes(MigrateMode mode)
    {
        using ModelTestDatabase db = new(model => model.Entity<MigComputed>().Computed(m => m.Total, m => m.Price * m.Quantity));
        db.Execute("CREATE TABLE \"MigComputed\" (\"Id\" INTEGER PRIMARY KEY, \"Price\" REAL NOT NULL, \"Quantity\" INTEGER NOT NULL, \"Total\" REAL NOT NULL)");

        db.Table<MigComputed>().Schema.Migrate(mode);

        MigrateModeExtensions.AssertSchemaEquivalent("CREATE TABLE \"MigComputed\" (\"Id\" INTEGER PRIMARY KEY, \"Price\" REAL NOT NULL, \"Quantity\" INTEGER NOT NULL, \"Total\" REAL GENERATED ALWAYS AS ((\"Price\" * CAST(\"Quantity\" AS REAL))) VIRTUAL)", TableSql(db, "MigComputed"));
        db.Execute("INSERT INTO \"MigComputed\" (\"Id\", \"Price\", \"Quantity\") VALUES (1, 2, 3)");
        Assert.Equal(6, db.Table<MigComputed>().Single().Total);
    }

    [Theory]
    [InlineData(MigrateMode.InPlace)]
    [InlineData(MigrateMode.Rebuild)]
    public void Migrate_Check_RebuildsAndEnforces(MigrateMode mode)
    {
        using ModelTestDatabase db = new(model => model.Entity<MigSimple>().Check(m => m.Extra == null || m.Extra >= 0, name: "CK_MigSimple_Extra"));
        db.Schema.CreateTable<MigSimple>();
        db.Execute("DELETE FROM \"MigSimple\"");

        db.Execute("DROP TABLE \"MigSimple\"");
        db.Execute("CREATE TABLE \"MigSimple\" (\"Id\" INTEGER PRIMARY KEY, \"Name\" TEXT NOT NULL, \"Extra\" INTEGER)");

        db.Table<MigSimple>().Schema.Migrate(mode);

        MigrateModeExtensions.AssertSchemaEquivalent("CREATE TABLE \"MigSimple\" (\"Id\" INTEGER PRIMARY KEY, \"Name\" TEXT NOT NULL, \"Extra\" INTEGER NULL, CONSTRAINT \"CK_MigSimple_Extra\" CHECK (\"Extra\" IS NULL OR COALESCE(\"Extra\" >= 0, 0)))", TableSql(db, "MigSimple"));
        Assert.Throws<SQLiteException>(() =>
            db.Table<MigSimple>().Add(new MigSimple { Id = 1, Name = "a", Extra = -5 }));
    }

    [Theory]
    [InlineData(MigrateMode.InPlace)]
    [InlineData(MigrateMode.Rebuild)]
    public void Migrate_Strict_Rebuilds(MigrateMode mode)
    {
        using ModelTestDatabase db = new(model => model.Entity<MigSimple>().Strict());
        db.Execute("CREATE TABLE \"MigSimple\" (\"Id\" INTEGER PRIMARY KEY, \"Name\" TEXT NOT NULL, \"Extra\" INTEGER)");

        db.Table<MigSimple>().Schema.Migrate(mode);

        MigrateModeExtensions.AssertSchemaEquivalent("CREATE TABLE \"MigSimple\" (\"Id\" INTEGER PRIMARY KEY, \"Name\" TEXT NOT NULL, \"Extra\" INTEGER NULL) STRICT", TableSql(db, "MigSimple"));
    }

    [Theory]
    [InlineData(MigrateMode.InPlace)]
    [InlineData(MigrateMode.Rebuild)]
    public void Migrate_Default_RebuildsAndApplies(MigrateMode mode)
    {
        using ModelTestDatabase db = new(model => model.Entity<MigSimple>().Default(m => m.Extra, 7));
        db.Execute("CREATE TABLE \"MigSimple\" (\"Id\" INTEGER PRIMARY KEY, \"Name\" TEXT NOT NULL, \"Extra\" INTEGER)");

        db.Table<MigSimple>().Schema.Migrate(mode);

        MigrateModeExtensions.AssertSchemaEquivalent("CREATE TABLE \"MigSimple\" (\"Id\" INTEGER PRIMARY KEY, \"Name\" TEXT NOT NULL, \"Extra\" INTEGER NULL DEFAULT 7)", TableSql(db, "MigSimple"));
        db.Execute("INSERT INTO \"MigSimple\" (\"Id\", \"Name\") VALUES (1, 'a')");
        Assert.Equal(7, db.Table<MigSimple>().Single().Extra);
    }

    [Theory]
    [InlineData(MigrateMode.InPlace)]
    [InlineData(MigrateMode.Rebuild)]
    public void Migrate_IndexBodyChange_Recreates(MigrateMode mode)
    {
        using ModelTestDatabase db = new(model => model.Entity<MigIndexed>().Index(m => m.Age, name: "IX_Mig"));
        db.Execute("CREATE TABLE \"MigIndexed\" (\"Id\" INTEGER PRIMARY KEY, \"Name\" TEXT NOT NULL, \"Age\" INTEGER NOT NULL)");
        db.Execute("CREATE INDEX \"IX_Mig\" ON \"MigIndexed\" (\"Name\")");

        db.Table<MigIndexed>().Schema.Migrate(mode);

        Assert.Equal("CREATE INDEX \"IX_Mig\" ON \"MigIndexed\" (\"Age\")", IndexSql(db, "IX_Mig"));
        Assert.Equal("CREATE INDEX \"IX_Mig\" ON \"MigIndexed\" (\"Age\")", IndexSql(db, "IX_Mig"));
    }

    [Theory]
    [InlineData(MigrateMode.InPlace)]
    [InlineData(MigrateMode.Rebuild)]
    public void Migrate_DropsUndeclaredIndex(MigrateMode mode)
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<MigIndexed>();
        db.Execute("CREATE INDEX \"IX_Stale\" ON \"MigIndexed\" (\"Name\")");
        Assert.NotNull(IndexSql(db, "IX_Stale"));

        db.Table<MigIndexed>().Schema.Migrate(mode);

        Assert.Null(IndexSql(db, "IX_Stale"));
    }

    [Theory]
    [InlineData(MigrateMode.InPlace)]
    [InlineData(MigrateMode.Rebuild)]
    public void Migrate_UniqueIndex_StillEnforcedAfterRebuild(MigrateMode mode)
    {
        using ModelTestDatabase db = new(model => model.Entity<MigIndexed>()
            .Index(m => m.Name, unique: true, name: "UX_Name")
            .Check(m => m.Age >= 0, name: "CK_Age"));
        db.Execute("CREATE TABLE \"MigIndexed\" (\"Id\" INTEGER PRIMARY KEY, \"Name\" TEXT NOT NULL, \"Age\" INTEGER NOT NULL)");
        db.Execute("CREATE UNIQUE INDEX \"UX_Name\" ON \"MigIndexed\" (\"Name\")");
        db.Execute("INSERT INTO \"MigIndexed\" (\"Id\", \"Name\", \"Age\") VALUES (1, 'a', 1)");

        db.Table<MigIndexed>().Schema.Migrate(mode);

        Assert.Single(db.Table<MigIndexed>().ToList());
        Assert.Throws<SQLiteException>(() =>
            db.Table<MigIndexed>().Add(new MigIndexed { Id = 2, Name = "a", Age = 2 }));
    }

    [Theory]
    [InlineData(MigrateMode.InPlace)]
    [InlineData(MigrateMode.Rebuild)]
    public void Migrate_ForeignKeyChild_PreservesDataAndConstraint(MigrateMode mode)
    {
        using TestDatabase db = new();
        db.Execute("PRAGMA foreign_keys = ON");
        db.Execute("CREATE TABLE \"MigParent\" (\"Id\" INTEGER PRIMARY KEY, \"Name\" TEXT NOT NULL)");
        db.Execute("CREATE TABLE \"MigChild\" (\"Id\" INTEGER PRIMARY KEY, \"ParentId\" INTEGER NOT NULL)");
        db.Execute("INSERT INTO \"MigParent\" (\"Id\", \"Name\") VALUES (1, 'p')");
        db.Execute("INSERT INTO \"MigChild\" (\"Id\", \"ParentId\") VALUES (1, 1)");

        db.Schema.Table<MigChild>().Migrate(mode);

        Assert.Equal(1, db.Table<MigChild>().Single().ParentId);
        MigrateModeExtensions.AssertSchemaEquivalent("CREATE TABLE \"MigChild\" (\"Id\" INTEGER PRIMARY KEY, \"ParentId\" INTEGER NOT NULL REFERENCES \"MigParent\"(\"Id\"))", TableSql(db, "MigChild"));
        Assert.Throws<SQLiteException>(() => db.Table<MigChild>().Add(new MigChild { Id = 2, ParentId = 999 }));
    }

    [Theory]
    [InlineData(MigrateMode.InPlace)]
    [InlineData(MigrateMode.Rebuild)]
    public void Migrate_CompositePrimaryKey_PreservesData(MigrateMode mode)
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<MigComposite>();
        db.Table<MigComposite>().Add(new MigComposite { A = 1, B = 2, V = 10 });
        db.Table<MigComposite>().Add(new MigComposite { A = 1, B = 3, V = 20 });
        db.Execute("CREATE INDEX \"IX_drop\" ON \"MigComposite\" (\"V\")");

        db.Table<MigComposite>().Schema.Migrate(mode);

        Assert.Null(IndexSql(db, "IX_drop"));
        List<MigComposite> rows = db.Table<MigComposite>().OrderBy(c => c.B).ToList();
        Assert.Equal([10, 20], rows.Select(r => r.V));
    }

    [Theory]
    [InlineData(MigrateMode.InPlace)]
    [InlineData(MigrateMode.Rebuild)]
    public void Migrate_WithoutRowId_PreservesData(MigrateMode mode)
    {
        using TestDatabase db = new();
        db.Execute("CREATE TABLE \"MigWor\" (\"Key\" TEXT NOT NULL PRIMARY KEY, \"Val\" INTEGER NOT NULL, \"Note\" TEXT) WITHOUT ROWID");
        db.Execute("INSERT INTO \"MigWor\" (\"Key\", \"Val\") VALUES ('a', 1), ('b', 2)");

        db.Table<MigWor>().Schema.Migrate(mode);

        MigrateModeExtensions.AssertSchemaEquivalent("CREATE TABLE \"MigWor\" (\"Key\" TEXT NOT NULL PRIMARY KEY, \"Val\" INTEGER NOT NULL, \"Note\" TEXT NULL) WITHOUT ROWID", TableSql(db, "MigWor"));
        List<MigWor> rows = db.Table<MigWor>().OrderBy(w => w.Key).ToList();
        Assert.Equal(["a", "b"], rows.Select(r => r.Key));
    }

    [Theory]
    [InlineData(MigrateMode.InPlace)]
    [InlineData(MigrateMode.Rebuild)]
    public void Migrate_NoCommonColumns_SkipsCopy(MigrateMode mode)
    {
        using TestDatabase db = new();
        db.Execute("CREATE TABLE \"MigNoCommon\" (\"Other\" TEXT)");

        db.Schema.Table<MigNoCommon>().Migrate(mode);

        MigrateModeExtensions.AssertSchemaEquivalent("CREATE TABLE \"MigNoCommon\" (\"Id\" INTEGER PRIMARY KEY, \"Value\" INTEGER NOT NULL)", TableSql(db, "MigNoCommon"));
        MigrateModeExtensions.AssertSchemaEquivalent("CREATE TABLE \"MigNoCommon\" (\"Id\" INTEGER PRIMARY KEY, \"Value\" INTEGER NOT NULL)", TableSql(db, "MigNoCommon"));
    }

    [Theory]
    [InlineData(MigrateMode.InPlace)]
    [InlineData(MigrateMode.Rebuild)]
    public void Migrate_ShadowColumn_PreservedAcrossRebuild(MigrateMode mode)
    {
        using ModelTestDatabase db = new(model => model.Entity<MigSimple>()
            .Column("Version", SQLiteColumnType.Integer, nullable: false, defaultSql: "0")
            .Check(m => m.Extra == null || m.Extra >= 0, name: "CK_MigSimple_Extra"));
        db.Execute("CREATE TABLE \"MigSimple\" (\"Id\" INTEGER PRIMARY KEY, \"Name\" TEXT NOT NULL, \"Extra\" INTEGER, \"Version\" INTEGER NOT NULL DEFAULT 0)");
        db.Execute("INSERT INTO \"MigSimple\" (\"Id\", \"Name\", \"Version\") VALUES (1, 'a', 42)");

        db.Table<MigSimple>().Schema.Migrate(mode);

        MigrateModeExtensions.AssertSchemaEquivalent("CREATE TABLE \"MigSimple\" (\"Id\" INTEGER PRIMARY KEY, \"Name\" TEXT NOT NULL, \"Extra\" INTEGER NULL, \"Version\" INTEGER NOT NULL DEFAULT 0, CONSTRAINT \"CK_MigSimple_Extra\" CHECK (\"Extra\" IS NULL OR COALESCE(\"Extra\" >= 0, 0)))", TableSql(db, "MigSimple"));
        Assert.Equal(42, db.ExecuteScalar<long>("SELECT \"Version\" FROM \"MigSimple\""));
    }

    [Theory]
    [InlineData(MigrateMode.InPlace)]
    [InlineData(MigrateMode.Rebuild)]
    public void Migrate_FailedCopy_RollsBackAndPreservesData(MigrateMode mode)
    {
        using ModelTestDatabase db = new(model => model.Entity<MigSimple>().Check(m => m.Extra == null || m.Extra >= 0, name: "CK_MigSimple_Extra"));
        db.Execute("CREATE TABLE \"MigSimple\" (\"Id\" INTEGER PRIMARY KEY, \"Name\" TEXT NOT NULL, \"Extra\" INTEGER)");
        db.Execute("INSERT INTO \"MigSimple\" (\"Id\", \"Name\", \"Extra\") VALUES (1, 'a', -5)");
        long foreignKeysBefore = db.ExecuteScalar<long>("PRAGMA foreign_keys");

        Assert.Throws<SQLiteException>(() => db.Table<MigSimple>().Schema.Migrate(mode));

        Assert.Equal(-5, db.Table<MigSimple>().Single().Extra);
        MigrateModeExtensions.AssertSchemaEquivalent("CREATE TABLE \"MigSimple\" (\"Id\" INTEGER PRIMARY KEY, \"Name\" TEXT NOT NULL, \"Extra\" INTEGER)", TableSql(db, "MigSimple"));
        Assert.Equal(foreignKeysBefore, db.ExecuteScalar<long>("PRAGMA foreign_keys"));
        Assert.Null(TableSql(db, "MigSimple__sqlitefw_migrate"));
    }

    [Theory]
    [InlineData(MigrateMode.InPlace)]
    [InlineData(MigrateMode.Rebuild)]
    public void Migrate_FtsTable_EnsuresExists(MigrateMode mode)
    {
        using TestDatabase db = new();

        db.Schema.Table<SimpleSearchEntity>().Migrate(mode);

        Assert.True(db.Schema.TableExists<SimpleSearchEntity>());
    }

    [Theory]
    [InlineData(MigrateMode.InPlace)]
    [InlineData(MigrateMode.Rebuild)]
    public void Migrate_RTreeTable_EnsuresExists(MigrateMode mode)
    {
        using TestDatabase db = new();

        db.Schema.Table<Region2D>().Migrate(mode);

        Assert.True(db.Schema.TableExists<Region2D>());
    }

    [Fact]
    public async Task MigrateAsync_CreatesMissingTable()
    {
        using TestDatabase db = new();

        await db.Schema.Table<MigSimple>().MigrateAsync(TestContext.Current.CancellationToken);

        Assert.True(db.Schema.TableExists<MigSimple>());
    }

    [Fact]
    public async Task MigrateByRebuildAsync_CreatesMissingTable()
    {
        using TestDatabase db = new();

        await db.Schema.Table<MigSimple>().MigrateByRebuildAsync(TestContext.Current.CancellationToken);

        Assert.True(db.Schema.TableExists<MigSimple>());
    }

    [Theory]
    [InlineData(MigrateMode.InPlace)]
    [InlineData(MigrateMode.Rebuild)]
    public void Migrate_CreatesMissingDeclaredTrigger(MigrateMode mode)
    {
        using MigrateTriggerDatabase db = new();
        db.Schema.CreateTable<MigSimple>();
        db.Schema.CreateTable<MigAudit>();
        db.Schema.DropTrigger("trg_mig_ins");
        Assert.Null(TriggerSql(db, "trg_mig_ins"));

        db.Table<MigSimple>().Schema.Migrate(mode);

        Assert.NotNull(TriggerSql(db, "trg_mig_ins"));
    }

    [Theory]
    [InlineData(MigrateMode.InPlace)]
    [InlineData(MigrateMode.Rebuild)]
    public void Migrate_RecreatesChangedDeclaredTrigger(MigrateMode mode)
    {
        using MigrateTriggerDatabase db = new();
        db.Schema.CreateTable<MigSimple>();
        db.Schema.CreateTable<MigAudit>();
        db.Execute("DROP TRIGGER \"trg_mig_ins\"");
        db.Execute("CREATE TRIGGER \"trg_mig_ins\" AFTER INSERT ON \"MigSimple\" BEGIN INSERT INTO \"MigAudit\" (\"ItemId\") VALUES (999); END");

        db.Table<MigSimple>().Schema.Migrate(mode);

        db.Execute("INSERT INTO \"MigSimple\" (\"Id\", \"Name\") VALUES (3, 'a')");
        Assert.Equal(3, db.Table<MigAudit>().Single().ItemId);
    }

    [Theory]
    [InlineData(MigrateMode.InPlace)]
    [InlineData(MigrateMode.Rebuild)]
    public void Migrate_LeavesUndeclaredTriggerAlone_AndPreservesAcrossRebuild(MigrateMode mode)
    {
        using MigrateTriggerDatabase db = new();
        db.Schema.CreateTable<MigSimple>();
        db.Schema.CreateTable<MigAudit>();
        db.Execute("CREATE TRIGGER \"trg_extra\" AFTER INSERT ON \"MigSimple\" BEGIN INSERT INTO \"MigAudit\" (\"ItemId\") VALUES (1); END");

        db.Table<MigSimple>().Schema.Migrate(mode);

        Assert.NotNull(TriggerSql(db, "trg_extra"));
        Assert.NotNull(TriggerSql(db, "trg_mig_ins"));
    }

    [Theory]
    [InlineData(MigrateMode.InPlace)]
    [InlineData(MigrateMode.Rebuild)]
    public void Migrate_RebuildPreservesTriggers(MigrateMode mode)
    {
        using MigrateTriggerDatabase db = new();
        db.Schema.CreateTable<MigAudit>();
        db.Execute("CREATE TABLE \"MigSimple\" (\"Id\" INTEGER PRIMARY KEY, \"Name\" TEXT NOT NULL, \"Extra\" INTEGER, \"Old\" TEXT)");
        db.Execute("CREATE TRIGGER \"trg_mig_ins\" AFTER INSERT ON \"MigSimple\" FOR EACH ROW BEGIN INSERT INTO \"MigAudit\" (\"ItemId\") VALUES (NEW.\"Id\"); END");
        db.Execute("CREATE TRIGGER \"trg_extra\" AFTER INSERT ON \"MigSimple\" BEGIN INSERT INTO \"MigAudit\" (\"ItemId\") VALUES (1); END");

        db.Table<MigSimple>().Schema.Migrate(mode);

        MigrateModeExtensions.AssertSchemaEquivalent("CREATE TABLE \"MigSimple\" (\"Id\" INTEGER PRIMARY KEY, \"Name\" TEXT NOT NULL, \"Extra\" INTEGER NULL)", TableSql(db, "MigSimple"));
        Assert.NotNull(TriggerSql(db, "trg_extra"));
        Assert.NotNull(TriggerSql(db, "trg_mig_ins"));
    }

    [Theory]
    [InlineData(MigrateMode.InPlace)]
    [InlineData(MigrateMode.Rebuild)]
    public void Migrate_Set_FillsNewNotNullColumns(MigrateMode mode)
    {
        using TestDatabase db = new();
        db.Execute("CREATE TABLE \"MigFill\" (\"Id\" INTEGER PRIMARY KEY, \"Name\" TEXT NOT NULL)");
        db.Execute("INSERT INTO \"MigFill\" (\"Id\", \"Name\") VALUES (1, 'a'), (2, 'b')");

        db.Table<MigFill>().Schema.Migrate(mode, m => m
            .Set(x => x.Status, "active")
            .Set(x => x.Doubled, x => x.Id * 2));

        List<MigFill> rows = db.Table<MigFill>().OrderBy(r => r.Id).ToList();
        Assert.Equal(["active", "active"], rows.Select(r => r.Status));
        Assert.Equal([2, 4], rows.Select(r => r.Doubled));
    }

    [Theory]
    [InlineData(MigrateMode.InPlace)]
    [InlineData(MigrateMode.Rebuild)]
    public void Migrate_Set_NoDrift_StillOverridesColumn(MigrateMode mode)
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<MigFill>();
        db.Execute("INSERT INTO \"MigFill\" (\"Id\", \"Name\", \"Status\", \"Doubled\") VALUES (1, 'a', 'old', 2)");

        db.Table<MigFill>().Schema.Migrate(mode, m => m.Set(x => x.Status, "new"));

        MigFill row = db.Table<MigFill>().Single();
        Assert.Equal("new", row.Status);
        Assert.Equal(2, row.Doubled);
    }

    [Theory]
    [InlineData(MigrateMode.InPlace)]
    [InlineData(MigrateMode.Rebuild)]
    public void Migrate_Set_CastTarget_ResolvesColumn(MigrateMode mode)
    {
        using TestDatabase db = new();
        db.Execute("CREATE TABLE \"MigFill\" (\"Id\" INTEGER PRIMARY KEY, \"Name\" TEXT NOT NULL)");
        db.Execute("INSERT INTO \"MigFill\" (\"Id\", \"Name\") VALUES (1, 'a')");

        db.Table<MigFill>().Schema.Migrate(mode, m => m
            .Set(x => (long)x.Doubled, 9L)
            .Set(x => x.Status, "x"));

        Assert.Equal(9, db.Table<MigFill>().Single().Doubled);
    }

    [Theory]
    [InlineData(MigrateMode.InPlace)]
    [InlineData(MigrateMode.Rebuild)]
    public void Migrate_NewNotNull_NoValue_ThrowsClearError(MigrateMode mode)
    {
        using TestDatabase db = new();
        db.Execute("CREATE TABLE \"MigFill\" (\"Id\" INTEGER PRIMARY KEY, \"Name\" TEXT NOT NULL)");
        db.Execute("INSERT INTO \"MigFill\" (\"Id\", \"Name\") VALUES (1, 'a')");

        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() => db.Table<MigFill>().Schema.Migrate(mode));
        Assert.Contains("Status", ex.Message);
    }

    [Theory]
    [InlineData(MigrateMode.InPlace)]
    [InlineData(MigrateMode.Rebuild)]
    public void Migrate_NewNotNull_EmptyTable_Succeeds(MigrateMode mode)
    {
        using TestDatabase db = new();
        db.Execute("CREATE TABLE \"MigFill\" (\"Id\" INTEGER PRIMARY KEY, \"Name\" TEXT NOT NULL)");

        db.Table<MigFill>().Schema.Migrate(mode);

        Assert.True(db.Table<MigFill>().Schema.ColumnExists("Status"));
    }

    [Theory]
    [InlineData(MigrateMode.InPlace)]
    [InlineData(MigrateMode.Rebuild)]
    public void Migrate_Set_ShadowColumnTarget_FromOldRow(MigrateMode mode)
    {
        using ModelTestDatabase db = new(model => model.Entity<MigFill>()
            .Column("Tag", SQLiteColumnType.Text, nullable: false));
        db.Execute("CREATE TABLE \"MigFill\" (\"Id\" INTEGER PRIMARY KEY, \"Name\" TEXT NOT NULL, \"Status\" TEXT NOT NULL, \"Doubled\" INTEGER NOT NULL)");
        db.Execute("INSERT INTO \"MigFill\" (\"Id\", \"Name\", \"Status\", \"Doubled\") VALUES (1, 'a', 's', 2)");

        db.Table<MigFill>().Schema.Migrate(mode, m => m.Set(x => SQLiteColumn.Of<string>(x, "Tag"), x => x.Name));

        Assert.Equal("a", db.ExecuteScalar<string>("SELECT \"Tag\" FROM \"MigFill\""));
    }

    [Theory]
    [InlineData(MigrateMode.InPlace)]
    [InlineData(MigrateMode.Rebuild)]
    public void Migrate_Set_ValueReadsShadowColumn(MigrateMode mode)
    {
        using ModelTestDatabase db = new(model => model.Entity<MigFill>()
            .Column("Tag", SQLiteColumnType.Text, nullable: true));
        db.Execute("CREATE TABLE \"MigFill\" (\"Id\" INTEGER PRIMARY KEY, \"Name\" TEXT NOT NULL, \"Status\" TEXT NOT NULL, \"Doubled\" INTEGER NOT NULL, \"Tag\" TEXT)");
        db.Execute("INSERT INTO \"MigFill\" (\"Id\", \"Name\", \"Status\", \"Doubled\", \"Tag\") VALUES (1, 'a', 's', 2, 'hello')");

        db.Table<MigFill>().Schema.Migrate(mode, m => m.Set(x => x.Status, x => SQLiteColumn.Of<string>(x, "Tag")));

        Assert.Equal("hello", db.ExecuteScalar<string>("SELECT \"Status\" FROM \"MigFill\""));
    }

    [Theory]
    [InlineData(MigrateMode.InPlace)]
    [InlineData(MigrateMode.Rebuild)]
    public void Migrate_Set_InvalidTarget_Throws(MigrateMode mode)
    {
        using TestDatabase db = new();

        Assert.Throws<ArgumentException>(() => db.Table<MigFill>().Schema.Migrate(mode, m => m.Set(x => 5, 1)));
    }

    [Theory]
    [InlineData(MigrateMode.InPlace)]
    [InlineData(MigrateMode.Rebuild)]
    public void Migrate_Set_NonColumnMemberTarget_Throws(MigrateMode mode)
    {
        using TestDatabase db = new();

        Assert.Throws<ArgumentException>(() => db.Table<MigFill>().Schema.Migrate(mode, m => m.Set(x => x.Scratch, 1)));
    }

    [Fact]
    public void Column_DirectCall_Throws()
    {
        Assert.Throws<InvalidOperationException>(() => SQLiteColumn.Of<int>(new object(), "X"));
    }

    [Fact]
    public async Task MigrateByRebuildAsync_WithSet_FillsColumns()
    {
        using TestDatabase db = new();
        db.Execute("CREATE TABLE \"MigFill\" (\"Id\" INTEGER PRIMARY KEY, \"Name\" TEXT NOT NULL)");
        db.Execute("INSERT INTO \"MigFill\" (\"Id\", \"Name\") VALUES (1, 'a')");

        await db.Table<MigFill>().Schema.MigrateByRebuildAsync(
            m => m.Set(x => x.Status, "active").Set(x => x.Doubled, x => x.Id),
            TestContext.Current.CancellationToken);

        Assert.Equal("active", db.Table<MigFill>().Single().Status);
    }

    [Fact]
    public async Task MigrateAsync_WithSet_FillsColumns()
    {
        using TestDatabase db = new();
        db.Execute("CREATE TABLE \"MigFill\" (\"Id\" INTEGER PRIMARY KEY, \"Name\" TEXT NOT NULL)");
        db.Execute("INSERT INTO \"MigFill\" (\"Id\", \"Name\") VALUES (1, 'a')");

        await db.Table<MigFill>().Schema.MigrateAsync(
            m => m.Set(x => x.Status, "active").Set(x => x.Doubled, x => x.Id),
            TestContext.Current.CancellationToken);

        Assert.Equal("active", db.Table<MigFill>().Single().Status);
    }

    [Fact]
    public void ValidateModel_ViaBuilder_MatchesSchema()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<MigSimple>();

        Assert.True(db.Schema.Table<MigSimple>().ValidateModel().IsValid);
        Assert.True(db.Table<MigSimple>().Schema.ValidateModel().IsValid);
    }
}

public sealed class MigrateTriggerDatabase : TestDatabase
{
    public MigrateTriggerDatabase([CallerMemberName] string? methodName = null)
        : base(methodName)
    {
    }

    protected override void OnModelCreating(SQLiteModelBuilder builder)
    {
        builder.Entity<MigAudit>().HasKey(a => a.Id);
        builder.Entity<MigSimple>()
            .Trigger("trg_mig_ins", SQLiteTriggerTiming.After, SQLiteTriggerEvent.Insert,
                t => t.Insert(Table<MigAudit>(), s => s.Set(a => a.ItemId, _ => t.New.Id)));
    }
}

[Table("MigSimple")]
public class MigSimple
{
    [Key]
    public int Id { get; set; }
    public required string Name { get; set; }
    public int? Extra { get; set; }
}

[Table("MigFill")]
public class MigFill
{
    [Key]
    public int Id { get; set; }
    public required string Name { get; set; }
    public string Status { get; set; } = "";
    public int Doubled { get; set; }

    [NotMapped]
    public int Scratch { get; set; }
}

[Table("MigComputed")]
public class MigComputed
{
    [Key]
    public int Id { get; set; }
    public double Price { get; set; }
    public int Quantity { get; set; }
    public double Total { get; set; }
}

[Table("MigParent")]
public class MigParent
{
    [Key]
    public int Id { get; set; }
    public required string Name { get; set; }
}

[Table("MigChild")]
public class MigChild
{
    [Key]
    public int Id { get; set; }

    [ReferencesTable(typeof(MigParent))]
    public int ParentId { get; set; }
}

[Table("MigIndexed")]
public class MigIndexed
{
    [Key]
    public int Id { get; set; }
    public required string Name { get; set; }
    public int Age { get; set; }
}

[Table("MigComposite")]
public class MigComposite
{
    [Key]
    public int A { get; set; }
    [Key]
    public int B { get; set; }
    public int? V { get; set; }
}

[Table("MigWor")]
[WithoutRowId]
public class MigWor
{
    [Key]
    public required string Key { get; set; }
    public int Val { get; set; }
    public string? Note { get; set; }
}

[Table("MigNoCommon")]
public class MigNoCommon
{
    [Key]
    public int Id { get; set; }
    public int Value { get; set; }
}

[Table("MigAudit")]
public class MigAudit
{
    public int Id { get; set; }
    public int ItemId { get; set; }
}
