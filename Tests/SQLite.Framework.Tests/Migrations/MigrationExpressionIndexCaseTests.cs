using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("SecQExprCase")]
public class SecQExprCaseRow
{
    [Key]
    public int Id { get; set; }
}

public class MigrationExpressionIndexCaseTests
{
    [Fact]
    public void ReconcileRebuildsWhenAnExpressionIndexOnADifferentCasedTableReferencesTheDroppedColumn()
    {
        using TestDatabase db = new();
        db.Execute("CREATE TABLE \"secqexprcase\" (\"Id\" INTEGER PRIMARY KEY, \"C\" TEXT)");
        db.Execute("INSERT INTO \"secqexprcase\" VALUES (1, 'v')");
        db.Execute("CREATE INDEX \"SecQExprIdx\" ON \"secqexprcase\" (\"C\" || 'x')");

        db.Schema.Migrations()
            .Version(1, m => m.TableChanged<SecQExprCaseRow>())
            .Migrate();

        Assert.False(db.Schema.ColumnExists<SecQExprCaseRow>("C"));
        Assert.False(db.Schema.IndexExists("SecQExprIdx"));
        Assert.Equal(1, db.Table<SecQExprCaseRow>().Single().Id);
    }
}
