using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework;
using SQLite.Framework.Attributes;
using SQLite.Framework.Enums;
using SQLite.Framework.Exceptions;
using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("SeedCategory")]
public class SeedCategoryRow
{
    [Key]
    [AutoIncrement]
    public int Id { get; set; }

    public string Name { get; set; } = "";
}

[Table("SeedExpand")]
public class SeedExpandRow
{
    [Key]
    public int Id { get; set; }

    public int Extra { get; set; }
}

[Table("SeedStatus")]
public class SeedStatusRow
{
    [Key]
    public int Id { get; set; }

    public SeedState State { get; set; }
}

public enum SeedState
{
    Draft = 0,
    Active = 1,
}

[Table("SeedFixed")]
public class SeedFixedRow
{
    [Key]
    public int Id { get; set; }

    public string Code { get; set; } = "";
}

public class MigrationInsertStepTests
{
    [Fact]
    public void InsertSeedsRowsWithCreateTableInOneVersion()
    {
        using TestDatabase db = new(useFile: true);

        int count = db.Schema.Migrations()
            .Version(1, m => m
                .CreateTable<SeedCategoryRow>()
                .Insert(new SeedCategoryRow { Name = "fiction" }, new SeedCategoryRow { Name = "science" }))
            .Migrate();

        Assert.True(count >= 2);
        List<string> names = db.Table<SeedCategoryRow>().OrderBy(r => r.Id).Select(r => r.Name).ToList();
        Assert.Equal(["fiction", "science"], names);
        Assert.Equal(1, db.Pragmas.UserVersion);
    }

    [Fact]
    public void AppliedVersionDoesNotInsertAgain()
    {
        using TestDatabase db = new(useFile: true);
        db.Schema.Migrations()
            .Version(1, m => m.CreateTable<SeedCategoryRow>().Insert(new SeedCategoryRow { Name = "fiction" }))
            .Migrate();

        int second = db.Schema.Migrations()
            .Version(1, m => m.CreateTable<SeedCategoryRow>().Insert(new SeedCategoryRow { Name = "fiction" }))
            .Migrate();

        Assert.Equal(0, second);
        Assert.Equal(1, db.Table<SeedCategoryRow>().Count());
    }

    [Fact]
    public void InsertRunsAfterReconcileAgainstFinalShape()
    {
        using TestDatabase db = new(useFile: true);
        db.Execute("CREATE TABLE \"SeedExpand\" (\"Id\" INTEGER PRIMARY KEY)");
        db.Execute("INSERT INTO \"SeedExpand\" (\"Id\") VALUES (1)");

        db.Schema.Migrations()
            .Version(1, m => m
                .TableChanged<SeedExpandRow>(s => s.Set(r => r.Extra, 0))
                .Insert(new SeedExpandRow { Id = 5, Extra = 7 }))
            .Migrate();

        List<SeedExpandRow> rows = db.Table<SeedExpandRow>().OrderBy(r => r.Id).ToList();
        Assert.Equal(2, rows.Count);
        Assert.Equal(0, rows[0].Extra);
        Assert.Equal(7, rows[1].Extra);
    }

    [Fact]
    public void SqlStepDeclaredAfterInsertSeesTheInsertedRows()
    {
        using TestDatabase db = new(useFile: true);

        db.Schema.Migrations()
            .Version(1, m => m
                .CreateTable<SeedCategoryRow>()
                .Insert(new SeedCategoryRow { Name = "fiction" })
                .Sql("UPDATE \"SeedCategory\" SET \"Name\" = UPPER(\"Name\")"))
            .Migrate();

        Assert.Equal("FICTION", db.Table<SeedCategoryRow>().Single().Name);
    }

    [Fact]
    public void FailedInsertRollsBackTheWholeRun()
    {
        using TestDatabase db = new(useFile: true);
        db.Schema.Migrations()
            .Version(1, m => m.CreateTable<SeedFixedRow>().Insert(new SeedFixedRow { Id = 1, Code = "first" }))
            .Migrate();

        SQLiteMigrationRunner runner = db.Schema.Migrations()
            .Version(1, m => m.CreateTable<SeedFixedRow>().Insert(new SeedFixedRow { Id = 1, Code = "first" }))
            .Version(2, m => m
                .Sql("UPDATE \"SeedFixed\" SET \"Code\" = 'touched' WHERE \"Id\" = 1")
                .Insert(new SeedFixedRow { Id = 1, Code = "duplicate" }));

        Assert.Throws<SQLiteException>(() => runner.Migrate());
        Assert.Equal(1, db.Pragmas.UserVersion);
        Assert.Equal("first", db.Table<SeedFixedRow>().Single().Code);
    }

    [Fact]
    public void InsertWritesBackAutoIncrementKeys()
    {
        using TestDatabase db = new(useFile: true);
        SeedCategoryRow first = new() { Name = "fiction" };
        SeedCategoryRow second = new() { Name = "science" };

        db.Schema.Migrations()
            .Version(1, m => m.CreateTable<SeedCategoryRow>().Insert(first, second))
            .Migrate();

        Assert.Equal(1, first.Id);
        Assert.Equal(2, second.Id);
    }

    [Fact]
    public void InsertAppliesEnumTextStorage()
    {
        using TestDatabase db = new(b => b.UseEnumStorage(EnumStorageMode.Text), useFile: true);

        db.Schema.Migrations()
            .Version(1, m => m
                .CreateTable<SeedStatusRow>()
                .Insert(new SeedStatusRow { Id = 1, State = SeedState.Active }))
            .Migrate();

        Assert.Equal("Active", db.ExecuteScalar<string>("SELECT \"State\" FROM \"SeedStatus\" WHERE \"Id\" = 1"));
        Assert.Equal(SeedState.Active, db.Table<SeedStatusRow>().Single().State);
    }

    [Fact]
    public void InsertRunsOnAddHooks()
    {
        using TestDatabase db = new(b => b.OnAdd<SeedCategoryRow>(r => r.Name += "!"), useFile: true);

        db.Schema.Migrations()
            .Version(1, m => m.CreateTable<SeedCategoryRow>().Insert(new SeedCategoryRow { Name = "fiction" }))
            .Migrate();

        Assert.Equal("fiction!", db.Table<SeedCategoryRow>().Single().Name);
    }

    [Fact]
    public void PlanReportsInsertWithoutInserting()
    {
        using TestDatabase db = new(useFile: true);

        SQLiteMigrationPlan plan = db.Schema.Migrations()
            .Version(1, m => m
                .CreateTable<SeedCategoryRow>()
                .Insert(new SeedCategoryRow { Name = "fiction" }, new SeedCategoryRow { Name = "science" }))
            .Plan();

        Assert.False(plan.IsUpToDate);
        Assert.Contains("insert 2 row(s) into \"SeedCategory\"", plan.Operations);
        Assert.False(db.Schema.TableExists<SeedCategoryRow>());
    }

    [Fact]
    public void InsertNullRowsThrows()
    {
        using TestDatabase db = new(useFile: true);
        SQLiteMigrationRunner runner = db.Schema.Migrations()
            .Version(1, m => m.Insert((SeedCategoryRow[])null!));

        Assert.Throws<ArgumentNullException>(() => runner.Migrate());
    }

    [Fact]
    public void InsertWithoutRowsThrows()
    {
        using TestDatabase db = new(useFile: true);
        SQLiteMigrationRunner runner = db.Schema.Migrations()
            .Version(1, m => m.Insert<SeedCategoryRow>());

        ArgumentException ex = Assert.Throws<ArgumentException>(() => runner.Migrate());
        Assert.StartsWith("Insert requires at least one row.", ex.Message);
    }
}
