using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("SecC_QuotedParent")]
public class SecCQuotedParent
{
    [Key]
    public int Id { get; set; }

    public string Name { get; set; } = "";
}

[Table("SecC_QuotedGpParent")]
public class SecCQuotedGpParent
{
    [Key]
    public int Id { get; set; }

    public string Name { get; set; } = "";
}

public class MigrateReferencedTableQuotedNameTests
{
    [Fact]
    public void ParentRebuildWithReferencingQuotedChildNamePreservesData()
    {
        using TestDatabase db = new();
        db.Execute("CREATE TABLE \"SecC_QuotedParent\" (\"Id\" INTEGER PRIMARY KEY, \"Name\" TEXT NOT NULL, \"Legacy\" TEXT)");
        db.Execute("CREATE TABLE \"SecC_QuotedKid\"\"x\" (\"Id\" INTEGER PRIMARY KEY, \"ParentId\" INTEGER REFERENCES \"SecC_QuotedParent\"(\"Id\"))");
        db.Execute("INSERT INTO \"SecC_QuotedParent\" (\"Id\", \"Name\", \"Legacy\") VALUES (1, 'p', 'x')");
        db.Execute("INSERT INTO \"SecC_QuotedKid\"\"x\" (\"Id\", \"ParentId\") VALUES (10, 1)");
        db.Pragmas.ForeignKeys = true;

        db.Schema.Migrations()
            .Version(1, m => m.TableChanged<SecCQuotedParent>(rebuild: true))
            .Migrate();

        Assert.Equal(1, db.Query<int>("SELECT \"ParentId\" FROM \"SecC_QuotedKid\"\"x\"").First());
        Assert.Equal("p", db.Query<string>("SELECT \"Name\" FROM \"SecC_QuotedParent\"").First());
    }

    [Fact]
    public void ParentRebuildWithReferencingQuotedGrandchildNamePreservesData()
    {
        using TestDatabase db = new();
        db.Execute("CREATE TABLE \"SecC_QuotedGpParent\" (\"Id\" INTEGER PRIMARY KEY, \"Name\" TEXT NOT NULL, \"Legacy\" TEXT)");
        db.Execute("CREATE TABLE \"SecC_QuotedGpKid\" (\"Id\" INTEGER PRIMARY KEY, \"ParentId\" INTEGER REFERENCES \"SecC_QuotedGpParent\"(\"Id\"))");
        db.Execute("CREATE TABLE \"SecC_QuotedGpGrand\"\"x\" (\"Id\" INTEGER PRIMARY KEY, \"KidId\" INTEGER REFERENCES \"SecC_QuotedGpKid\"(\"Id\"))");
        db.Execute("INSERT INTO \"SecC_QuotedGpParent\" (\"Id\", \"Name\", \"Legacy\") VALUES (1, 'p', 'x')");
        db.Execute("INSERT INTO \"SecC_QuotedGpKid\" (\"Id\", \"ParentId\") VALUES (10, 1)");
        db.Execute("INSERT INTO \"SecC_QuotedGpGrand\"\"x\" (\"Id\", \"KidId\") VALUES (100, 10)");
        db.Pragmas.ForeignKeys = true;

        db.Schema.Migrations()
            .Version(1, m => m.TableChanged<SecCQuotedGpParent>(rebuild: true))
            .Migrate();

        Assert.Equal(1, db.Query<int>("SELECT \"ParentId\" FROM \"SecC_QuotedGpKid\"").First());
        Assert.Equal(10, db.Query<int>("SELECT \"KidId\" FROM \"SecC_QuotedGpGrand\"\"x\"").First());
        Assert.Equal("p", db.Query<string>("SELECT \"Name\" FROM \"SecC_QuotedGpParent\"").First());
    }
}
