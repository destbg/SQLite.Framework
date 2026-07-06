using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("SchemaCaseItems")]
public class SchemaCaseItem
{
    [Key]
    public int Id { get; set; }

    public string Name { get; set; } = "";
}

[Table("vSchemaCaseSlim")]
public class SchemaCaseSlim
{
    public int Id { get; set; }
}

public class SchemaExistenceCaseTests
{
    [Fact]
    public void ViewExistsMatchesDifferentCasedName()
    {
        using TestDatabase db = new();
        db.Table<SchemaCaseItem>().Schema.CreateTable();
        db.Schema.CreateView<SchemaCaseSlim>(() =>
            from i in db.Table<SchemaCaseItem>()
            select new SchemaCaseSlim { Id = i.Id });

        Exception? clash = Record.Exception(() => db.Execute("CREATE VIEW \"VSCHEMACASESLIM\" AS SELECT 1 AS x"));
        Assert.NotNull(clash);

        Assert.True(db.Schema.TableExists("schemacaseitems"));
        Assert.True(db.Schema.ViewExists("VSCHEMACASESLIM"));
    }

    [Fact]
    public void IndexExistsMatchesDifferentCasedName()
    {
        using TestDatabase db = new();
        db.Table<SchemaCaseItem>().Schema.CreateTable();
        db.Schema.CreateIndex<SchemaCaseItem>(i => i.Name, name: "IX_SchemaCase_Name");

        Exception? clash = Record.Exception(() => db.Execute("CREATE INDEX \"ix_schemacase_name\" ON \"SchemaCaseItems\" (\"Id\")"));
        Assert.NotNull(clash);

        Assert.True(db.Schema.IndexExists("ix_schemacase_name"));
    }

    [Fact]
    public void ColumnExistsMatchesDifferentCasedName()
    {
        using TestDatabase db = new();
        db.Table<SchemaCaseItem>().Schema.CreateTable();

        Exception? clash = Record.Exception(() => db.Execute("ALTER TABLE \"SchemaCaseItems\" ADD COLUMN \"name\" TEXT"));
        Assert.NotNull(clash);

        Assert.True(db.Schema.ColumnExists<SchemaCaseItem>("name"));
    }
}
