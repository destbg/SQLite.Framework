using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework;
using SQLite.Framework.Attributes;
using SQLite.Framework.Enums;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("ChnDeferredRows")]
public class ChnDeferredRow
{
    [Key]
    public int Id { get; set; }

    public int Val { get; set; }

    public string? Note { get; set; }
}

public class MigrationDeferredRebuildRowOpTests
{
    [Fact]
    public void ADeferredRebuildAddsANewColumnBeforeTheEarlierUpdate()
    {
        using TestDatabase stepwise = new(useFile: true);
        Seed(stepwise);
        Chain(stepwise.Schema.Migrations(), 2).Migrate();
        Chain(stepwise.Schema.Migrations(), 3).Migrate();

        using TestDatabase collapsed = new(useFile: true);
        Seed(collapsed);
        Chain(collapsed.Schema.Migrations(), 3).Migrate();

        List<(int Val, string? Note)> stepwiseRows = Rows(stepwise);
        List<(int Val, string? Note)> collapsedRows = Rows(collapsed);

        Assert.Equal([(12, null)], stepwiseRows);
        Assert.Equal(stepwiseRows, collapsedRows);
    }

    [Fact]
    public void ANewNotNullColumnWithoutADefaultFailsTheDeferredRebuildWithGuidance()
    {
        using TestDatabase db = new(useFile: true);
        db.Execute("CREATE TABLE \"ChnStrictAdd\" (\"Id\" INTEGER PRIMARY KEY, \"Name\" TEXT, \"Val\" INTEGER NOT NULL)");
        db.Execute("INSERT INTO \"ChnStrictAdd\" (\"Id\", \"Name\", \"Val\") VALUES (1, NULL, 10)");
        db.Pragmas.UserVersion = 1;

        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() => db.Schema.Migrations()
            .Version(2, m => m.Update<ChnStrictAddRow>(s => s.Set(x => x.Val, 20)))
            .Version(3, m => m.TableChanged<ChnStrictAddRow>(s => s.Set(x => x.Name, r => r.Name ?? "x")))
            .Migrate());

