using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("DropCaseTicket")]
public class DropCaseTicketRow
{
    [Key]
    public int Id { get; set; }

    public int Keep { get; set; }
}

public class MigrationDropColumnNameCaseTests
{
    [Fact]
    public void DropsTheColumnWhenTheDeclaredNameDiffersInCase()
    {
        using TestDatabase db = new(useFile: true);
        db.Execute("CREATE TABLE \"DropCaseTicket\" (\"Id\" INTEGER PRIMARY KEY, \"Keep\" INTEGER NOT NULL, \"Extra\" TEXT)");
        db.Execute("INSERT INTO \"DropCaseTicket\" (\"Id\", \"Keep\", \"Extra\") VALUES (1, 5, 'x')");

        db.Schema.Migrations()
            .Version(1, m => m.DropColumn<DropCaseTicketRow>("extra"))
            .Migrate();

        Assert.DoesNotContain("Extra", db.Pragmas.TableInfo("DropCaseTicket").Select(c => c.Name));
        Assert.Equal(5, db.Table<DropCaseTicketRow>().Single().Keep);
    }
}
