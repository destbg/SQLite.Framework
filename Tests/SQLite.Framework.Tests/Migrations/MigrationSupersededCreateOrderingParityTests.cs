using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework;
using SQLite.Framework.Attributes;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("migord_CopyOutSource")]
public class MigOrdCopyOutSourceRow
{
    [Key]
    public int Id { get; set; }

    public string Val { get; set; } = "";
}

[Table("migord_CopyOutKeep")]
public class MigOrdCopyOutKeepRow
{
    [Key]
    public int Id { get; set; }

    public string Val { get; set; } = "";
}

[Table("migord_RawBorn")]
public class MigOrdRawBornRow
{
    [Key]
    public int Id { get; set; }
}

[Table("migord_RenamedNew")]
public class MigOrdRenamedNewRow
{
    [Key]
    public int Id { get; set; }

    public string Val { get; set; } = "";
}

public class MigrationSupersededCreateOrderingParityTests
{
    private static void SeedCopyOut(TestDatabase db)
    {
        db.Execute("CREATE TABLE \"migord_CopyOutSource\" (\"Id\" INTEGER PRIMARY KEY, \"Val\" TEXT)");
        db.Execute("CREATE TABLE \"migord_CopyOutKeep\" (\"Id\" INTEGER PRIMARY KEY, \"Val\" TEXT)");
        db.Execute("INSERT INTO \"migord_CopyOutSource\" (\"Id\", \"Val\") VALUES (1, 'a'), (2, 'b')");
        db.Pragmas.UserVersion = 1;
    }

    [Fact]
    public void RawCopyStepBeforeDropAndLaterCreateSeesTheOldRows()
    {
        using TestDatabase stepwise = new(useFile: true);
        SeedCopyOut(stepwise);
        stepwise.Schema.Migrations()
            .Version(2, m => m.Sql("INSERT INTO \"migord_CopyOutKeep\" (\"Id\", \"Val\") SELECT \"Id\", \"Val\" FROM \"migord_CopyOutSource\""))
            .Migrate();
        stepwise.Schema.Migrations()
            .Version(2, m => m.Sql("INSERT INTO \"migord_CopyOutKeep\" (\"Id\", \"Val\") SELECT \"Id\", \"Val\" FROM \"migord_CopyOutSource\""))
            .Version(3, m => m.DropTable<MigOrdCopyOutSourceRow>())
            .Migrate();
        stepwise.Schema.Migrations()
            .Version(2, m => m.Sql("INSERT INTO \"migord_CopyOutKeep\" (\"Id\", \"Val\") SELECT \"Id\", \"Val\" FROM \"migord_CopyOutSource\""))
            .Version(3, m => m.DropTable<MigOrdCopyOutSourceRow>())
            .Version(4, m => m.CreateTable<MigOrdCopyOutSourceRow>())
            .Migrate();

        using TestDatabase collapsed = new(useFile: true);
        SeedCopyOut(collapsed);
        collapsed.Schema.Migrations()
            .Version(2, m => m.Sql("INSERT INTO \"migord_CopyOutKeep\" (\"Id\", \"Val\") SELECT \"Id\", \"Val\" FROM \"migord_CopyOutSource\""))
            .Version(3, m => m.DropTable<MigOrdCopyOutSourceRow>())
            .Version(4, m => m.CreateTable<MigOrdCopyOutSourceRow>())
            .Migrate();

        List<string> stepwiseKept = stepwise.Table<MigOrdCopyOutKeepRow>().OrderBy(x => x.Id).Select(x => x.Val).ToList();
        List<string> collapsedKept = collapsed.Table<MigOrdCopyOutKeepRow>().OrderBy(x => x.Id).Select(x => x.Val).ToList();

        Assert.Equal(["a", "b"], stepwiseKept);
        Assert.Equal(stepwiseKept, collapsedKept);
    }

