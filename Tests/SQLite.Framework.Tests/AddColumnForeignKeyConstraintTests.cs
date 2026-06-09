using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using SQLite.Framework.Attributes;
using SQLite.Framework.Models;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("FkCol_Parent")]
file sealed class FkColParent
{
    [Key]
    public int Id { get; set; }
}

[Table("FkCol_Child")]
file sealed class FkColChild
{
    [Key]
    public int Id { get; set; }

    public string Note { get; set; } = "";

    [ReferencesTable(typeof(FkColParent))]
    public int? ParentId { get; set; }
}

public class AddColumnForeignKeyConstraintTests
{
    private static void CreateBaseTables(TestDatabase db)
    {
        db.Execute("CREATE TABLE \"FkCol_Parent\" (\"Id\" INTEGER PRIMARY KEY)");
        db.Execute("CREATE TABLE \"FkCol_Child\" (\"Id\" INTEGER PRIMARY KEY, \"Note\" TEXT NOT NULL)");
    }

    [Fact]
    public void AddColumn_ForeignKeyWithoutDefault_KeepsConstraint()
    {
        using TestDatabase db = new();
        CreateBaseTables(db);

        db.Table<FkColChild>().Schema.AddColumn(c => (object?)c.ParentId);

        List<PragmaForeignKey> fks = db.Pragmas.ForeignKeyList("FkCol_Child").ToList();

        Assert.Single(fks);
        Assert.Equal("FkCol_Parent", fks[0].ReferencedTable);
        Assert.Equal("ParentId", fks[0].FromColumn);
    }

    [Fact]
    public void AddColumn_ForeignKeyWithDefaultValue_Throws()
    {
        using TestDatabase db = new();
        CreateBaseTables(db);

        Assert.Throws<InvalidOperationException>(() =>
            db.Table<FkColChild>().Schema.AddColumn(c => (object?)c.ParentId, defaultValue: 1));
    }

    [Fact]
    public void AddColumn_ForeignKeyWithDefaultExpression_Throws()
    {
        using TestDatabase db = new();
        CreateBaseTables(db);

        Assert.Throws<InvalidOperationException>(() =>
            db.Table<FkColChild>().Schema.AddColumn(c => (object?)c.ParentId, () => 1));
    }
}