        Assert.Contains("Total", ex.Message);
    }

    [Fact]
    public void ANewColumnWithAnExpressionDefaultBackfillsDuringTheDeferredRebuild()
    {
        using ModelTestDatabase stepwise = new(model => model.Entity<ChnExprRow>()
            .Default(r => r.Stamp, () => SQLiteFunctions.SqliteVersion())
            .Default(r => r.Note, "n")
            .Default(r => r.Total, 7));
        SeedExpr(stepwise);
        ExprChain(stepwise.Schema.Migrations(), 2).Migrate();
        ExprChain(stepwise.Schema.Migrations(), 3).Migrate();

        using ModelTestDatabase collapsed = new(model => model.Entity<ChnExprRow>()
            .Default(r => r.Stamp, () => SQLiteFunctions.SqliteVersion())
            .Default(r => r.Note, "n")
            .Default(r => r.Total, 7));
        SeedExpr(collapsed);
        ExprChain(collapsed.Schema.Migrations(), 3).Migrate();

        List<(string Name, int Val, string? Stamp, string? Note, int Total)> stepwiseRows = ExprRows(stepwise);
        List<(string Name, int Val, string? Stamp, string? Note, int Total)> collapsedRows = ExprRows(collapsed);

        string version = stepwise.ExecuteScalar<string>("SELECT sqlite_version()")!;
        Assert.Equal([("x", 20, version, "n", 7)], stepwiseRows);
        Assert.Equal(stepwiseRows, collapsedRows);
    }

    [Fact]
    public void AnEarlyOutsideFillRunsAtItsVersionWhileTheUniqueShiftRebuildDefers()
    {
        using TestDatabase stepwise = new(useFile: true);
        SeedEarlyFill(stepwise);
        EarlyFillChain(stepwise.Schema.Migrations(), 2).Migrate();
        EarlyFillChain(stepwise.Schema.Migrations(), 3).Migrate();

        using TestDatabase collapsed = new(useFile: true);
        SeedEarlyFill(collapsed);
        EarlyFillChain(collapsed.Schema.Migrations(), 3).Migrate();

        List<(string? Tag, int Val)> stepwiseRows = EarlyFillRows(stepwise);
        List<(string? Tag, int Val)> collapsedRows = EarlyFillRows(collapsed);

        Assert.Equal([("upd", 21)], stepwiseRows);
        Assert.Equal(stepwiseRows, collapsedRows);
    }

    private static void SeedEarlyFill(TestDatabase db)
    {
        db.Execute("CREATE TABLE \"ChnEarlyRows\" (\"Id\" INTEGER PRIMARY KEY, \"Tag\" TEXT, \"Val\" INTEGER NOT NULL, \"Legacy\" TEXT)");
        db.Execute("INSERT INTO \"ChnEarlyRows\" (\"Id\", \"Tag\", \"Val\", \"Legacy\") VALUES (1, NULL, 20, 'lg')");
        db.Pragmas.UserVersion = 1;
    }

    private static SQLiteMigrationRunner EarlyFillChain(SQLiteMigrationRunner runner, int upTo)
    {
        runner.Version(2, m => m
            .Update<ChnEarlyRow>(s => s.Set(x => x.Tag, "upd"))
            .TableChanged<ChnEarlyRow>(s => s.Set(x => x.Tag, r => SQLiteColumn.Of<string?>(r, "Legacy"))));
        if (upTo >= 3)
        {
            runner.Version(3, m => m.TableChanged<ChnEarlyRow>(s => s.Set(x => x.Val, r => r.Val + 1)));
        }

        return runner;
    }

    private static List<(string? Tag, int Val)> EarlyFillRows(TestDatabase db)
    {
        return db.Table<ChnEarlyRow>()
            .OrderBy(x => x.Id)
            .Select(x => new { x.Tag, x.Val })
            .ToList()
            .Select(x => (x.Tag, x.Val))
            .ToList();
    }

    [Fact]
    public void EarlyFillsOfEveryKindRunAtTheirVersionWhileTheReconvertRebuildDefers()
    {
        using TestDatabase stepwise = new(
            b => b.TypeConverters[typeof(Address)] = new SQLiteJsonConverter<Address>(TestJsonContext.Default.Address),
            useFile: true);
        SeedEveryFill(stepwise);
        EveryFillChain(stepwise.Schema.Migrations(), 2).Migrate();
        EveryFillChain(stepwise.Schema.Migrations(), 3).Migrate();

        using TestDatabase collapsed = new(
            b => b.TypeConverters[typeof(Address)] = new SQLiteJsonConverter<Address>(TestJsonContext.Default.Address),
            useFile: true);
        SeedEveryFill(collapsed);
        EveryFillChain(collapsed.Schema.Migrations(), 3).Migrate();

        List<(string Name, string Name2, string? Tag, string? Note, int Val2, string? NewCol, string City)> stepwiseRows = EveryFillRows(stepwise);
        List<(string Name, string Name2, string? Tag, string? Note, int Val2, string? NewCol, string City)> collapsedRows = EveryFillRows(collapsed);

        Assert.Equal([("x", "x", "lg", "c", 10, "new", "A")], stepwiseRows);
        Assert.Equal(stepwiseRows, collapsedRows);
    }

    private static void SeedEveryFill(TestDatabase db)
    {
        db.Execute("CREATE TABLE \"ChnEveryRows\" (\"Id\" INTEGER PRIMARY KEY, \"Name\" TEXT, \"Name2\" TEXT, \"Tag\" TEXT, \"Note\" TEXT, \"Val2\" INTEGER NOT NULL, \"Data\" TEXT NOT NULL, \"Legacy\" TEXT)");
        db.Execute("INSERT INTO \"ChnEveryRows\" (\"Id\", \"Name\", \"Name2\", \"Tag\", \"Note\", \"Val2\", \"Data\", \"Legacy\") VALUES (1, NULL, NULL, NULL, NULL, 5, '{\"Street\":\"1\",\"City\":\"A\"}', 'lg')");
        db.Pragmas.UserVersion = 1;
    }

    private static SQLiteMigrationRunner EveryFillChain(SQLiteMigrationRunner runner, int upTo)
    {
        runner.Version(2, m => m
            .Update<ChnEveryRow>(s => s.Set(x => x.Note, "c"))
            .TableChanged<ChnEveryRow>(s => s
                .Set(x => x.Name, r => r.Name ?? "x")
                .Set(x => x.Name2, r => r.Name2 ?? "x")
                .Set(x => x.Tag, r => SQLiteColumn.Of<string?>(r, "Legacy"))
                .Set(x => x.Note, "c")
                .Set(x => x.Val2, r => r.Val2 * 2)
                .Set(x => x.NewCol, "new")));
        if (upTo >= 3)
        {
            runner.Version(3, m => m.TableChanged<ChnEveryRow>(s => s.Reconvert(x => x.Data)));
        }

        return runner;
    }

    private static List<(string Name, string Name2, string? Tag, string? Note, int Val2, string? NewCol, string City)> EveryFillRows(TestDatabase db)
    {
        return db.Table<ChnEveryRow>()
            .OrderBy(x => x.Id)
            .Select(x => new { x.Name, x.Name2, x.Tag, x.Note, x.Val2, x.NewCol, City = x.Data.City })
            .ToList()
            .Select(x => (x.Name, x.Name2, x.Tag, x.Note, x.Val2, x.NewCol, x.City))
            .ToList();
    }

    [Fact]
    public void ADeferredRebuildSkipsTheComputedColumnWhenAddingMissingColumns()
    {
        using ModelTestDatabase stepwise = new(model => model.Entity<ChnComputedRow>()
            .Computed(r => r.Doubled, r => r.Val * 2),
            b => b.TypeConverters[typeof(Address)] = new SQLiteJsonConverter<Address>(TestJsonContext.Default.Address));
        SeedComputed(stepwise);
        ComputedChain(stepwise.Schema.Migrations(), 2).Migrate();
        ComputedChain(stepwise.Schema.Migrations(), 3).Migrate();

        using ModelTestDatabase collapsed = new(model => model.Entity<ChnComputedRow>()
            .Computed(r => r.Doubled, r => r.Val * 2),
            b => b.TypeConverters[typeof(Address)] = new SQLiteJsonConverter<Address>(TestJsonContext.Default.Address));
        SeedComputed(collapsed);
        ComputedChain(collapsed.Schema.Migrations(), 3).Migrate();

        List<(string Name, int Val, int Doubled, string? Note)> stepwiseRows = ComputedRows(stepwise);
        List<(string Name, int Val, int Doubled, string? Note)> collapsedRows = ComputedRows(collapsed);

        Assert.Equal([("x", 20, 40, null)], stepwiseRows);
        Assert.Equal(stepwiseRows, collapsedRows);
    }

    private static void SeedComputed(TestDatabase db)
    {
        db.Execute("CREATE TABLE \"ChnComputedRows\" (\"Id\" INTEGER PRIMARY KEY, \"Name\" TEXT, \"Val\" INTEGER NOT NULL, \"Data\" TEXT NOT NULL)");
        db.Execute("INSERT INTO \"ChnComputedRows\" (\"Id\", \"Name\", \"Val\", \"Data\") VALUES (1, NULL, 10, '{\"Street\":\"1\",\"City\":\"A\"}')");
        db.Pragmas.UserVersion = 1;
    }

    private static SQLiteMigrationRunner ComputedChain(SQLiteMigrationRunner runner, int upTo)
    {
        runner.Version(2, m => m
            .Update<ChnComputedRow>(s => s.Set(x => x.Val, 20))
            .TableChanged<ChnComputedRow>(s => s.Set(x => x.Name, r => r.Name ?? "x")));
        if (upTo >= 3)
        {
            runner.Version(3, m => m.TableChanged<ChnComputedRow>(s => s.Reconvert(x => x.Data)));
        }

        return runner;
    }

    private static List<(string Name, int Val, int Doubled, string? Note)> ComputedRows(TestDatabase db)
    {
        return db.Table<ChnComputedRow>()
            .OrderBy(x => x.Id)
            .Select(x => new { x.Name, x.Val, x.Doubled, x.Note })
            .ToList()
            .Select(x => (x.Name, x.Val, x.Doubled, x.Note))
            .ToList();
    }

    [Fact]
    public void ADeferredRebuildAddsAMissingShadowColumn()
    {
        using ModelTestDatabase stepwise = new(model => model.Entity<ChnTightenRow>()
            .Column("ShadowNote", SQLiteColumnType.Text));
        SeedShadow(stepwise);
        TightenChainLocal(stepwise.Schema.Migrations(), 2).Migrate();
        TightenChainLocal(stepwise.Schema.Migrations(), 3).Migrate();

        using ModelTestDatabase collapsed = new(model => model.Entity<ChnTightenRow>()
            .Column("ShadowNote", SQLiteColumnType.Text));
        SeedShadow(collapsed);
        TightenChainLocal(collapsed.Schema.Migrations(), 3).Migrate();

        List<(string Name, int Val, string? Note)> stepwiseRows = ShadowRows(stepwise);
        List<(string Name, int Val, string? Note)> collapsedRows = ShadowRows(collapsed);

        Assert.Equal([("x", 99, null)], stepwiseRows);
        Assert.Equal(stepwiseRows, collapsedRows);
        Assert.Equal(1L, collapsed.ExecuteScalar<long>(
            "SELECT COUNT(*) FROM pragma_table_xinfo('ChnTightenRows', 'main') WHERE name = 'ShadowNote'"));
    }

    private static void SeedShadow(TestDatabase db)
    {
        db.Execute("CREATE TABLE \"ChnTightenRows\" (\"Id\" INTEGER PRIMARY KEY, \"Name\" TEXT, \"Val\" INTEGER NOT NULL)");
        db.Execute("INSERT INTO \"ChnTightenRows\" (\"Id\", \"Name\", \"Val\") VALUES (1, NULL, 10)");
        db.Pragmas.UserVersion = 1;
    }

    private static SQLiteMigrationRunner TightenChainLocal(SQLiteMigrationRunner runner, int upTo)
    {
        runner.Version(2, m => m.Update<ChnTightenRow>(s => s.Set(x => x.Val, 99)));
        if (upTo >= 3)
        {
            runner.Version(3, m => m.TableChanged<ChnTightenRow>(s => s.Set(x => x.Name, r => r.Name ?? "x")));
        }

        return runner;
    }

    private static List<(string Name, int Val, string? Note)> ShadowRows(TestDatabase db)
    {
        return db.Table<ChnTightenRow>()
            .OrderBy(x => x.Id)
            .Select(x => new { x.Name, x.Val, x.Note })
            .ToList()
            .Select(x => (x.Name, x.Val, x.Note))
            .ToList();
    }

    private static void Seed(TestDatabase db)
    {
        db.Execute("CREATE TABLE \"ChnDeferredRows\" (\"Id\" INTEGER PRIMARY KEY, \"Val\" INTEGER NOT NULL)");
        db.Execute("INSERT INTO \"ChnDeferredRows\" (\"Id\", \"Val\") VALUES (1, 10)");
        db.Pragmas.UserVersion = 1;
    }

    private static SQLiteMigrationRunner Chain(SQLiteMigrationRunner runner, int upTo)
    {
        runner.Version(2, m => m.Update<ChnDeferredRow>(s => s.Set(x => x.Val, 11)));
        if (upTo >= 3)
        {
            runner.Version(3, m => m.TableChanged<ChnDeferredRow>(s => s.Set(x => x.Val, r => r.Val + 1)));
        }

        return runner;
    }

    private static List<(int Val, string? Note)> Rows(TestDatabase db)
    {
        return db.Table<ChnDeferredRow>()
            .OrderBy(x => x.Id)
            .Select(x => new { x.Val, x.Note })
            .ToList()
            .Select(x => (x.Val, x.Note))
            .ToList();
    }

    private static void SeedExpr(TestDatabase db)
    {
        db.Execute("CREATE TABLE \"ChnExprRows\" (\"Id\" INTEGER PRIMARY KEY, \"Name\" TEXT, \"Val\" INTEGER NOT NULL)");
        db.Execute("INSERT INTO \"ChnExprRows\" (\"Id\", \"Name\", \"Val\") VALUES (1, NULL, 11)");
        db.Pragmas.UserVersion = 1;
    }

    private static SQLiteMigrationRunner ExprChain(SQLiteMigrationRunner runner, int upTo)
    {
        runner.Version(2, m => m.Update<ChnExprRow>(s => s.Set(x => x.Val, 20)));
        if (upTo >= 3)
        {
            runner.Version(3, m => m.TableChanged<ChnExprRow>(s => s.Set(x => x.Name, r => r.Name ?? "x")));
        }

        return runner;
    }

    private static List<(string Name, int Val, string? Stamp, string? Note, int Total)> ExprRows(TestDatabase db)
    {
        return db.Table<ChnExprRow>()
            .OrderBy(x => x.Id)
            .Select(x => new { x.Name, x.Val, x.Stamp, x.Note, x.Total })
            .ToList()
            .Select(x => (x.Name, x.Val, x.Stamp, x.Note, x.Total))
            .ToList();
    }
}

