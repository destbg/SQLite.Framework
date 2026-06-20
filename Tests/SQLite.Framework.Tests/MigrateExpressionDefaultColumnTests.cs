using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using SQLite.Framework;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("MigrateExprDefault")]
public class MigrateExprDefaultRow
{
    [Key]
    public int Id { get; set; }

    public required string Name { get; set; }

    public string? Tag { get; set; }
}

public class MigrateExpressionDefaultColumnTests
{
    [Fact]
    public void MigrateAddsNewColumnWithExpressionDefaultToPopulatedTable()
    {
        using ModelTestDatabase db = new(model => model.Entity<MigrateExprDefaultRow>()
            .Default(b => b.Tag, () => SQLiteFunctions.SqliteVersion()));

        db.Execute("CREATE TABLE \"MigrateExprDefault\" (\"Id\" INTEGER PRIMARY KEY, \"Name\" TEXT NOT NULL)");
        db.Execute("INSERT INTO \"MigrateExprDefault\" (\"Id\", \"Name\") VALUES (1, 'a')");

        db.Schema.Table<MigrateExprDefaultRow>().Migrate();

        MigrateExprDefaultRow row = db.Table<MigrateExprDefaultRow>().Single(x => x.Id == 1);

        Assert.False(string.IsNullOrEmpty(row.Tag));
    }
}
