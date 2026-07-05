using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework;
using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("NewerDbRows")]
public class NewerDatabaseRow
{
    [Key]
    public int Id { get; set; }

    public string Name { get; set; } = "";
}

public class MigrationNewerDatabaseTests
{
    [Fact]
    public void MigrateThrowsWhenDatabaseIsNewer()
    {
        using TestDatabase db = new(useFile: true);
        db.Pragmas.UserVersion = 5;

        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() => db.Schema.Migrations()
            .Version(1, m => m.CreateTable<NewerDatabaseRow>())
            .Version(3, m => m.TableChanged<NewerDatabaseRow>())
            .Migrate());

        Assert.Equal(
            "The database records version 5 but the highest declared version is 3. " +
            "A newer app version created this database. Add the missing versions or open it with the newer app.",
            ex.Message);
        Assert.Equal(5, db.Pragmas.UserVersion);
    }

    [Fact]
    public async Task MigrateAsyncThrowsWhenDatabaseIsNewer()
    {
        using TestDatabase db = new(useFile: true);
        db.Pragmas.UserVersion = 2;

        await Assert.ThrowsAsync<InvalidOperationException>(() => db.Schema.Migrations()
            .Version(1, m => m.CreateTable<NewerDatabaseRow>())
            .MigrateAsync());
    }

    [Fact]
    public void ScriptThrowsWhenDatabaseIsNewer()
    {
        using TestDatabase db = new(useFile: true);
        db.Pragmas.UserVersion = 2;

        Assert.Throws<InvalidOperationException>(() => db.Schema.Migrations()
            .Version(1, m => m.CreateTable<NewerDatabaseRow>())
            .Script());
    }

    [Fact]
    public void PlanReportsNewerDatabaseWithoutThrowing()
    {
        using TestDatabase db = new(useFile: true);
        db.Pragmas.UserVersion = 5;

        SQLiteMigrationPlan plan = db.Schema.Migrations()
            .Version(1, m => m.CreateTable<NewerDatabaseRow>())
            .Version(3, m => m.TableChanged<NewerDatabaseRow>())
            .Plan();

        Assert.True(plan.DatabaseIsNewer);
        Assert.False(plan.IsUpToDate);
        Assert.Equal(5, plan.CurrentVersion);
        Assert.Equal(3, plan.TargetVersion);
        Assert.Empty(plan.Operations);
    }

    [Fact]
    public void PlanIsUpToDateWhenVersionsMatch()
    {
        using TestDatabase db = new(useFile: true);
        db.Pragmas.UserVersion = 3;

        SQLiteMigrationPlan plan = db.Schema.Migrations()
            .Version(3, m => m.TableChanged<NewerDatabaseRow>())
            .Plan();

        Assert.True(plan.IsUpToDate);
        Assert.False(plan.DatabaseIsNewer);
    }

    [Fact]
    public void PlanWithNoVersionsIsUpToDate()
    {
        using TestDatabase db = new(useFile: true);

        SQLiteMigrationPlan plan = db.Schema.Migrations().Plan();

        Assert.True(plan.IsUpToDate);
        Assert.False(plan.DatabaseIsNewer);
        Assert.Equal(0, plan.CurrentVersion);
        Assert.Equal(0, plan.TargetVersion);
    }

    [Fact]
    public void MigrateWithNoVersionsIgnoresRecordedVersion()
    {
        using TestDatabase db = new(useFile: true);
        db.Pragmas.UserVersion = 9;

        int statements = db.Schema.Migrations().Migrate();

        Assert.Equal(0, statements);
        Assert.Equal(9, db.Pragmas.UserVersion);
    }
}