[Table("ChnExprRows")]
public class ChnExprRow
{
    [Key]
    public int Id { get; set; }

    public string Name { get; set; } = "";

    public int Val { get; set; }

    public string? Stamp { get; set; }

    public string? Note { get; set; }

    public int Total { get; set; }
}

[Table("ChnEarlyRows")]
public class ChnEarlyRow
{
    [Key]
    public int Id { get; set; }

    public string? Tag { get; set; }

    [Indexed(IsUnique = true)]
    public int Val { get; set; }
}

[Table("ChnEveryRows")]
public class ChnEveryRow
{
    [Key]
    public int Id { get; set; }

    public string Name { get; set; } = "";

    public string Name2 { get; set; } = "";

    public string? Tag { get; set; }

    public string? Note { get; set; }

    public int Val2 { get; set; }

    public Address Data { get; set; } = new();

    public string? NewCol { get; set; }
}

[Table("ChnComputedRows")]
public class ChnComputedRow
{
    [Key]
    public int Id { get; set; }

    public string Name { get; set; } = "";

    public int Val { get; set; }

    public int Doubled { get; set; }

    public Address Data { get; set; } = new();

    public string? Note { get; set; }
}

[Table("ChnStrictAdd")]
public class ChnStrictAddRow
{
    [Key]
    public int Id { get; set; }

    public string Name { get; set; } = "";

    public int Val { get; set; }

    public int Total { get; set; }
}
