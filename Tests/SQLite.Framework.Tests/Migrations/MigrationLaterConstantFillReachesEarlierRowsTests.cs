using System.Collections.Generic;
using System.Linq;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("mig2_LaterFillInsert")]
public class Mig2LaterFillInsertRow
{
    [Key]
    public int Id { get; set; }

    public int Val { get; set; }
}

[Table("mig2_LaterFillUpdate")]
public class Mig2LaterFillUpdateRow
{
    [Key]
    public int Id { get; set; }

    public int Val { get; set; }
}

[Table("mig2_LaterFillRun")]
public class Mig2LaterFillRunRow
{
    [Key]
    public int Id { get; set; }

    public int Src { get; set; }

    public int Dst { get; set; }
}

public class MigrationLaterConstantFillReachesEarlierRowsTests
{
    [Fact]
    public void ConstantFillReachesRowInsertedByEarlierVersion()
    {
        List<(int, int)> collapsed;
        using (TestDatabase db = new(useFile: true))
        {
            db.Execute("CREATE TABLE \"mig2_LaterFillInsert\" (\"Id\" INTEGER PRIMARY KEY, \"Val\" INTEGER)");
            db.Execute("INSERT INTO \"mig2_LaterFillInsert\" (\"Id\", \"Val\") VALUES (1, 1)");
            db.Pragmas.UserVersion = 1;
            db.Schema.Migrations()
                .Version(2, m => m.Insert(new Mig2LaterFillInsertRow { Id = 2, Val = 2 }))
                .Version(3, m => m.TableChanged<Mig2LaterFillInsertRow>(s => s.Set(r => r.Val, 999)))
                .Migrate();
            collapsed = db.Table<Mig2LaterFillInsertRow>().OrderBy(r => r.Id).Select(r => new { r.Id, r.Val }).ToList()
                .Select(x => (x.Id, x.Val)).ToList();
        }

        List<(int, int)> stepwise;
        using (TestDatabase db = new(useFile: true))
        {
            db.Execute("CREATE TABLE \"mig2_LaterFillInsert\" (\"Id\" INTEGER PRIMARY KEY, \"Val\" INTEGER)");
            db.Execute("INSERT INTO \"mig2_LaterFillInsert\" (\"Id\", \"Val\") VALUES (1, 1)");
            db.Pragmas.UserVersion = 1;
            db.Schema.Migrations()
                .Version(2, m => m.Insert(new Mig2LaterFillInsertRow { Id = 2, Val = 2 }))
                .Migrate();
            db.Schema.Migrations()
                .Version(2, m => m.Insert(new Mig2LaterFillInsertRow { Id = 2, Val = 2 }))
                .Version(3, m => m.TableChanged<Mig2LaterFillInsertRow>(s => s.Set(r => r.Val, 999)))
                .Migrate();
            stepwise = db.Table<Mig2LaterFillInsertRow>().OrderBy(r => r.Id).Select(r => new { r.Id, r.Val }).ToList()
                .Select(x => (x.Id, x.Val)).ToList();
        }

        Assert.Equal(new List<(int, int)> { (1, 999), (2, 999) }, stepwise);
        Assert.Equal(stepwise, collapsed);
    }

    [Fact]
    public void ConstantFillOverridesValueSetByEarlierUpdate()
    {
        int collapsed;
        using (TestDatabase db = new(useFile: true))
        {
            db.Execute("CREATE TABLE \"mig2_LaterFillUpdate\" (\"Id\" INTEGER PRIMARY KEY, \"Val\" INTEGER)");
            db.Execute("INSERT INTO \"mig2_LaterFillUpdate\" (\"Id\", \"Val\") VALUES (1, 1)");
            db.Pragmas.UserVersion = 1;
            db.Schema.Migrations()
                .Version(2, m => m.Update<Mig2LaterFillUpdateRow>(x => x.Id == 1, s => s.Set(r => r.Val, 50)))
                .Version(3, m => m.TableChanged<Mig2LaterFillUpdateRow>(s => s.Set(r => r.Val, 999)))
                .Migrate();
            collapsed = db.Table<Mig2LaterFillUpdateRow>().Single().Val;
        }

        int stepwise;
        using (TestDatabase db = new(useFile: true))
        {
            db.Execute("CREATE TABLE \"mig2_LaterFillUpdate\" (\"Id\" INTEGER PRIMARY KEY, \"Val\" INTEGER)");
            db.Execute("INSERT INTO \"mig2_LaterFillUpdate\" (\"Id\", \"Val\") VALUES (1, 1)");
            db.Pragmas.UserVersion = 1;
            db.Schema.Migrations()
                .Version(2, m => m.Update<Mig2LaterFillUpdateRow>(x => x.Id == 1, s => s.Set(r => r.Val, 50)))
                .Migrate();
            db.Schema.Migrations()
                .Version(2, m => m.Update<Mig2LaterFillUpdateRow>(x => x.Id == 1, s => s.Set(r => r.Val, 50)))
                .Version(3, m => m.TableChanged<Mig2LaterFillUpdateRow>(s => s.Set(r => r.Val, 999)))
                .Migrate();
            stepwise = db.Table<Mig2LaterFillUpdateRow>().Single().Val;
        }

        Assert.Equal(999, stepwise);
        Assert.Equal(stepwise, collapsed);
    }

    [Fact]
    public void EarlierRunCallbackDoesNotSeeLaterVersionFill()
    {
        int collapsed;
        using (TestDatabase db = new(useFile: true))
        {
            db.Execute("CREATE TABLE \"mig2_LaterFillRun\" (\"Id\" INTEGER PRIMARY KEY, \"Src\" INTEGER, \"Dst\" INTEGER NOT NULL DEFAULT 0)");
            db.Execute("INSERT INTO \"mig2_LaterFillRun\" (\"Id\", \"Src\", \"Dst\") VALUES (1, 7, 0)");
            db.Pragmas.UserVersion = 1;
            db.Schema.Migrations()
                .Version(2, m => m.Run(ctx => ctx.Database.Execute("UPDATE \"mig2_LaterFillRun\" SET \"Dst\" = \"Src\"")))
                .Version(3, m => m.TableChanged<Mig2LaterFillRunRow>(s => s.Set(r => r.Src, 100)))
                .Migrate();
            collapsed = db.Table<Mig2LaterFillRunRow>().Single().Dst;
        }

        int stepwise;
        using (TestDatabase db = new(useFile: true))
        {
            db.Execute("CREATE TABLE \"mig2_LaterFillRun\" (\"Id\" INTEGER PRIMARY KEY, \"Src\" INTEGER, \"Dst\" INTEGER NOT NULL DEFAULT 0)");
            db.Execute("INSERT INTO \"mig2_LaterFillRun\" (\"Id\", \"Src\", \"Dst\") VALUES (1, 7, 0)");
            db.Pragmas.UserVersion = 1;
            db.Schema.Migrations()
                .Version(2, m => m.Run(ctx => ctx.Database.Execute("UPDATE \"mig2_LaterFillRun\" SET \"Dst\" = \"Src\"")))
                .Migrate();
            db.Schema.Migrations()
                .Version(2, m => m.Run(ctx => ctx.Database.Execute("UPDATE \"mig2_LaterFillRun\" SET \"Dst\" = \"Src\"")))
                .Version(3, m => m.TableChanged<Mig2LaterFillRunRow>(s => s.Set(r => r.Src, 100)))
                .Migrate();
            stepwise = db.Table<Mig2LaterFillRunRow>().Single().Dst;
        }

        Assert.Equal(7, stepwise);
        Assert.Equal(stepwise, collapsed);
    }
}
