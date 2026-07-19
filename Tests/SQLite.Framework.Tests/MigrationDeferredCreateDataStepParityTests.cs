using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H20Mig2_DeferredCreate")]
public class H20Mig2CreateRow
{
    [Key]
    public int Id { get; set; }

    public int Val { get; set; }
}

[Table("H20Mig2_DeferredCreateSecond")]
public class H20Mig2CreateSecondRow
{
    [Key]
    public int Id { get; set; }

    public int Val { get; set; }
}

public class MigrationDeferredCreateDataStepParityTests
{
    [Fact]
    public void RawSqlOpaqueStepKeepsRowsWrittenBeforeCreate()
    {
        using TestDatabase db = new(useFile: true);
        db.Schema.Migrations()
            .Version(1, m => m.CreateTable<H20Mig2CreateRow>())
            .Version(2, m => m.Sql("SELECT 1"))
            .Version(3, m => m
                .DropTable<H20Mig2CreateRow>()
                .Insert(new H20Mig2CreateRow { Id = 1, Val = 7 }))
            .Version(4, m => m.CreateTable<H20Mig2CreateRow>())
            .Migrate();

        List<int> vals = db.Table<H20Mig2CreateRow>().OrderBy(x => x.Id).Select(x => x.Val).ToList();

        Assert.Equal([7], vals);
    }

    [Fact]
    public void UpdateBetweenDropAndCreateIsKept()
    {
        using TestDatabase db = new(useFile: true);
        db.Schema.Migrations()
            .Version(1, m => m.CreateTable<H20Mig2CreateRow>())
            .Version(2, m => m.Run(_ => { }))
            .Version(3, m => m
                .DropTable<H20Mig2CreateRow>()
                .Insert(new H20Mig2CreateRow { Id = 1, Val = 7 })
                .Update<H20Mig2CreateRow>(s => s.Set(x => x.Val, 99)))
            .Version(4, m => m.CreateTable<H20Mig2CreateRow>())
            .Migrate();

        List<int> vals = db.Table<H20Mig2CreateRow>().OrderBy(x => x.Id).Select(x => x.Val).ToList();

        Assert.Equal([99], vals);
    }

    [Fact]
    public void DeleteBetweenDropAndCreateIsKept()
    {
        using TestDatabase db = new(useFile: true);
        db.Schema.Migrations()
            .Version(1, m => m.CreateTable<H20Mig2CreateRow>())
            .Version(2, m => m.Run(_ => { }))
            .Version(3, m => m
                .DropTable<H20Mig2CreateRow>()
                .Insert(new H20Mig2CreateRow { Id = 1, Val = 7 }, new H20Mig2CreateRow { Id = 2, Val = 8 })
                .Delete<H20Mig2CreateRow>(x => x.Id == 1))
            .Version(4, m => m.CreateTable<H20Mig2CreateRow>())
            .Migrate();

        List<int> vals = db.Table<H20Mig2CreateRow>().OrderBy(x => x.Id).Select(x => x.Val).ToList();

        Assert.Equal([8], vals);
    }

    [Fact]
    public void CreateAtMiddleVersionKeepsRowsFromBothSides()
    {
        using TestDatabase db = new(useFile: true);
        db.Schema.Migrations()
            .Version(1, m => m.CreateTable<H20Mig2CreateRow>())
            .Version(2, m => m.Run(_ => { }))
            .Version(3, m => m
                .DropTable<H20Mig2CreateRow>()
                .Insert(new H20Mig2CreateRow { Id = 1, Val = 7 }))
            .Version(4, m => m.CreateTable<H20Mig2CreateRow>())
            .Version(5, m => m.Insert(new H20Mig2CreateRow { Id = 2, Val = 8 }))
            .Migrate();

        List<int> vals = db.Table<H20Mig2CreateRow>().OrderBy(x => x.Id).Select(x => x.Val).ToList();

        Assert.Equal([7, 8], vals);
    }

    [Fact]
    public void MultipleDeferredCreatesKeepTheirRows()
    {
        using TestDatabase db = new(useFile: true);
        db.Schema.Migrations()
            .Version(1, m => m.CreateTable<H20Mig2CreateRow>().CreateTable<H20Mig2CreateSecondRow>())
            .Version(2, m => m.Run(_ => { }))
            .Version(3, m => m
                .DropTable<H20Mig2CreateRow>()
                .Insert(new H20Mig2CreateRow { Id = 1, Val = 7 })
                .DropTable<H20Mig2CreateSecondRow>()
                .Insert(new H20Mig2CreateSecondRow { Id = 1, Val = 70 }))
            .Version(4, m => m.CreateTable<H20Mig2CreateRow>().CreateTable<H20Mig2CreateSecondRow>())
            .Migrate();

        List<int> vals = db.Table<H20Mig2CreateRow>().OrderBy(x => x.Id).Select(x => x.Val).ToList();
        List<int> secondVals = db.Table<H20Mig2CreateSecondRow>().OrderBy(x => x.Id).Select(x => x.Val).ToList();

        Assert.Equal([7], vals);
        Assert.Equal([70], secondVals);
    }

    [Fact]
    public void ScriptReplayKeepsRowsWrittenBeforeCreate()
    {
        using TestDatabase scripted = new(useFile: true);
        SQLiteMigrationRunner runner = scripted.Schema.Migrations()
            .Version(1, m => m.CreateTable<H20Mig2CreateRow>())
            .Version(2, m => m.Run(_ => { }))
            .Version(3, m => m
                .DropTable<H20Mig2CreateRow>()
                .Insert(new H20Mig2CreateRow { Id = 1, Val = 7 }))
            .Version(4, m => m.CreateTable<H20Mig2CreateRow>());
        IReadOnlyList<string> statements = runner.Script();

        using TestDatabase replay = new(useFile: true);
        foreach (string statement in statements)
        {
            if (!statement.StartsWith("--"))
            {
                replay.Execute(statement);
            }
        }

        List<int> vals = replay.Table<H20Mig2CreateRow>().OrderBy(x => x.Id).Select(x => x.Val).ToList();

        Assert.Equal([7], vals);
    }
}
