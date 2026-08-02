#if !SQLITECIPHER
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework;
using SQLite.Framework.Attributes;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[StrictTable]
[Table("ChnBPayloadRows")]
public class ChnBPayloadRow
{
    [Key]
    public int Id { get; set; }

    public Address Data { get; set; } = new();
}

public class MigrationStrictStorageFlipChainTests
{
    [Fact]
    public void ACollapsedChainPastAReversedStorageFlipKeepsReadableData()
    {
        using TestDatabase db = Db();
        db.Execute("CREATE TABLE \"ChnBPayloadRows\" (\"Id\" INTEGER PRIMARY KEY, \"Data\" TEXT NOT NULL) STRICT");
        db.Execute("INSERT INTO \"ChnBPayloadRows\" (\"Id\", \"Data\") VALUES (1, '{\"Street\":\"1\",\"City\":\"A\"}')");
        db.Pragmas.UserVersion = 1;

        Chain(db, 6).Migrate();

        Assert.Equal("A", db.Table<ChnBPayloadRow>().Single().Data.City);
        Assert.Equal("text", db.ExecuteScalar<string>("SELECT typeof(\"Data\") FROM \"ChnBPayloadRows\""));
    }

    [Fact]
    public void AStepwiseChainPastAReversedStorageFlipStopsWithGuidance()
    {
        using TestDatabase db = Db();
        db.Execute("CREATE TABLE \"ChnBPayloadRows\" (\"Id\" INTEGER PRIMARY KEY, \"Data\" BLOB NOT NULL) STRICT");
        db.Execute("INSERT INTO \"ChnBPayloadRows\" (\"Id\", \"Data\") VALUES (1, jsonb('{\"Street\":\"1\",\"City\":\"A\"}'))");
        db.Pragmas.UserVersion = 2;

        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() => Chain(db, 6).Migrate());

        Assert.Contains("Data", ex.Message);
        Assert.Contains("Reconvert", ex.Message);
    }

    private static TestDatabase Db()
    {
        return new TestDatabase(b =>
            b.TypeConverters[typeof(Address)] = new SQLiteJsonConverter<Address>(TestJsonContext.Default.Address));
    }

    private static SQLiteMigrationRunner Chain(TestDatabase db, int upTo)
    {
        SQLiteMigrationRunner runner = db.Schema.Migrations();
        runner.Version(2, m => m.TableChanged<ChnBPayloadRow>(s => s.Reconvert(x => x.Data)));
        if (upTo >= 3)
        {
            runner.Version(3, m => m.TableChanged<ChnBPayloadRow>());
        }

        if (upTo >= 4)
        {
            runner.Version(4, m => m.TableChanged<ChnBPayloadRow>());
        }

        if (upTo >= 5)
        {
            runner.Version(5, m => m.TableChanged<ChnBPayloadRow>());
        }

        if (upTo >= 6)
        {
            runner.Version(6, m => m.TableChanged<ChnBPayloadRow>());
        }

        return runner;
    }
}
#endif
