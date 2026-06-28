using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework;
using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("RunExpand")]
public class RunnerExpandRow
{
    [Key]
    public int Id { get; set; }

    public int A { get; set; }

    public int B { get; set; }
}

[Table("RunOverride")]
public class RunnerOverrideRow
{
    [Key]
    public int Id { get; set; }

    public int A { get; set; }
}

[Table("RunRename")]
public class RunnerRenameRow
{
    [Key]
    public int Id { get; set; }

    public string NewName { get; set; } = "";
}

[Table("RunDrop")]
public class RunnerDropRow
{
    [Key]
    public int Id { get; set; }

    public int Keep { get; set; }
}

[Table("RunData")]
public class RunnerDataRow
{
    [Key]
    public int Id { get; set; }

    public int Value { get; set; }
}

[Table("DiaRoot")]
public class RunnerDiamondRootRow
{
    [Key]
    public int Id { get; set; }

    public string Name { get; set; } = "";
}

public sealed class RunnerAddMigration : ISQLiteMigration
{
    public static int Version => 3;

    public void Apply(SQLiteMigrationStep step)
    {
        step.TableChanged<RunnerDataRow>();
    }
}

public sealed class RunnerThrowingMigration : ISQLiteMigration
{
    public RunnerThrowingMigration()
    {
        throw new InvalidOperationException("constructed");
    }

    public static int Version => 2;

    public void Apply(SQLiteMigrationStep step)
    {
        step.TableChanged<RunnerDataRow>();
    }
}

public class MigrationRunnerTests
{
    [Fact]
    public void Migrate_UnionsFillsAcrossVersions_FillsEveryNotNullColumn()
    {
        using TestDatabase db = new(useFile: true);
        db.Execute("CREATE TABLE \"RunExpand\" (\"Id\" INTEGER PRIMARY KEY)");
        db.Execute("INSERT INTO \"RunExpand\" (\"Id\") VALUES (1)");

        db.Schema.Migrations()
            .Version(1, m => m.TableChanged<RunnerExpandRow>(s => s.Set(b => b.A, 7)))
            .Version(2, m => m.TableChanged<RunnerExpandRow>(s => s.Set(b => b.B, 9)))
            .Migrate();

        RunnerExpandRow row = db.Table<RunnerExpandRow>().Single();
        Assert.Equal(7, row.A);
        Assert.Equal(9, row.B);
        Assert.Equal(2, db.Pragmas.UserVersion);
    }

    [Fact]
    public void Migrate_LaterVersionFill_OverridesEarlier()
    {
        using TestDatabase db = new(useFile: true);
        db.Execute("CREATE TABLE \"RunOverride\" (\"Id\" INTEGER PRIMARY KEY)");
        db.Execute("INSERT INTO \"RunOverride\" (\"Id\") VALUES (1)");

        db.Schema.Migrations()
            .Version(1, m => m.TableChanged<RunnerOverrideRow>(s => s.Set(b => b.A, 1)))
            .Version(2, m => m.TableChanged<RunnerOverrideRow>(s => s.Set(b => b.A, 2)))
            .Migrate();

        Assert.Equal(2, db.Table<RunnerOverrideRow>().Single().A);
    }

    [Fact]
    public void Migrate_RebuildFlag_FillsColumns()
    {
        using TestDatabase db = new(useFile: true);
        db.Execute("CREATE TABLE \"RunExpand\" (\"Id\" INTEGER PRIMARY KEY)");
        db.Execute("INSERT INTO \"RunExpand\" (\"Id\") VALUES (1)");

        db.Schema.Migrations()
            .Version(1, m => m.TableChanged<RunnerExpandRow>(s => s.Set(b => b.A, 3).Set(b => b.B, 4), rebuild: true))
            .Migrate();

        RunnerExpandRow row = db.Table<RunnerExpandRow>().Single();
        Assert.Equal(3, row.A);
        Assert.Equal(4, row.B);
    }

    [Fact]
    public void Migrate_RenameColumnBeforeReconcile_PreservesData()
    {
        using TestDatabase db = new(useFile: true);
        db.Execute("CREATE TABLE \"RunRename\" (\"Id\" INTEGER PRIMARY KEY, \"OldName\" TEXT NOT NULL)");
        db.Execute("INSERT INTO \"RunRename\" (\"Id\", \"OldName\") VALUES (1, 'keep')");

        db.Schema.Migrations()
            .Version(1, m => m.RenameColumn<RunnerRenameRow>("OldName", "NewName").TableChanged<RunnerRenameRow>())
            .Migrate();

        Assert.Equal("keep", db.Table<RunnerRenameRow>().Single().NewName);
    }