    [Fact]
    public void RawCreatedTableDroppedAndRecreatedAcrossVersionsMatchesStepwise()
    {
        using TestDatabase stepwise = new(useFile: true);
        stepwise.Schema.Migrations()
            .Version(1, m => m.Sql("CREATE TABLE \"migord_RawBorn\" (\"Id\" INTEGER PRIMARY KEY)"))
            .Migrate();
        stepwise.Schema.Migrations()
            .Version(1, m => m.Sql("CREATE TABLE \"migord_RawBorn\" (\"Id\" INTEGER PRIMARY KEY)"))
            .Version(2, m => m.DropTable<MigOrdRawBornRow>())
            .Migrate();
        stepwise.Schema.Migrations()
            .Version(1, m => m.Sql("CREATE TABLE \"migord_RawBorn\" (\"Id\" INTEGER PRIMARY KEY)"))
            .Version(2, m => m.DropTable<MigOrdRawBornRow>())
            .Version(3, m => m.CreateTable<MigOrdRawBornRow>())
            .Migrate();

        using TestDatabase collapsed = new(useFile: true);
        Exception? collapsedEx = Record.Exception(() => collapsed.Schema.Migrations()
            .Version(1, m => m.Sql("CREATE TABLE \"migord_RawBorn\" (\"Id\" INTEGER PRIMARY KEY)"))
            .Version(2, m => m.DropTable<MigOrdRawBornRow>())
            .Version(3, m => m.CreateTable<MigOrdRawBornRow>())
            .Migrate());

        Assert.Null(collapsedEx);
        Assert.Equal(
            stepwise.Schema.TableExists("migord_RawBorn"),
            collapsed.Schema.TableExists("migord_RawBorn"));
    }

    [Fact]
    public void RenamedRawCreatedTableDroppedAndRecreatedMatchesStepwise()
    {
        using TestDatabase stepwise = new(useFile: true);
        stepwise.Schema.Migrations()
            .Version(1, m => m.Sql("CREATE TABLE \"migord_RenamedOld\" (\"Id\" INTEGER PRIMARY KEY, \"Val\" TEXT)"))
            .Migrate();
        stepwise.Schema.Migrations()
            .Version(1, m => m.Sql("CREATE TABLE \"migord_RenamedOld\" (\"Id\" INTEGER PRIMARY KEY, \"Val\" TEXT)"))
            .Version(2, m => m.RenameTable<MigOrdRenamedNewRow>("migord_RenamedOld"))
            .Migrate();
        stepwise.Schema.Migrations()
            .Version(1, m => m.Sql("CREATE TABLE \"migord_RenamedOld\" (\"Id\" INTEGER PRIMARY KEY, \"Val\" TEXT)"))
            .Version(2, m => m.RenameTable<MigOrdRenamedNewRow>("migord_RenamedOld"))
            .Version(3, m => m.DropTable<MigOrdRenamedNewRow>())
            .Migrate();
        stepwise.Schema.Migrations()
            .Version(1, m => m.Sql("CREATE TABLE \"migord_RenamedOld\" (\"Id\" INTEGER PRIMARY KEY, \"Val\" TEXT)"))
            .Version(2, m => m.RenameTable<MigOrdRenamedNewRow>("migord_RenamedOld"))
            .Version(3, m => m.DropTable<MigOrdRenamedNewRow>())
            .Version(4, m => m.CreateTable<MigOrdRenamedNewRow>())
            .Migrate();

        using TestDatabase collapsed = new(useFile: true);
        Exception? collapsedEx = Record.Exception(() => collapsed.Schema.Migrations()
            .Version(1, m => m.Sql("CREATE TABLE \"migord_RenamedOld\" (\"Id\" INTEGER PRIMARY KEY, \"Val\" TEXT)"))
            .Version(2, m => m.RenameTable<MigOrdRenamedNewRow>("migord_RenamedOld"))
            .Version(3, m => m.DropTable<MigOrdRenamedNewRow>())
            .Version(4, m => m.CreateTable<MigOrdRenamedNewRow>())
            .Migrate());

        Assert.Null(collapsedEx);
        Assert.Equal(
            stepwise.Schema.TableExists("migord_RenamedNew"),
            collapsed.Schema.TableExists("migord_RenamedNew"));
    }
}
