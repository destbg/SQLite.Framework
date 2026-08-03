using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework;
using SQLite.Framework.Attributes;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("ChnTightenRows")]
public class ChnTightenRow
{
    [Key]
    public int Id { get; set; }

    public string Name { get; set; } = "";

    public int Val { get; set; }

    public string? Note { get; set; }
}

[Table("ChnUniqueRows")]
public class ChnUniqueRow
{
    [Key]
    public int Id { get; set; }

    [Indexed(IsUnique = true)]
    public int Val { get; set; }

    public string? Tag { get; set; }
}

public class MigrationDeferredRebuildFillTests
{
    [Fact]
    public void ANotNullTighteningFillDefersTheRebuildPastAnEarlierUpdate()
    {
        using TestDatabase stepwise = new(useFile: true);
        SeedTighten(stepwise);
        TightenChain(stepwise.Schema.Migrations(), 2).Migrate();
        TightenChain(stepwise.Schema.Migrations(), 3).Migrate();

        using TestDatabase collapsed = new(useFile: true);
        SeedTighten(collapsed);
        TightenChain(collapsed.Schema.Migrations(), 3).Migrate();

        List<(string Name, int Val, string? Note)> stepwiseRows = TightenRows(stepwise);
        List<(string Name, int Val, string? Note)> collapsedRows = TightenRows(collapsed);

        Assert.Equal([("x", 99, null)], stepwiseRows);
        Assert.Equal(stepwiseRows, collapsedRows);
    }

    [Fact]
    public void AUniqueColumnShiftDefersTheRebuildPastAnEarlierUpdate()
    {
        using TestDatabase stepwise = new(useFile: true);
        SeedUnique(stepwise);
        UniqueChain(stepwise.Schema.Migrations(), 2).Migrate();
        UniqueChain(stepwise.Schema.Migrations(), 3).Migrate();

        using TestDatabase collapsed = new(useFile: true);
        SeedUnique(collapsed);
        UniqueChain(collapsed.Schema.Migrations(), 3).Migrate();

        List<(int Val, string? Tag)> stepwiseRows = UniqueRows(stepwise);
        List<(int Val, string? Tag)> collapsedRows = UniqueRows(collapsed);

        Assert.Equal([(21, null), (31, null)], stepwiseRows);
        Assert.Equal(stepwiseRows, collapsedRows);
    }

    [Fact]
    public void ANewNotNullColumnWithAnExpressionDefaultBackfillsDuringTheDeferredRebuild()
    {
        using ModelTestDatabase stepwise = new(model => model.Entity<ChnTightenRow>()
            .Default(r => r.Name, () => "filled"));
        SeedTighten(stepwise);
        TightenChain(stepwise.Schema.Migrations(), 2).Migrate();
        TightenChain(stepwise.Schema.Migrations(), 3).Migrate();

        using ModelTestDatabase collapsed = new(model => model.Entity<ChnTightenRow>()
            .Default(r => r.Name, () => "filled"));
        SeedTighten(collapsed);
        TightenChain(collapsed.Schema.Migrations(), 3).Migrate();

        List<(string Name, int Val, string? Note)> stepwiseRows = TightenRows(stepwise);
        List<(string Name, int Val, string? Note)> collapsedRows = TightenRows(collapsed);

        Assert.Equal([("x", 99, null)], stepwiseRows);
        Assert.Equal(stepwiseRows, collapsedRows);
    }

    private static void SeedTighten(TestDatabase db)
    {
        db.Execute("CREATE TABLE \"ChnTightenRows\" (\"Id\" INTEGER PRIMARY KEY, \"Name\" TEXT, \"Val\" INTEGER NOT NULL)");
        db.Execute("INSERT INTO \"ChnTightenRows\" (\"Id\", \"Name\", \"Val\") VALUES (1, NULL, 10)");
        db.Pragmas.UserVersion = 1;
    }

    private static SQLiteMigrationRunner TightenChain(SQLiteMigrationRunner runner, int upTo)
    {
        runner.Version(2, m => m.Update<ChnTightenRow>(s => s.Set(x => x.Val, 99)));
        if (upTo >= 3)
        {
            runner.Version(3, m => m.TableChanged<ChnTightenRow>(s => s.Set(x => x.Name, r => r.Name ?? "x")));
        }

        return runner;
    }

