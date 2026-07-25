using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("CasedLedger")]
public class CasedLedgerRow
{
    [Key]
    public int Id { get; set; }

    public string Name { get; set; } = "";
}

public class MigrationRenameTableCaseTests
{
    [Fact]
    public void RenameToTheSameNameInDifferentCaseWorks()
    {
        using TestDatabase db = new(useFile: true);
        db.Execute("CREATE TABLE \"casedledger\" (\"Id\" INTEGER PRIMARY KEY, \"Name\" TEXT NOT NULL)");
        db.Execute("INSERT INTO \"casedledger\" (\"Id\", \"Name\") VALUES (1, 'kept')");

        db.Schema.Migrations()
            .Version(1, m => m.RenameTable<CasedLedgerRow>("casedledger"))
            .Migrate();

        List<string> names = db.Query<string>("SELECT name FROM sqlite_master WHERE type = 'table' AND name LIKE 'CasedLedger'");
        Assert.Equal(["CasedLedger"], names);
        Assert.Equal("kept", db.Table<CasedLedgerRow>().Single().Name);
    }
}
