using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("migord_DropRejoin")]
public class MigOrdDropRejoinRow
{
    [Key]
    public int Id { get; set; }

    public string Note { get; set; } = "";
}

public class MigrationDropThenTableChangedParityTests
{
    [Fact]
    public void DropAtOneVersionThenTableChangedAtNextMatchesStepwise()
    {
        using TestDatabase stepwise = new(useFile: true);
        stepwise.Schema.Migrations()
            .Version(1, m => m.CreateTable<MigOrdDropRejoinRow>())
            .Migrate();
        stepwise.Schema.Migrations()
            .Version(1, m => m.CreateTable<MigOrdDropRejoinRow>())
            .Version(2, m => m.DropTable<MigOrdDropRejoinRow>())
            .Migrate();
        stepwise.Schema.Migrations()
            .Version(1, m => m.CreateTable<MigOrdDropRejoinRow>())
            .Version(2, m => m.DropTable<MigOrdDropRejoinRow>())
            .Version(3, m => m.TableChanged<MigOrdDropRejoinRow>())
            .Migrate();

        using TestDatabase collapsed = new(useFile: true);
        collapsed.Schema.Migrations()
            .Version(1, m => m.CreateTable<MigOrdDropRejoinRow>())
            .Migrate();
        collapsed.Schema.Migrations()
            .Version(1, m => m.CreateTable<MigOrdDropRejoinRow>())
            .Version(2, m => m.DropTable<MigOrdDropRejoinRow>())
            .Version(3, m => m.TableChanged<MigOrdDropRejoinRow>())
            .Migrate();

        bool stepwiseExists = stepwise.Schema.TableExists("migord_DropRejoin");
        bool collapsedExists = collapsed.Schema.TableExists("migord_DropRejoin");

        Assert.True(stepwiseExists);
        Assert.Equal(stepwiseExists, collapsedExists);
    }

    [Fact]
    public void DropThenTableChangedWithSeedRowMatchesStepwise()
    {
        using TestDatabase stepwise = new(useFile: true);
        stepwise.Schema.Migrations()
            .Version(1, m => m.CreateTable<MigOrdDropRejoinRow>())
            .Migrate();
        stepwise.Schema.Migrations()
            .Version(1, m => m.CreateTable<MigOrdDropRejoinRow>())
            .Version(2, m => m.DropTable<MigOrdDropRejoinRow>())
            .Migrate();
        stepwise.Schema.Migrations()
            .Version(1, m => m.CreateTable<MigOrdDropRejoinRow>())
            .Version(2, m => m.DropTable<MigOrdDropRejoinRow>())
            .Version(3, m => m
                .TableChanged<MigOrdDropRejoinRow>()
                .Insert(new MigOrdDropRejoinRow { Id = 5, Note = "back" }))
            .Migrate();

        using TestDatabase collapsed = new(useFile: true);
        collapsed.Schema.Migrations()
            .Version(1, m => m.CreateTable<MigOrdDropRejoinRow>())
            .Migrate();
        Exception? collapsedEx = Record.Exception(() => collapsed.Schema.Migrations()
            .Version(1, m => m.CreateTable<MigOrdDropRejoinRow>())
            .Version(2, m => m.DropTable<MigOrdDropRejoinRow>())
            .Version(3, m => m
                .TableChanged<MigOrdDropRejoinRow>()
                .Insert(new MigOrdDropRejoinRow { Id = 5, Note = "back" }))
            .Migrate());

        Assert.Null(collapsedEx);

        string stepwiseNote = stepwise.Table<MigOrdDropRejoinRow>().Single().Note;
        string collapsedNote = collapsed.Table<MigOrdDropRejoinRow>().Single().Note;

        Assert.Equal("back", stepwiseNote);
        Assert.Equal(stepwiseNote, collapsedNote);
    }
}
