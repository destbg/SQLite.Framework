using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("ChnARenCol")]
public class ChnARenColRow
{
    [Key]
    public int Id { get; set; }

    public int Val { get; set; }

    public string? Note { get; set; }
}

[Table("ChnANewTab")]
public class ChnANewTabRow
{
    [Key]
    public int Id { get; set; }

    public int Val { get; set; }
}

[Table("ChnAColTab")]
public class ChnAColTabRow
{
    [Key]
    public int Id { get; set; }

    public string? Name { get; set; }

    [Column("New")]
    public string? Renamed { get; set; }
}

public class MigrationRenameAcrossRawStepParityTests
{
    [Fact]
    public void RenameColumnOfAColumnAnEarlierRawStepAddsMatchesStepwise()
    {
        using TestDatabase stepwise = new(useFile: true);
        SeedRenCol(stepwise);
        RenColChain(stepwise.Schema.Migrations(), 2).Migrate();
        RenColChain(stepwise.Schema.Migrations(), 3).Migrate();
        RenColChain(stepwise.Schema.Migrations(), 4).Migrate();
        RenColChain(stepwise.Schema.Migrations(), 5).Migrate();
        RenColChain(stepwise.Schema.Migrations(), 6).Migrate();

        using TestDatabase collapsed = new(useFile: true);
        SeedRenCol(collapsed);
        RenColChain(collapsed.Schema.Migrations(), 6).Migrate();

        List<(int Id, int Val, string? Note)> stepwiseRows = RenColRows(stepwise);
        List<(int Id, int Val, string? Note)> collapsedRows = RenColRows(collapsed);
        List<string> stepwiseColumns = ColumnNames(stepwise, "ChnARenCol");
        List<string> collapsedColumns = ColumnNames(collapsed, "ChnARenCol");

        Assert.Equal([(1, 10, "kept"), (2, 20, null)], stepwiseRows);
        Assert.Equal(["Id", "Val", "Note"], stepwiseColumns);
        Assert.Equal(stepwiseRows, collapsedRows);
        Assert.Equal(stepwiseColumns, collapsedColumns);
    }

    [Fact]
    public void RenameTableAfterARawStepThatReadsTheOldNameMatchesStepwise()
    {
        using TestDatabase stepwise = new(useFile: true);
        SeedNewTab(stepwise);
        NewTabChain(stepwise.Schema.Migrations(), 2).Migrate();
        NewTabChain(stepwise.Schema.Migrations(), 3).Migrate();
        NewTabChain(stepwise.Schema.Migrations(), 4).Migrate();
        NewTabChain(stepwise.Schema.Migrations(), 5).Migrate();
        NewTabChain(stepwise.Schema.Migrations(), 6).Migrate();

        List<long> stepwiseLog = LogValues(stepwise, "ChnAOldLog");
        int stepwiseVal = stepwise.Table<ChnANewTabRow>().Single().Val;

        Assert.Equal([10L, 11L], stepwiseLog);
        Assert.Equal(11, stepwiseVal);

        using TestDatabase collapsed = new(useFile: true);
        SeedNewTab(collapsed);
        Exception? collapsedEx = Record.Exception(() => NewTabChain(collapsed.Schema.Migrations(), 6).Migrate());

        Assert.Null(collapsedEx);
        Assert.Equal(stepwiseLog, LogValues(collapsed, "ChnAOldLog"));
        Assert.Equal(stepwiseVal, collapsed.Table<ChnANewTabRow>().Single().Val);
    }

    [Fact]
    public void RenameColumnAfterARawStepThatReadsTheOldColumnMatchesStepwise()
    {
        using TestDatabase stepwise = new(useFile: true);
        SeedColTab(stepwise);
        ColTabChain(stepwise.Schema.Migrations(), 2).Migrate();
        ColTabChain(stepwise.Schema.Migrations(), 3).Migrate();
        ColTabChain(stepwise.Schema.Migrations(), 4).Migrate();
        ColTabChain(stepwise.Schema.Migrations(), 5).Migrate();
        ColTabChain(stepwise.Schema.Migrations(), 6).Migrate();

        List<string> stepwiseLog = LogText(stepwise, "ChnAColLog");
        string? stepwiseRenamed = stepwise.Table<ChnAColTabRow>().Single().Renamed;

        Assert.Equal(["a", "a!"], stepwiseLog);
        Assert.Equal("a!", stepwiseRenamed);

        using TestDatabase collapsed = new(useFile: true);
        SeedColTab(collapsed);
        Exception? collapsedEx = Record.Exception(() => ColTabChain(collapsed.Schema.Migrations(), 6).Migrate());

        Assert.Null(collapsedEx);
        Assert.Equal(stepwiseLog, LogText(collapsed, "ChnAColLog"));
        Assert.Equal(stepwiseRenamed, collapsed.Table<ChnAColTabRow>().Single().Renamed);
    }

    private static void SeedRenCol(TestDatabase db)
    {
        db.Execute("CREATE TABLE \"ChnARenCol\" (\"Id\" INTEGER PRIMARY KEY, \"Val\" INTEGER)");
        db.Execute("INSERT INTO \"ChnARenCol\" (\"Id\", \"Val\") VALUES (1, 10)");
        db.Pragmas.UserVersion = 1;
    }

