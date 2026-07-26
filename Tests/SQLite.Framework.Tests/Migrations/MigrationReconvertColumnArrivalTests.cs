#if !SQLITECIPHER
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H22lArrivalDocs")]
public class H22lArrivalDoc
{
    [Key]
    public int Id { get; set; }

    public Address Data { get; set; } = new();

    public string? Tag { get; set; }
}

public class MigrationReconvertColumnArrivalTests
{
    [Fact]
    public void AFillOnANewColumnRunsAfterTheColumnExistsWhenALaterVersionReconverts()
    {
        Assert.Equal(Stepwise(FillChain), Collapsed(FillChain));
    }

    [Fact]
    public void AFillOnANewColumnKeepsItsValueWhenALaterVersionReconverts()
    {
        Assert.Equal([(1, "1", "A", "t3"), (2, "2", "B", "t3")], Collapsed(FillChain));
    }

    [Fact]
    public void ARawStepReadsTheColumnAnEarlierVersionAddedWhenALaterVersionReconverts()
    {
        Assert.Equal(Stepwise(RawStepChain), Collapsed(RawStepChain));
    }

    [Fact]
    public void ARawStepKeepsItsValueWhenALaterVersionReconverts()
    {
        Assert.Equal([(1, "1", "A", "v4"), (2, "2", "B", "v4")], Collapsed(RawStepChain));
    }

    [Fact]
    public void AFillOnANewColumnRunsAfterARawStepOnAnUnrelatedTable()
    {
        Assert.Equal(Stepwise(UnrelatedTableChain), Collapsed(UnrelatedTableChain));
    }

    private static void FillChain(SQLiteMigrationRunner runner, int upTo)
    {
        runner.Version(2, m => m.Sql(
            "INSERT INTO \"H22lArrivalDocs\" (\"Id\", \"Data\") VALUES (2, '{\"Street\":\"2\",\"City\":\"B\"}')"));
        if (upTo >= 3)
        {
            runner.Version(3, m => m.TableChanged<H22lArrivalDoc>(s => s.Set(x => x.Tag, "t3")));
        }

        if (upTo >= 4)
        {
            runner.Version(4, m => m.Delete<H22lArrivalDoc>(x => x.Id == 999));
        }

        if (upTo >= 5)
        {
            runner.Version(5, m => m.TableChanged<H22lArrivalDoc>(s => s.Reconvert(x => x.Data)));
        }
    }

    private static void RawStepChain(SQLiteMigrationRunner runner, int upTo)
    {
        runner.Version(2, m => m.Sql(
            "INSERT INTO \"H22lArrivalDocs\" (\"Id\", \"Data\") VALUES (2, '{\"Street\":\"2\",\"City\":\"B\"}')"));
        if (upTo >= 3)
        {
            runner.Version(3, m => m.TableChanged<H22lArrivalDoc>());
        }

        if (upTo >= 4)
        {
            runner.Version(4, m => m.Sql("UPDATE \"H22lArrivalDocs\" SET \"Tag\" = 'v4'"));
        }

        if (upTo >= 5)
        {
            runner.Version(5, m => m.TableChanged<H22lArrivalDoc>(s => s.Reconvert(x => x.Data)));
        }
    }

    private static void UnrelatedTableChain(SQLiteMigrationRunner runner, int upTo)
    {
        runner.Version(2, m => m.Sql("INSERT INTO \"H22lArrivalNotes\" (\"Id\") VALUES (1)"));
        if (upTo >= 3)
        {
            runner.Version(3, m => m.TableChanged<H22lArrivalDoc>(s => s.Set(x => x.Tag, "s3")));
        }

        if (upTo >= 4)
        {
            runner.Version(4, m => m.Sql("UPDATE \"H22lArrivalNotes\" SET \"Id\" = \"Id\""));
        }

        if (upTo >= 5)
        {
            runner.Version(5, m => m.TableChanged<H22lArrivalDoc>(s => s.Reconvert(x => x.Data)));
        }
    }

    private static List<(int Id, string? Street, string? City, string? Tag)> Stepwise(Action<SQLiteMigrationRunner, int> chain)
    {
        using TestDatabase db = Db();
        Seed(db);
        for (int upTo = 2; upTo <= 5; upTo++)
        {
            SQLiteMigrationRunner runner = db.Schema.Migrations();
            chain(runner, upTo);
            runner.Migrate();
        }

        return Rows(db);
    }

    private static List<(int Id, string? Street, string? City, string? Tag)> Collapsed(Action<SQLiteMigrationRunner, int> chain)
    {
        using TestDatabase db = Db();
        Seed(db);
        SQLiteMigrationRunner runner = db.Schema.Migrations();
        chain(runner, 5);
        runner.Migrate();
        return Rows(db);
    }

    private static TestDatabase Db()
    {
        return new TestDatabase(
            b => b.TypeConverters[typeof(Address)] = new SQLiteJsonConverter<Address>(TestJsonContext.Default.Address),
            useFile: true);
    }

    private static void Seed(TestDatabase db)
    {
        db.Execute("CREATE TABLE \"H22lArrivalDocs\" (\"Id\" INTEGER PRIMARY KEY, \"Data\" TEXT NOT NULL)");
        db.Execute("INSERT INTO \"H22lArrivalDocs\" (\"Id\", \"Data\") VALUES (1, '{\"Street\":\"1\",\"City\":\"A\"}')");
        db.Execute("CREATE TABLE \"H22lArrivalNotes\" (\"Id\" INTEGER PRIMARY KEY)");
        db.Pragmas.UserVersion = 1;
    }

    private static List<(int Id, string? Street, string? City, string? Tag)> Rows(TestDatabase db)
    {
        return db.Table<H22lArrivalDoc>()
            .OrderBy(r => r.Id)
            .Select(r => new { r.Id, r.Data, r.Tag })
            .ToList()
            .Select(r => (r.Id, r.Data?.Street, r.Data?.City, r.Tag))
            .ToList();
    }
}
#endif
