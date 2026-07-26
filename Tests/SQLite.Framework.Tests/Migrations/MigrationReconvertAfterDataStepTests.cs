#if !SQLITECIPHER
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("ReconvertAfterDataRows")]
public class ReconvertAfterDataRow
{
    [Key]
    public int Id { get; set; }

    public Address Data { get; set; } = new();
}

public class MigrationReconvertAfterDataStepTests
{
    [Fact]
    public void ReconvertStaysInTheRebuildAfterAnEarlierDataStep()
    {
        Assert.Equal(Stepwise(), Collapsed());
    }

    [Fact]
    public void ReconvertAfterAnEarlierDataStepKeepsEveryValue()
    {
        Assert.Equal([(1, "1", "A"), (2, "2", "B")], Stepwise());
    }

    private static List<(int Id, string? Street, string? City)> Stepwise()
    {
        using TestDatabase db = Db();
        Seed(db);
        for (int upTo = 2; upTo <= 3; upTo++)
        {
            SQLiteMigrationRunner runner = db.Schema.Migrations();
            Chain(runner, upTo);
            runner.Migrate();
        }

        return Rows(db);
    }

    private static List<(int Id, string? Street, string? City)> Collapsed()
    {
        using TestDatabase db = Db();
        Seed(db);
        SQLiteMigrationRunner runner = db.Schema.Migrations();
        Chain(runner, 3);
        runner.Migrate();
        return Rows(db);
    }

    private static void Chain(SQLiteMigrationRunner runner, int upTo)
    {
        runner.Version(2, m => m.Sql(
            "INSERT INTO \"ReconvertAfterDataRows\" (\"Id\", \"Data\") VALUES (2, jsonb('{\"Street\":\"2\",\"City\":\"B\"}'))"));
        if (upTo >= 3)
        {
            runner.Version(3, m => m.TableChanged<ReconvertAfterDataRow>(s => s.Reconvert(x => x.Data)));
        }
    }

    private static void Seed(TestDatabase db)
    {
        db.Execute("CREATE TABLE \"ReconvertAfterDataRows\" (\"Id\" INTEGER PRIMARY KEY, \"Data\" BLOB NOT NULL)");
        db.Execute("INSERT INTO \"ReconvertAfterDataRows\" (\"Id\", \"Data\") VALUES (1, jsonb('{\"Street\":\"1\",\"City\":\"A\"}'))");
        db.Pragmas.UserVersion = 1;
    }

    private static TestDatabase Db()
    {
        return new TestDatabase(b =>
            b.TypeConverters[typeof(Address)] = new SQLiteJsonConverter<Address>(TestJsonContext.Default.Address), true);
    }

    private static List<(int Id, string? Street, string? City)> Rows(TestDatabase db)
    {
        return db.Table<ReconvertAfterDataRow>()
            .OrderBy(r => r.Id)
            .Select(r => new { r.Id, r.Data })
            .ToList()
            .Select(r => (r.Id, r.Data?.Street, r.Data?.City))
            .ToList();
    }
}
#endif