    [Fact]
    public void Migrate_DropColumn_RemovesColumn()
    {
        using TestDatabase db = new(useFile: true);
        db.Execute("CREATE TABLE \"RunDrop\" (\"Id\" INTEGER PRIMARY KEY, \"Keep\" INTEGER NOT NULL, \"Gone\" INTEGER)");

        db.Schema.Migrations()
            .Version(1, m => m.DropColumn<RunnerDropRow>("Gone"))
            .Migrate();

        Assert.DoesNotContain(db.Schema.ListColumns<RunnerDropRow>(), c => c.Name == "Gone");
    }

    [Fact]
    public void Migrate_DropTableByName_RemovesTable()
    {
        using TestDatabase db = new(useFile: true);
        db.Execute("CREATE TABLE \"RunData\" (\"Id\" INTEGER PRIMARY KEY, \"Value\" INTEGER NOT NULL)");

        db.Schema.Migrations()
            .Version(1, m => m.DropTable("RunData"))
            .Migrate();

        Assert.False(db.Schema.TableExists("RunData"));
    }

    [Fact]
    public void Migrate_DropTableByType_RemovesTable()
    {
        using TestDatabase db = new(useFile: true);
        db.Execute("CREATE TABLE \"RunData\" (\"Id\" INTEGER PRIMARY KEY, \"Value\" INTEGER NOT NULL)");

        db.Schema.Migrations()
            .Version(1, m => m.DropTable<RunnerDataRow>())
            .Migrate();

        Assert.False(db.Schema.TableExists<RunnerDataRow>());
    }

    [Fact]
    public void Migrate_RebuildWithDiamondForeignKeys_PreservesReferencingRows()
    {
        using TestDatabase db = new(useFile: true);
        db.Execute("PRAGMA foreign_keys = ON");
        db.Execute("CREATE TABLE \"DiaRoot\" (\"Id\" INTEGER PRIMARY KEY, \"Name\" TEXT NOT NULL, \"Old\" TEXT)");
        db.Execute("CREATE TABLE \"DiaB\" (\"Id\" INTEGER PRIMARY KEY, \"RootId\" INTEGER NOT NULL REFERENCES \"DiaRoot\"(\"Id\"))");
        db.Execute("CREATE TABLE \"DiaC\" (\"Id\" INTEGER PRIMARY KEY, \"RootId\" INTEGER NOT NULL REFERENCES \"DiaRoot\"(\"Id\"))");
        db.Execute("CREATE TABLE \"DiaD\" (\"Id\" INTEGER PRIMARY KEY, \"BId\" INTEGER NOT NULL REFERENCES \"DiaB\"(\"Id\"), \"CId\" INTEGER NOT NULL REFERENCES \"DiaC\"(\"Id\"))");
        db.Execute("INSERT INTO \"DiaRoot\" (\"Id\", \"Name\") VALUES (1, 'r')");
        db.Execute("INSERT INTO \"DiaB\" (\"Id\", \"RootId\") VALUES (1, 1)");
        db.Execute("INSERT INTO \"DiaC\" (\"Id\", \"RootId\") VALUES (1, 1)");
        db.Execute("INSERT INTO \"DiaD\" (\"Id\", \"BId\", \"CId\") VALUES (1, 1, 1)");

        db.Schema.Migrations()
            .Version(1, m => m.TableChanged<RunnerDiamondRootRow>())
            .Migrate();

        Assert.Equal(1, db.Query<int>("SELECT \"BId\" FROM \"DiaD\"").First());
        Assert.Equal(1, db.Query<int>("SELECT \"CId\" FROM \"DiaD\"").First());
        Assert.DoesNotContain(db.Schema.ListColumns("DiaRoot"), c => c.Name == "Old");
    }

    [Fact]
    public void Migrate_SqlDataStep_RunsAgainstFinalSchema()
    {
        using TestDatabase db = new(useFile: true);
        db.Execute("CREATE TABLE \"RunData\" (\"Id\" INTEGER PRIMARY KEY, \"Value\" INTEGER NOT NULL)");
        db.Execute("INSERT INTO \"RunData\" (\"Id\", \"Value\") VALUES (1, 10)");

        db.Schema.Migrations()
            .Version(1, m => m.TableChanged<RunnerDataRow>().Sql("UPDATE \"RunData\" SET \"Value\" = 99"))
            .Migrate();

        Assert.Equal(99, db.Table<RunnerDataRow>().Single().Value);
    }

