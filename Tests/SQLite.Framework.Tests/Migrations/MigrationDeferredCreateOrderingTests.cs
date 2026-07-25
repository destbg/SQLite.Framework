using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H20Mig_DeferredCreate")]
public class H20MigDeferredCreateRow
{
    [Key]
    public int Id { get; set; }

    public int Val { get; set; }
}

public class MigrationDeferredCreateOrderingTests
{
    [Fact]
    public void InsertAfterDropBeforeDeferredCreateIsKept()
    {
        using TestDatabase db = new(useFile: true);
        db.Schema.Migrations()
            .Version(1, m => m.CreateTable<H20MigDeferredCreateRow>())
            .Version(2, m => m.Run(_ => { }))
            .Version(3, m => m
                .DropTable<H20MigDeferredCreateRow>()
                .Insert(new H20MigDeferredCreateRow { Id = 1, Val = 7 }))
            .Version(4, m => m.CreateTable<H20MigDeferredCreateRow>())
            .Migrate();

        List<int> vals = db.Table<H20MigDeferredCreateRow>().OrderBy(x => x.Id).Select(x => x.Val).ToList();

        Assert.Equal([7], vals);
    }

    [Fact]
    public void RunCallbackDoesNotChangeDropRecreateOutcome()
    {
        using TestDatabase withRun = new(useFile: true);
        withRun.Schema.Migrations()
            .Version(1, m => m.CreateTable<H20MigDeferredCreateRow>())
            .Version(2, m => m.Run(_ => { }))
            .Version(3, m => m
                .DropTable<H20MigDeferredCreateRow>()
                .Insert(new H20MigDeferredCreateRow { Id = 1, Val = 7 }))
            .Version(4, m => m.CreateTable<H20MigDeferredCreateRow>())
            .Migrate();

        using TestDatabase withoutRun = new(useFile: true);
        withoutRun.Schema.Migrations()
            .Version(1, m => m.CreateTable<H20MigDeferredCreateRow>())
            .Version(3, m => m
                .DropTable<H20MigDeferredCreateRow>()
                .Insert(new H20MigDeferredCreateRow { Id = 1, Val = 7 }))
            .Version(4, m => m.CreateTable<H20MigDeferredCreateRow>())
            .Migrate();

        List<int> withRunVals = withRun.Table<H20MigDeferredCreateRow>().OrderBy(x => x.Id).Select(x => x.Val).ToList();
        List<int> withoutRunVals = withoutRun.Table<H20MigDeferredCreateRow>().OrderBy(x => x.Id).Select(x => x.Val).ToList();

        Assert.Equal([7], withoutRunVals);
        Assert.Equal(withoutRunVals, withRunVals);
    }

    [Fact]
    public void InsertAtCreateVersionBeforeCreateIsKept()
    {
        using TestDatabase db = new(useFile: true);
        db.Schema.Migrations()
            .Version(1, m => m.CreateTable<H20MigDeferredCreateRow>())
            .Version(2, m => m.Run(_ => { }))
            .Version(3, m => m.DropTable<H20MigDeferredCreateRow>())
            .Version(4, m => m
                .Insert(new H20MigDeferredCreateRow { Id = 1, Val = 7 })
                .CreateTable<H20MigDeferredCreateRow>())
            .Migrate();

        List<int> vals = db.Table<H20MigDeferredCreateRow>().OrderBy(x => x.Id).Select(x => x.Val).ToList();

        Assert.Equal([7], vals);
    }
}
