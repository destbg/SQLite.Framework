using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("SlitcItems")]
public class SlitcItem
{
    [Key]
    public int Id { get; set; }

    public string Name { get; set; } = "";
}

public class SchemaListIndexesTableCaseTests
{
    [Fact]
    public void ListIndexesMatchesDifferentCasedTableName()
    {
        using TestDatabase db = new();
        db.Table<SlitcItem>().Schema.CreateTable();
        db.Schema.CreateIndex<SlitcItem>(i => i.Name, name: "IX_Slitc_Name");

        IReadOnlyList<string> indexes = db.Schema.ListIndexes("SLITCITEMS");

        Assert.Contains("IX_Slitc_Name", indexes);
    }
}
