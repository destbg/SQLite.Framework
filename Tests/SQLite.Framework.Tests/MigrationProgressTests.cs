using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework;
using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("ProgressRows")]
public class ProgressRow
{
    [Key]
    public int Id { get; set; }

    public string Name { get; set; } = "";
}

public class MigrationProgressTests
{
    [Fact]
    public void ReportsEveryOperationInExecutionOrder()
    {
        using TestDatabase db = new(useFile: true);
        List<(int Version, string Description, int Index, int Count)> events = [];

        db.Schema.Migrations()
            .Progress(p => events.Add((p.Version, p.Description, p.Index, p.Count)))
            .Version(1, m => m
                .RunBefore(_ => { })
                .CreateTable<ProgressRow>()
                .Insert(new ProgressRow { Id = 1, Name = "a" }))
            .Version(2, m => m
                .RenameTable<ProgressRow>("ProgressOld")
                .RenameColumn<ProgressRow>("OldName", "Name")
                .TableChanged<ProgressRow>()
                .Sql("UPDATE \"ProgressRows\" SET \"Name\" = 'b'")
                .Run(_ => { }))
            .Migrate();

        Assert.Equal(
        [
            (1, "run callback before schema changes at version 1", 1, 8),
            (2, "rename table \"ProgressOld\" to \"ProgressRows\"", 2, 8),
            (2, "rename column \"OldName\" to \"Name\" on \"ProgressRows\"", 3, 8),
            (1, "create \"ProgressRows\"", 4, 8),
            (2, "reconcile \"ProgressRows\"", 5, 8),
            (1, "insert 1 row(s) into \"ProgressRows\"", 6, 8),
            (2, "run SQL", 7, 8),
            (2, "run callback at version 2", 8, 8),
        ], events);
    }

    [Fact]
    public async Task ReportsDuringMigrateAsync()
    {
        using TestDatabase db = new(useFile: true);
        List<string> descriptions = [];

        await db.Schema.Migrations()
            .Progress(p => descriptions.Add(p.Description))
            .Version(1, m => m
                .RunBefore(_ => { })
                .CreateTable<ProgressRow>()
                .RunAsync(_ => Task.CompletedTask))
            .MigrateAsync();

        Assert.Equal(
        [
            "run callback before schema changes at version 1",
            "create \"ProgressRows\"",
            "run async callback at version 1",
        ], descriptions);
    }

    [Fact]
    public void ReportsDuringScript()
    {
        using TestDatabase db = new(useFile: true);
        List<string> descriptions = [];

        db.Schema.Migrations()
            .Progress(p => descriptions.Add(p.Description))
            .Version(1, m => m.CreateTable<ProgressRow>().Run(_ => { }))
            .Script();

        Assert.Equal(["create \"ProgressRows\"", "run callback at version 1"], descriptions);
    }

    [Fact]
    public void NothingReportedWhenUpToDate()
    {
        using TestDatabase db = new(useFile: true);
        db.Pragmas.UserVersion = 1;
        List<string> descriptions = [];

        db.Schema.Migrations()
            .Progress(p => descriptions.Add(p.Description))
            .Version(1, m => m.CreateTable<ProgressRow>())
            .Migrate();

        Assert.Empty(descriptions);
    }

    [Fact]
    public void NullCallbackThrows()
    {
        using TestDatabase db = new(useFile: true);

        Assert.Throws<ArgumentNullException>(() => db.Schema.Migrations().Progress(null!));
    }
}
