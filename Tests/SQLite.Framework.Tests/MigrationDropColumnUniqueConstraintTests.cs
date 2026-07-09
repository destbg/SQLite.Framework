using System.Linq;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("UniqueDropWidget")]
public class UniqueDropWidgetRow
{
    [Key]
    public int Id { get; set; }

    public string? Name { get; set; }
}

public class MigrationDropColumnUniqueConstraintTests
{
    [Fact]
    public void DropColumnWithInlineUniqueKeepsOtherColumns()
    {
        using TestDatabase db = new(useFile: true);
        db.Execute("CREATE TABLE \"UniqueDropWidget\" (\"Id\" INTEGER PRIMARY KEY, \"Name\" TEXT, \"Code\" TEXT UNIQUE)");
        db.Execute("INSERT INTO \"UniqueDropWidget\" (\"Id\", \"Name\", \"Code\") VALUES (1, 'a', 'x')");

        db.Schema.Migrations()
            .Version(1, m => m.DropColumn<UniqueDropWidgetRow>("Code"))
            .Migrate();

        Assert.Equal("a", db.Table<UniqueDropWidgetRow>().Single().Name);
    }

    [Fact]
    public void DropUnindexedColumnKeepsIndexOnAnotherColumn()
    {
        using TestDatabase db = new(useFile: true);
        db.Execute("CREATE TABLE \"UniqueDropWidget\" (\"Id\" INTEGER PRIMARY KEY, \"Name\" TEXT, \"Extra\" TEXT)");
        db.Execute("CREATE INDEX \"ix_widget_name\" ON \"UniqueDropWidget\" (\"Name\")");
        db.Execute("INSERT INTO \"UniqueDropWidget\" (\"Id\", \"Name\", \"Extra\") VALUES (1, 'a', 'e')");

        db.Schema.Migrations()
            .Version(1, m => m.DropColumn<UniqueDropWidgetRow>("Extra"))
            .Migrate();

        Assert.Equal("a", db.Table<UniqueDropWidgetRow>().Single().Name);
        Assert.True(db.Schema.IndexExists("ix_widget_name"));
    }
}
