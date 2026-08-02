using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework;
using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("MigBProg")]
public class MigBProgRow
{
    [Key]
    public int Id { get; set; }

    public string? Name { get; set; }
}

public class MigrationDeferredProgressOrderTests
{
    [Fact]
    public void ADeferredReconcileReportsProgressInApplicationOrder()
    {
        using TestDatabase db = new(useFile: true);
        List<(string Description, int Index, int Count)> events = [];

        db.Schema.Migrations()
            .Progress(p => events.Add((p.Description, p.Index, p.Count)))
            .Version(1, m => m.Sql("CREATE TABLE \"MigBProg\" (\"Id\" INTEGER PRIMARY KEY, \"Name\" TEXT)"))
            .Version(2, m => m.TableChanged<MigBProgRow>())
            .Migrate();

        Assert.Equal(
        [
            ("run SQL", 1, 2),
            ("reconcile \"MigBProg\"", 2, 2),
        ], events);
    }

    [Fact]
    public void ADeferredReconcileReportsProgressInApplicationOrderDuringScript()
    {
        using TestDatabase db = new(useFile: true);
        List<string> events = [];

        db.Schema.Migrations()
            .Progress(p => events.Add(p.Description))
            .Version(1, m => m.Sql("CREATE TABLE \"MigBProg\" (\"Id\" INTEGER PRIMARY KEY, \"Name\" TEXT)"))
            .Version(2, m => m.TableChanged<MigBProgRow>())
            .Script();

        Assert.Equal(["run SQL", "reconcile \"MigBProg\""], events);
    }

    [Fact]
    public async Task ADeferredReconcileReportsProgressInApplicationOrderDuringMigrateAsync()
    {
        using TestDatabase db = new(useFile: true);
        List<string> events = [];

        await db.Schema.Migrations()
            .Progress(p => events.Add(p.Description))
            .Version(1, m => m.Sql("CREATE TABLE \"MigBProg\" (\"Id\" INTEGER PRIMARY KEY, \"Name\" TEXT)"))
            .Version(2, m => m.TableChanged<MigBProgRow>())
            .MigrateAsync();

        Assert.Equal(["run SQL", "reconcile \"MigBProg\""], events);
    }
}
