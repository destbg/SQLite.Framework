using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H20Mig2_FillOrder")]
public class H20Mig2FillRow
{
    [Key]
    public int Id { get; set; }

    public int Val { get; set; }

    public string? Tag { get; set; }

    public string? Note { get; set; }
}

public class MigrationDeferredReconcileLaterDataParityTests
{
    private static void Seed(TestDatabase db)
    {
        db.Execute("CREATE TABLE \"H20Mig2_FillOrder\" (\"Id\" INTEGER PRIMARY KEY, \"Val\" INTEGER, \"Tag\" TEXT, \"Note\" TEXT)");
        db.Execute("INSERT INTO \"H20Mig2_FillOrder\" (\"Id\", \"Val\", \"Tag\", \"Note\") VALUES (1, 10, 'old', 'n0')");
        db.Pragmas.UserVersion = 1;
    }

    private static List<(int Val, string? Tag, string? Note)> Rows(TestDatabase db)
    {
        return db.Table<H20Mig2FillRow>()
            .OrderBy(x => x.Id)
            .Select(x => new { x.Val, x.Tag, x.Note })
            .ToList()
            .Select(x => (x.Val, x.Tag, x.Note))
            .ToList();
    }

    [Fact]
    public void EarlierFillDoesNotOverwriteLaterUpdate()
    {
        using TestDatabase stepwise = new(useFile: true);
        Seed(stepwise);
        stepwise.Schema.Migrations()
            .Version(2, m => m.TableChanged<H20Mig2FillRow>(s => s.Set(x => x.Tag, "v2")))
            .Migrate();
        stepwise.Schema.Migrations()
            .Version(2, m => m.TableChanged<H20Mig2FillRow>(s => s.Set(x => x.Tag, "v2")))
            .Version(3, m => m.Update<H20Mig2FillRow>(s => s.Set(x => x.Tag, "upd")))
            .Migrate();
        stepwise.Schema.Migrations()
            .Version(2, m => m.TableChanged<H20Mig2FillRow>(s => s.Set(x => x.Tag, "v2")))
            .Version(3, m => m.Update<H20Mig2FillRow>(s => s.Set(x => x.Tag, "upd")))
            .Version(4, m => m.TableChanged<H20Mig2FillRow>(s => s.Set(x => x.Val, r => r.Val + 1)))
            .Migrate();

        using TestDatabase collapsed = new(useFile: true);
        Seed(collapsed);
        collapsed.Schema.Migrations()
            .Version(2, m => m.TableChanged<H20Mig2FillRow>(s => s.Set(x => x.Tag, "v2")))
            .Version(3, m => m.Update<H20Mig2FillRow>(s => s.Set(x => x.Tag, "upd")))
            .Version(4, m => m.TableChanged<H20Mig2FillRow>(s => s.Set(x => x.Val, r => r.Val + 1)))
            .Migrate();

        List<(int Val, string? Tag, string? Note)> stepwiseRows = Rows(stepwise);
        List<(int Val, string? Tag, string? Note)> collapsedRows = Rows(collapsed);

        Assert.Equal([(11, "upd", "n0")], stepwiseRows);
        Assert.Equal(stepwiseRows, collapsedRows);
    }

    [Fact]
    public void EarlierFillDoesNotChangeLaterDeleteMatch()
    {
        using TestDatabase stepwise = new(useFile: true);
        Seed(stepwise);
        stepwise.Schema.Migrations()
            .Version(2, m => m.TableChanged<H20Mig2FillRow>(s => s.Set(x => x.Tag, "v2")))
            .Migrate();
        stepwise.Schema.Migrations()
            .Version(2, m => m.TableChanged<H20Mig2FillRow>(s => s.Set(x => x.Tag, "v2")))
            .Version(3, m => m.Delete<H20Mig2FillRow>(x => x.Tag == "old"))
            .Migrate();
        stepwise.Schema.Migrations()
            .Version(2, m => m.TableChanged<H20Mig2FillRow>(s => s.Set(x => x.Tag, "v2")))
            .Version(3, m => m.Delete<H20Mig2FillRow>(x => x.Tag == "old"))
            .Version(4, m => m.TableChanged<H20Mig2FillRow>(s => s.Set(x => x.Val, r => r.Val + 1)))
            .Migrate();

        using TestDatabase collapsed = new(useFile: true);
        Seed(collapsed);
        collapsed.Schema.Migrations()
            .Version(2, m => m.TableChanged<H20Mig2FillRow>(s => s.Set(x => x.Tag, "v2")))
            .Version(3, m => m.Delete<H20Mig2FillRow>(x => x.Tag == "old"))
            .Version(4, m => m.TableChanged<H20Mig2FillRow>(s => s.Set(x => x.Val, r => r.Val + 1)))
            .Migrate();

        List<(int Val, string? Tag, string? Note)> stepwiseRows = Rows(stepwise);
        List<(int Val, string? Tag, string? Note)> collapsedRows = Rows(collapsed);

        Assert.Equal([(11, "v2", "n0")], stepwiseRows);
        Assert.Equal(stepwiseRows, collapsedRows);
    }

