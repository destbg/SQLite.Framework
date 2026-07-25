#if !SQLITECIPHER
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using SQLite.Framework;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("mig_reconvert_after_set_rows")]
public class ReconvertAfterSetRow
{
    [Key]
    public int Id { get; set; }

    public Address Data { get; set; } = new();
}

public class MigrationReconvertAfterEarlierSetTests
{
    private static TestDatabase Db()
    {
        return new TestDatabase(b =>
            b.TypeConverters[typeof(Address)] = new SQLiteJsonConverter<Address>(TestJsonContext.Default.Address));
    }

    [Fact]
    public void ReconvertAtLaterVersionKeepsTheValueAnEarlierVersionSet()
    {
        using TestDatabase collapsed = Db();
        collapsed.Execute("CREATE TABLE \"mig_reconvert_after_set_rows\" (\"Id\" INTEGER PRIMARY KEY, \"Data\" TEXT NOT NULL)");
        collapsed.Execute("INSERT INTO \"mig_reconvert_after_set_rows\" (\"Id\", \"Data\") VALUES (1, '{\"Street\":\"old\",\"City\":\"o\"}')");
        collapsed.Schema.Migrations()
            .Version(2, m => m.TableChanged<ReconvertAfterSetRow>(s => s.Set(x => x.Data, new Address { Street = "v2set", City = "c" })))
            .Version(3, m => m.TableChanged<ReconvertAfterSetRow>(s => s.Reconvert(x => x.Data)))
            .Migrate();

        using TestDatabase stepwise = Db();
        stepwise.Execute("CREATE TABLE \"mig_reconvert_after_set_rows\" (\"Id\" INTEGER PRIMARY KEY, \"Data\" TEXT NOT NULL)");
        stepwise.Execute("INSERT INTO \"mig_reconvert_after_set_rows\" (\"Id\", \"Data\") VALUES (1, '{\"Street\":\"old\",\"City\":\"o\"}')");
        stepwise.Schema.Migrations()
            .Version(2, m => m.TableChanged<ReconvertAfterSetRow>(s => s.Set(x => x.Data, new Address { Street = "v2set", City = "c" })))
            .Migrate();
        stepwise.Schema.Migrations()
            .Version(2, m => m.TableChanged<ReconvertAfterSetRow>(s => s.Set(x => x.Data, new Address { Street = "v2set", City = "c" })))
            .Version(3, m => m.TableChanged<ReconvertAfterSetRow>(s => s.Reconvert(x => x.Data)))
            .Migrate();

        string stepwiseStreet = stepwise.Table<ReconvertAfterSetRow>().Single().Data.Street;
        string collapsedStreet = collapsed.Table<ReconvertAfterSetRow>().Single().Data.Street;

        Assert.Equal("v2set", stepwiseStreet);
        Assert.Equal(stepwiseStreet, collapsedStreet);
    }
}
#endif
