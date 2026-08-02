using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("ChnASup")]
public class ChnASupRow
{
    [Key]
    public int Id { get; set; }

    public int Val { get; set; }

    public string? Note { get; set; }
}

[Table("ChnASup2")]
public class ChnASup2Row
{
    [Key]
    public int Id { get; set; }

    public int Val { get; set; }

    public string? Note { get; set; }
}

public class MigrationSupersededDropRawStepParityTests
{
    [Fact]
    public void ARawStepBetweenAReconcileAndASupersedingDropSeesTheReconciledSchema()
    {
        using TestDatabase stepwise = new(useFile: true);
        SeedSup(stepwise);
        SupChain(stepwise.Schema.Migrations(), 2).Migrate();
        SupChain(stepwise.Schema.Migrations(), 3).Migrate();
        SupChain(stepwise.Schema.Migrations(), 4).Migrate();
        SupChain(stepwise.Schema.Migrations(), 5).Migrate();
        SupChain(stepwise.Schema.Migrations(), 6).Migrate();

        List<(int Id, int Val, string? Note)> stepwiseRows = SupRows(stepwise);
        List<string> stepwiseLog = SupLog(stepwise);

        Assert.Equal([(1, 10, "seed")], stepwiseRows);
        Assert.Equal(["filled"], stepwiseLog);

        using TestDatabase collapsed = new(useFile: true);
        SeedSup(collapsed);
        Exception? collapsedEx = Record.Exception(() => SupChain(collapsed.Schema.Migrations(), 6).Migrate());

        Assert.Null(collapsedEx);
        Assert.Equal(stepwiseRows, SupRows(collapsed));
        Assert.Equal(stepwiseLog, SupLog(collapsed));
    }

    [Fact]
    public void ARunCallbackBetweenAReconcileAndASupersedingDropSeesTheReconciledSchema()
    {
        using TestDatabase stepwise = new(useFile: true);
        SeedSup2(stepwise);
        Sup2Chain(stepwise.Schema.Migrations(), 2).Migrate();
        Sup2Chain(stepwise.Schema.Migrations(), 3).Migrate();
        Sup2Chain(stepwise.Schema.Migrations(), 4).Migrate();
        Sup2Chain(stepwise.Schema.Migrations(), 5).Migrate();
        Sup2Chain(stepwise.Schema.Migrations(), 6).Migrate();

        List<(int Id, int Val, string? Note)> stepwiseRows = Sup2Rows(stepwise);
        List<string> stepwiseLog = Sup2Log(stepwise);

        Assert.Equal([(1, 10, "seed")], stepwiseRows);
        Assert.Equal(["filled"], stepwiseLog);

        using TestDatabase collapsed = new(useFile: true);
        SeedSup2(collapsed);
        Exception? collapsedEx = Record.Exception(() => Sup2Chain(collapsed.Schema.Migrations(), 6).Migrate());

        Assert.Null(collapsedEx);
        Assert.Equal(stepwiseRows, Sup2Rows(collapsed));
        Assert.Equal(stepwiseLog, Sup2Log(collapsed));
    }

    private static void SeedSup(TestDatabase db)
    {
        db.Execute("CREATE TABLE \"ChnASup\" (\"Id\" INTEGER PRIMARY KEY, \"Val\" INTEGER)");
        db.Execute("INSERT INTO \"ChnASup\" (\"Id\", \"Val\") VALUES (1, 10)");
        db.Execute("CREATE TABLE \"ChnASupLog\" (\"Id\" INTEGER, \"Note\" TEXT)");
        db.Pragmas.UserVersion = 1;
    }

    private static SQLiteMigrationRunner SupChain(SQLiteMigrationRunner runner, int upTo)
    {
        runner.Version(2, m => m.TableChanged<ChnASupRow>(s => s.Set(x => x.Note, "filled")));
        if (upTo >= 3)
        {
            runner.Version(3, m => m.Sql("INSERT INTO \"ChnASupLog\" (\"Id\", \"Note\") SELECT \"Id\", \"Note\" FROM \"ChnASup\""));
        }

        if (upTo >= 4)
        {
            runner.Version(4, m => m.DropTable<ChnASupRow>());
        }

        if (upTo >= 5)
        {
            runner.Version(5, m => m.CreateTable<ChnASupRow>());
        }

        if (upTo >= 6)
        {
            runner.Version(6, m => m.InsertIfMissing(x => x.Id, new ChnASupRow { Id = 1, Val = 10, Note = "seed" }));
        }

        return runner;
    }

    private static List<(int Id, int Val, string? Note)> SupRows(TestDatabase db)
    {
        return db.Table<ChnASupRow>()
            .OrderBy(x => x.Id)
            .Select(x => new { x.Id, x.Val, x.Note })
            .ToList()
            .Select(x => (x.Id, x.Val, x.Note))
            .ToList();
    }

    private static List<string> SupLog(TestDatabase db)
    {
        return db.Query<string>("SELECT \"Note\" FROM \"ChnASupLog\" ORDER BY \"rowid\"");
    }

    private static void SeedSup2(TestDatabase db)
    {
        db.Execute("CREATE TABLE \"ChnASup2\" (\"Id\" INTEGER PRIMARY KEY, \"Val\" INTEGER)");
        db.Execute("INSERT INTO \"ChnASup2\" (\"Id\", \"Val\") VALUES (1, 10)");
        db.Execute("CREATE TABLE \"ChnASup2Log\" (\"Id\" INTEGER, \"Note\" TEXT)");
        db.Pragmas.UserVersion = 1;
    }

    private static SQLiteMigrationRunner Sup2Chain(SQLiteMigrationRunner runner, int upTo)
    {
        runner.Version(2, m => m.TableChanged<ChnASup2Row>(s => s.Set(x => x.Note, "filled")));
        if (upTo >= 3)
        {
            runner.Version(3, m => m.Run(ctx => ctx.Database.Execute(
                "INSERT INTO \"ChnASup2Log\" (\"Id\", \"Note\") SELECT \"Id\", \"Note\" FROM \"ChnASup2\"")));
        }

        if (upTo >= 4)
        {
            runner.Version(4, m => m.DropTable<ChnASup2Row>());
        }

        if (upTo >= 5)
        {
            runner.Version(5, m => m.CreateTable<ChnASup2Row>());
        }

        if (upTo >= 6)
        {
            runner.Version(6, m => m.InsertIfMissing(x => x.Id, new ChnASup2Row { Id = 1, Val = 10, Note = "seed" }));
        }

        return runner;
    }

    private static List<(int Id, int Val, string? Note)> Sup2Rows(TestDatabase db)
    {
        return db.Table<ChnASup2Row>()
            .OrderBy(x => x.Id)
            .Select(x => new { x.Id, x.Val, x.Note })
            .ToList()
            .Select(x => (x.Id, x.Val, x.Note))
            .ToList();
    }

    private static List<string> Sup2Log(TestDatabase db)
    {
        return db.Query<string>("SELECT \"Note\" FROM \"ChnASup2Log\" ORDER BY \"rowid\"");
    }
}