    [Fact]
    public void Migrate_FreshDatabase_CreatesSchemaAndRunsDataSteps()
    {
        using TestDatabase db = new();

        db.Schema.Migrations()
            .Version(1, m => m.TableChanged<RunnerDataRow>().Sql("INSERT INTO \"RunData\" (\"Id\", \"Value\") VALUES (1, 5)"))
            .Migrate();

        Assert.True(db.Schema.TableExists<RunnerDataRow>());
        Assert.Equal(5, db.Table<RunnerDataRow>().Single().Value);
        Assert.Equal(1, db.Pragmas.UserVersion);
    }

    [Fact]
    public void Migrate_AlreadyAtVersion_DoesNothing()
    {
        using TestDatabase db = new(useFile: true);
        db.Pragmas.UserVersion = 5;

        int statements = db.Schema.Migrations()
            .Version(1, m => m.TableChanged<RunnerDataRow>())
            .Version(3, m => m.TableChanged<RunnerDataRow>())
            .Migrate();

        Assert.Equal(0, statements);
        Assert.False(db.Schema.TableExists<RunnerDataRow>());
        Assert.Equal(5, db.Pragmas.UserVersion);
    }

    [Fact]
    public void Migrate_AppliesOnlyVersionsAboveCurrent()
    {
        using TestDatabase db = new(useFile: true);
        db.Execute("CREATE TABLE \"RunData\" (\"Id\" INTEGER PRIMARY KEY, \"Value\" INTEGER NOT NULL)");
        db.Execute("INSERT INTO \"RunData\" (\"Id\", \"Value\") VALUES (1, 1)");
        db.Pragmas.UserVersion = 1;

        db.Schema.Migrations()
            .Version(1, m => m.Sql("UPDATE \"RunData\" SET \"Value\" = 50"))
            .Version(2, m => m.Sql("UPDATE \"RunData\" SET \"Value\" = 60"))
            .Migrate();

        Assert.Equal(60, db.Table<RunnerDataRow>().Single().Value);
        Assert.Equal(2, db.Pragmas.UserVersion);
    }

    [Fact]
    public void Plan_PendingMigration_ReportsVersionsAndOperations()
    {
        using TestDatabase db = new(useFile: true);

        SQLiteMigrationPlan plan = db.Schema.Migrations()
            .Version(1, m => m.TableChanged<RunnerExpandRow>(s => s.Set(b => b.A, 1), rebuild: true))
            .Version(2, m => m.TableChanged<RunnerExpandRow>())
            .Plan();

        Assert.Equal(0, plan.CurrentVersion);
        Assert.Equal(2, plan.TargetVersion);
        Assert.False(plan.IsUpToDate);
        Assert.Contains("reconcile \"RunExpand\" by rebuild with 1 value(s)", plan.Operations);
        Assert.Contains("reconcile \"RunExpand\"", plan.Operations);
    }

    [Fact]
    public void Plan_NoVersions_IsUpToDate()
    {
        using TestDatabase db = new(useFile: true);

        SQLiteMigrationPlan plan = db.Schema.Migrations().Plan();

        Assert.True(plan.IsUpToDate);
        Assert.Empty(plan.Operations);
        Assert.Equal(0, plan.TargetVersion);
    }

    [Fact]
    public void Plan_AfterMigrate_IsUpToDate()
    {
        using TestDatabase db = new(useFile: true);
        SQLiteMigrationRunner runner = db.Schema.Migrations().Version(1, m => m.TableChanged<RunnerDataRow>());
        runner.Migrate();

        SQLiteMigrationPlan plan = runner.Plan();

        Assert.True(plan.IsUpToDate);
        Assert.Empty(plan.Operations);
        Assert.Equal(1, plan.CurrentVersion);
    }

