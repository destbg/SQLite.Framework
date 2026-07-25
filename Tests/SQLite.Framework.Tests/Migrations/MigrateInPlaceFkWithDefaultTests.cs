using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Attributes;
using SQLite.Framework.Models;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("MigFkParent")]
file sealed class MigFkParent
{
    [Key]
    public int Id { get; set; }
}

[Table("MigFkChild")]
file sealed class MigFkChild
{
    [Key]
    public int Id { get; set; }

    public string Note { get; set; } = "";

    [DefaultValue(0)]
    [ReferencesTable(typeof(MigFkParent))]
    public int? ParentId { get; set; }
}

public class MigrateInPlaceFkWithDefaultTests
{
    [Fact]
    public void MigrateInPlace_NullableFkColumnWithDefault_PreservesForeignKeyConstraint()
    {
        using TestDatabase db = new();

        db.Execute("CREATE TABLE \"MigFkParent\" (\"Id\" INTEGER PRIMARY KEY)");
        db.Execute("CREATE TABLE \"MigFkChild\" (\"Id\" INTEGER PRIMARY KEY, \"Note\" TEXT NOT NULL)");

        db.Table<MigFkChild>().Schema.Migrate(MigrateMode.InPlace);

        List<PragmaForeignKey> fks = db.Pragmas.ForeignKeyList("MigFkChild").ToList();

        Assert.Single(fks);
        Assert.Equal("MigFkParent", fks[0].ReferencedTable);
    }
}