    private static List<(string Name, int Val, string? Note)> TightenRows(TestDatabase db)
    {
        return db.Table<ChnTightenRow>()
            .OrderBy(x => x.Id)
            .Select(x => new { x.Name, x.Val, x.Note })
            .ToList()
            .Select(x => (x.Name, x.Val, x.Note))
            .ToList();
    }

    private static void SeedUnique(TestDatabase db)
    {
        db.Execute("CREATE TABLE \"ChnUniqueRows\" (\"Id\" INTEGER PRIMARY KEY, \"Val\" INTEGER NOT NULL)");
        db.Execute("INSERT INTO \"ChnUniqueRows\" (\"Id\", \"Val\") VALUES (1, 20), (2, 30)");
        db.Pragmas.UserVersion = 1;
    }

    private static SQLiteMigrationRunner UniqueChain(SQLiteMigrationRunner runner, int upTo)
    {
        runner.Version(2, m => m.Update<ChnUniqueRow>(r => r.Id == 1, s => s.Set(x => x.Val, 20)));
        if (upTo >= 3)
        {
            runner.Version(3, m => m.TableChanged<ChnUniqueRow>(s => s.Set(x => x.Val, r => r.Val + 1)));
        }

        return runner;
    }

    private static List<(int Val, string? Tag)> UniqueRows(TestDatabase db)
    {
        return db.Table<ChnUniqueRow>()
            .OrderBy(x => x.Id)
            .Select(x => new { x.Val, x.Tag })
            .ToList()
            .Select(x => (x.Val, x.Tag))
            .ToList();
    }

    [Fact]
    public void AReconvertStaysInsideTheRebuildDeferredByALaterReconvert()
    {
        using TestDatabase stepwise = new(
            b => b.TypeConverters[typeof(Address)] = new SQLiteJsonConverter<Address>(TestJsonContext.Default.Address),
            useFile: true);
        SeedReconvert(stepwise);
        ReconvertChain(stepwise.Schema.Migrations(), 2).Migrate();
        ReconvertChain(stepwise.Schema.Migrations(), 3).Migrate();

        using TestDatabase collapsed = new(
            b => b.TypeConverters[typeof(Address)] = new SQLiteJsonConverter<Address>(TestJsonContext.Default.Address),
            useFile: true);
        SeedReconvert(collapsed);
        ReconvertChain(collapsed.Schema.Migrations(), 3).Migrate();

        List<(string Name, int Val, string City)> stepwiseRows = ReconvertRows(stepwise);
        List<(string Name, int Val, string City)> collapsedRows = ReconvertRows(collapsed);

        Assert.Equal([("x", 20, "A")], stepwiseRows);
        Assert.Equal(stepwiseRows, collapsedRows);
    }

    private static void SeedReconvert(TestDatabase db)
    {
        db.Execute("CREATE TABLE \"ChnReconvertRows\" (\"Id\" INTEGER PRIMARY KEY, \"Name\" TEXT, \"Val\" INTEGER NOT NULL, \"Data\" TEXT NOT NULL)");
        db.Execute("INSERT INTO \"ChnReconvertRows\" (\"Id\", \"Name\", \"Val\", \"Data\") VALUES (1, NULL, 10, '{\"Street\":\"1\",\"City\":\"A\"}')");
        db.Pragmas.UserVersion = 1;
    }

    private static SQLiteMigrationRunner ReconvertChain(SQLiteMigrationRunner runner, int upTo)
    {
        runner.Version(2, m => m
            .Update<ChnReconvertRow>(s => s.Set(x => x.Val, 20))
            .TableChanged<ChnReconvertRow>(s => s
                .Set(x => x.Name, r => r.Name ?? "x")
                .Reconvert(x => x.Data)));
        if (upTo >= 3)
        {
            runner.Version(3, m => m.TableChanged<ChnReconvertRow>(s => s.Reconvert(x => x.Data)));
        }

        return runner;
    }

    private static List<(string Name, int Val, string City)> ReconvertRows(TestDatabase db)
    {
        return db.Table<ChnReconvertRow>()
            .OrderBy(x => x.Id)
            .Select(x => new { x.Name, x.Val, City = x.Data.City })
            .ToList()
            .Select(x => (x.Name, x.Val, x.City))
            .ToList();
    }
}

[Table("ChnReconvertRows")]
public class ChnReconvertRow
{
    [Key]
    public int Id { get; set; }

    public string Name { get; set; } = "";

    public int Val { get; set; }

    public Address Data { get; set; } = new();
}