    [Fact]
    public void EarlierFillDoesNotOverwriteLaterInsertIfMissing()
    {
        using TestDatabase stepwise = new(useFile: true);
        Seed(stepwise);
        stepwise.Schema.Migrations()
            .Version(2, m => m.TableChanged<H20Mig2FillRow>(s => s.Set(x => x.Tag, "v2")))
            .Migrate();
        stepwise.Schema.Migrations()
            .Version(2, m => m.TableChanged<H20Mig2FillRow>(s => s.Set(x => x.Tag, "v2")))
            .Version(3, m => m.InsertIfMissing(x => x.Id, new H20Mig2FillRow { Id = 2, Val = 50, Tag = "ins", Note = "ni" }))
            .Migrate();
        stepwise.Schema.Migrations()
            .Version(2, m => m.TableChanged<H20Mig2FillRow>(s => s.Set(x => x.Tag, "v2")))
            .Version(3, m => m.InsertIfMissing(x => x.Id, new H20Mig2FillRow { Id = 2, Val = 50, Tag = "ins", Note = "ni" }))
            .Version(4, m => m.TableChanged<H20Mig2FillRow>(s => s.Set(x => x.Val, r => r.Val + 1)))
            .Migrate();

        using TestDatabase collapsed = new(useFile: true);
        Seed(collapsed);
        collapsed.Schema.Migrations()
            .Version(2, m => m.TableChanged<H20Mig2FillRow>(s => s.Set(x => x.Tag, "v2")))
            .Version(3, m => m.InsertIfMissing(x => x.Id, new H20Mig2FillRow { Id = 2, Val = 50, Tag = "ins", Note = "ni" }))
            .Version(4, m => m.TableChanged<H20Mig2FillRow>(s => s.Set(x => x.Val, r => r.Val + 1)))
            .Migrate();

        List<(int Val, string? Tag, string? Note)> stepwiseRows = Rows(stepwise);
        List<(int Val, string? Tag, string? Note)> collapsedRows = Rows(collapsed);

        Assert.Equal([(11, "v2", "n0"), (51, "ins", "ni")], stepwiseRows);
        Assert.Equal(stepwiseRows, collapsedRows);
    }

    [Fact]
    public void EarlierFillsAtTwoVersionsDoNotOverwriteLaterInsert()
    {
        using TestDatabase stepwise = new(useFile: true);
        Seed(stepwise);
        stepwise.Schema.Migrations()
            .Version(2, m => m.TableChanged<H20Mig2FillRow>(s => s.Set(x => x.Tag, "v2")))
            .Migrate();
        stepwise.Schema.Migrations()
            .Version(2, m => m.TableChanged<H20Mig2FillRow>(s => s.Set(x => x.Tag, "v2")))
            .Version(3, m => m.TableChanged<H20Mig2FillRow>(s => s.Set(x => x.Note, "v3")))
            .Migrate();
        stepwise.Schema.Migrations()
            .Version(2, m => m.TableChanged<H20Mig2FillRow>(s => s.Set(x => x.Tag, "v2")))
            .Version(3, m => m.TableChanged<H20Mig2FillRow>(s => s.Set(x => x.Note, "v3")))
            .Version(4, m => m.Insert(new H20Mig2FillRow { Id = 2, Val = 50, Tag = "ins", Note = "ni" }))
            .Migrate();
        stepwise.Schema.Migrations()
            .Version(2, m => m.TableChanged<H20Mig2FillRow>(s => s.Set(x => x.Tag, "v2")))
            .Version(3, m => m.TableChanged<H20Mig2FillRow>(s => s.Set(x => x.Note, "v3")))
            .Version(4, m => m.Insert(new H20Mig2FillRow { Id = 2, Val = 50, Tag = "ins", Note = "ni" }))
            .Version(5, m => m.TableChanged<H20Mig2FillRow>(s => s.Set(x => x.Val, r => r.Val + 1)))
            .Migrate();

        using TestDatabase collapsed = new(useFile: true);
        Seed(collapsed);
        collapsed.Schema.Migrations()
            .Version(2, m => m.TableChanged<H20Mig2FillRow>(s => s.Set(x => x.Tag, "v2")))
            .Version(3, m => m.TableChanged<H20Mig2FillRow>(s => s.Set(x => x.Note, "v3")))
            .Version(4, m => m.Insert(new H20Mig2FillRow { Id = 2, Val = 50, Tag = "ins", Note = "ni" }))
            .Version(5, m => m.TableChanged<H20Mig2FillRow>(s => s.Set(x => x.Val, r => r.Val + 1)))
            .Migrate();

        List<(int Val, string? Tag, string? Note)> stepwiseRows = Rows(stepwise);
        List<(int Val, string? Tag, string? Note)> collapsedRows = Rows(collapsed);

        Assert.Equal([(11, "v2", "v3"), (51, "ins", "ni")], stepwiseRows);
        Assert.Equal(stepwiseRows, collapsedRows);
    }

    [Fact]
    public void ScriptReplayDoesNotOverwriteLaterUpdate()
    {
        using TestDatabase scripted = new(useFile: true);
        Seed(scripted);
        SQLiteMigrationRunner runner = scripted.Schema.Migrations()
            .Version(2, m => m.TableChanged<H20Mig2FillRow>(s => s.Set(x => x.Tag, "v2")))
            .Version(3, m => m.Update<H20Mig2FillRow>(s => s.Set(x => x.Tag, "upd")))
            .Version(4, m => m.TableChanged<H20Mig2FillRow>(s => s.Set(x => x.Val, r => r.Val + 1)));
        IReadOnlyList<string> statements = runner.Script();

        using TestDatabase replay = new(useFile: true);
        Seed(replay);
        replay.Pragmas.UserVersion = 0;
        foreach (string statement in statements)
        {
            if (!statement.StartsWith("--"))
            {
                replay.Execute(statement);
            }
        }

        List<(int Val, string? Tag, string? Note)> replayRows = Rows(replay);

        Assert.Equal([(11, "upd", "n0")], replayRows);
    }
}