    private static SQLiteMigrationRunner RenColChain(SQLiteMigrationRunner runner, int upTo)
    {
        runner.Version(2, m => m.Sql("ALTER TABLE \"ChnARenCol\" ADD COLUMN \"Tmp\" TEXT"));
        if (upTo >= 3)
        {
            runner.Version(3, m => m.Sql("UPDATE \"ChnARenCol\" SET \"Tmp\" = 'kept'"));
        }

        if (upTo >= 4)
        {
            runner.Version(4, m => m.RenameColumn<ChnARenColRow>("Tmp", "Note"));
        }

        if (upTo >= 5)
        {
            runner.Version(5, m => m.InsertIfMissing(x => x.Id, new ChnARenColRow { Id = 2, Val = 20 }));
        }

        if (upTo >= 6)
        {
            runner.Version(6, m => m.TableChanged<ChnARenColRow>());
        }

        return runner;
    }

    private static List<(int Id, int Val, string? Note)> RenColRows(TestDatabase db)
    {
        return db.Table<ChnARenColRow>()
            .OrderBy(x => x.Id)
            .Select(x => new { x.Id, x.Val, x.Note })
            .ToList()
            .Select(x => (x.Id, x.Val, x.Note))
            .ToList();
    }

    private static void SeedNewTab(TestDatabase db)
    {
        db.Execute("CREATE TABLE \"ChnAOldTab\" (\"Id\" INTEGER PRIMARY KEY, \"Val\" INTEGER)");
        db.Execute("INSERT INTO \"ChnAOldTab\" (\"Id\", \"Val\") VALUES (1, 10)");
        db.Execute("CREATE TABLE \"ChnAOldLog\" (\"Id\" INTEGER, \"Val\" INTEGER)");
        db.Pragmas.UserVersion = 1;
    }

    private static SQLiteMigrationRunner NewTabChain(SQLiteMigrationRunner runner, int upTo)
    {
        runner.Version(2, m => m.Sql("INSERT INTO \"ChnAOldLog\" (\"Id\", \"Val\") SELECT \"Id\", \"Val\" FROM \"ChnAOldTab\""));
        if (upTo >= 3)
        {
            runner.Version(3, m => m.Sql("UPDATE \"ChnAOldTab\" SET \"Val\" = \"Val\" + 1"));
        }

        if (upTo >= 4)
        {
            runner.Version(4, m => m.Sql("INSERT INTO \"ChnAOldLog\" (\"Id\", \"Val\") SELECT \"Id\", \"Val\" FROM \"ChnAOldTab\""));
        }

        if (upTo >= 5)
        {
            runner.Version(5, m => m.RenameTable<ChnANewTabRow>("ChnAOldTab"));
        }

        if (upTo >= 6)
        {
            runner.Version(6, m => m.TableChanged<ChnANewTabRow>());
        }

        return runner;
    }

    private static void SeedColTab(TestDatabase db)
    {
        db.Execute("CREATE TABLE \"ChnAColTab\" (\"Id\" INTEGER PRIMARY KEY, \"Name\" TEXT, \"Old\" TEXT)");
        db.Execute("INSERT INTO \"ChnAColTab\" (\"Id\", \"Name\", \"Old\") VALUES (1, 'n', 'a')");
        db.Execute("CREATE TABLE \"ChnAColLog\" (\"Id\" INTEGER, \"Val\" TEXT)");
        db.Pragmas.UserVersion = 1;
    }

    private static SQLiteMigrationRunner ColTabChain(SQLiteMigrationRunner runner, int upTo)
    {
        runner.Version(2, m => m.Sql("INSERT INTO \"ChnAColLog\" (\"Id\", \"Val\") SELECT \"Id\", \"Old\" FROM \"ChnAColTab\""));
        if (upTo >= 3)
        {
            runner.Version(3, m => m.Sql("UPDATE \"ChnAColTab\" SET \"Old\" = \"Old\" || '!'"));
        }

        if (upTo >= 4)
        {
            runner.Version(4, m => m.Sql("INSERT INTO \"ChnAColLog\" (\"Id\", \"Val\") SELECT \"Id\", \"Old\" FROM \"ChnAColTab\""));
        }

        if (upTo >= 5)
        {
            runner.Version(5, m => m.RenameColumn<ChnAColTabRow>("Old", "New"));
        }

        if (upTo >= 6)
        {
            runner.Version(6, m => m.TableChanged<ChnAColTabRow>());
        }

        return runner;
    }

    private static List<string> ColumnNames(TestDatabase db, string table)
    {
        return db.Pragmas.TableInfo(table).Select(c => c.Name).ToList();
    }

    private static List<long> LogValues(TestDatabase db, string table)
    {
        return db.Query<long>($"SELECT \"Val\" FROM \"{table}\" ORDER BY \"rowid\"");
    }

    private static List<string> LogText(TestDatabase db, string table)
    {
        return db.Query<string>($"SELECT \"Val\" FROM \"{table}\" ORDER BY \"rowid\"");
    }
}