    [Fact]
    public void Version_LessThanOne_Throws()
    {
        using TestDatabase db = new();
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            db.Schema.Migrations().Version(0, m => m.TableChanged<RunnerDataRow>()));
    }

    [Fact]
    public void Version_Duplicate_Throws()
    {
        using TestDatabase db = new();
        SQLiteMigrationRunner runner = db.Schema.Migrations().Version(1, m => m.TableChanged<RunnerDataRow>());
        Assert.Throws<InvalidOperationException>(() =>
            runner.Version(1, m => m.TableChanged<RunnerDataRow>()));
    }

    [Fact]
    public void Version_NullBuild_Throws()
    {
        using TestDatabase db = new();
        Assert.Throws<ArgumentNullException>(() => db.Schema.Migrations().Version(1, null!));
    }

    [Fact]
    public void Step_EmptyArguments_Throw()
    {
        using TestDatabase db = new();
        Assert.Throws<ArgumentException>(() => db.Schema.Migrations().Version(1, m => m.RenameColumn<RunnerDataRow>("", "x")).Migrate());
        Assert.Throws<ArgumentException>(() => db.Schema.Migrations().Version(1, m => m.RenameColumn<RunnerDataRow>("x", "")).Migrate());
        Assert.Throws<ArgumentException>(() => db.Schema.Migrations().Version(1, m => m.DropColumn<RunnerDataRow>("")).Migrate());
        Assert.Throws<ArgumentException>(() => db.Schema.Migrations().Version(1, m => m.DropTable("")).Migrate());
        Assert.Throws<ArgumentException>(() => db.Schema.Migrations().Version(1, m => m.Sql("")).Migrate());
    }

    [Fact]
    public void CreateTable_FreshDatabase_CreatesTable()
    {
        using TestDatabase db = new();

        db.Schema.Migrations()
            .Version(1, m => m.CreateTable<RunnerDataRow>())
            .Migrate();

        Assert.True(db.Schema.TableExists<RunnerDataRow>());
        Assert.Equal(1, db.Pragmas.UserVersion);
    }

    [Fact]
    public void CreateTable_ExistingTable_KeepsRows()
    {
        using TestDatabase db = new(useFile: true);
        db.Execute("CREATE TABLE \"RunData\" (\"Id\" INTEGER PRIMARY KEY, \"Value\" INTEGER NOT NULL)");
        db.Execute("INSERT INTO \"RunData\" (\"Id\", \"Value\") VALUES (1, 7)");

        db.Schema.Migrations()
            .Version(1, m => m.CreateTable<RunnerDataRow>())
            .Migrate();

        Assert.Equal(7, db.Table<RunnerDataRow>().Single().Value);
    }

    [Fact]
    public void Plan_CreateTable_DescribesCreate()
    {
        using TestDatabase db = new();

        SQLiteMigrationPlan plan = db.Schema.Migrations()
            .Version(1, m => m.CreateTable<RunnerDataRow>())
            .Plan();

        Assert.Contains("create \"RunData\"", plan.Operations);
    }

    [Fact]
    public void Add_AppliesMigration_CreatesTableAndRecordsVersion()
    {
        using TestDatabase db = new();

        db.Schema.Migrations()
            .Add<RunnerAddMigration>()
            .Migrate();

        Assert.True(db.Schema.TableExists<RunnerDataRow>());
        Assert.Equal(3, db.Pragmas.UserVersion);
    }

    [Fact]
    public void Add_VersionAlreadyApplied_DoesNotConstructMigration()
    {
        using TestDatabase db = new(useFile: true);
        db.Pragmas.UserVersion = 5;

        int statements = db.Schema.Migrations()
            .Add<RunnerThrowingMigration>()
            .Migrate();

        Assert.Equal(0, statements);
        Assert.Equal(5, db.Pragmas.UserVersion);
    }

    [Fact]
    public void Add_VersionPending_ConstructsMigration()
    {
        using TestDatabase db = new();

        Exception ex = Assert.ThrowsAny<Exception>(() =>
            db.Schema.Migrations().Add<RunnerThrowingMigration>().Migrate());

        Assert.Contains("constructed", ex.ToString());
    }

    [Fact]
    public async Task MigrateAsync_AppliesPending()
    {
        using TestDatabase db = new();

        await db.Schema.Migrations()
            .Version(1, m => m.TableChanged<RunnerDataRow>())
            .MigrateAsync(TestContext.Current.CancellationToken);

        Assert.True(db.Schema.TableExists<RunnerDataRow>());
        Assert.Equal(1, db.Pragmas.UserVersion);
    }

    [Fact]
    public async Task PlanAsync_ReportsPending()
    {
        using TestDatabase db = new();

        SQLiteMigrationPlan plan = await db.Schema.Migrations()
            .Version(1, m => m.TableChanged<RunnerDataRow>())
            .PlanAsync(TestContext.Current.CancellationToken);

        Assert.False(plan.IsUpToDate);
        Assert.Equal(1, plan.TargetVersion);
    }
}
