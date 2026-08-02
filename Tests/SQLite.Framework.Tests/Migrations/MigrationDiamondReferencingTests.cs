using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Attributes;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("ChnDiamondA")]
public class ChnDiamondARow
{
    [Key]
    public int Id { get; set; }

    public string? Name { get; set; }
}

public class MigrationDiamondReferencingTests
{
    [Fact]
    public void ASharedGrandchildIsEmptiedAndRestoredOnce()
    {
        using TestDatabase db = new();
        db.Execute("CREATE TABLE \"ChnDiamondA\" (\"Id\" INTEGER PRIMARY KEY, \"Name\" TEXT, \"Legacy\" TEXT)");
        db.Execute("CREATE TABLE \"ChnDiamondB\" (\"Id\" INTEGER PRIMARY KEY, \"AId\" INTEGER REFERENCES \"ChnDiamondA\"(\"Id\"))");
        db.Execute("CREATE TABLE \"ChnDiamondC\" (\"Id\" INTEGER PRIMARY KEY, \"AId\" INTEGER REFERENCES \"ChnDiamondA\"(\"Id\"), \"BId\" INTEGER REFERENCES \"ChnDiamondB\"(\"Id\"))");
        db.Execute("INSERT INTO \"ChnDiamondA\" (\"Id\", \"Name\", \"Legacy\") VALUES (1, 'a', 'x')");
        db.Execute("INSERT INTO \"ChnDiamondB\" (\"Id\", \"AId\") VALUES (10, 1)");
        db.Execute("INSERT INTO \"ChnDiamondC\" (\"Id\", \"AId\", \"BId\") VALUES (100, 1, 10)");
        db.Pragmas.ForeignKeys = true;

        db.Schema.Migrations()
            .Version(1, m => m.TableChanged<ChnDiamondARow>(rebuild: true))
            .Migrate();

        Assert.Equal("a", db.Table<ChnDiamondARow>().Single().Name);
        Assert.Equal(1L, db.ExecuteScalar<long>("SELECT COUNT(*) FROM \"ChnDiamondB\""));
        Assert.Equal(1L, db.ExecuteScalar<long>("SELECT COUNT(*) FROM \"ChnDiamondC\""));
        Assert.Equal(100L, db.ExecuteScalar<long>("SELECT \"Id\" FROM \"ChnDiamondC\""));
    }
}
