using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("SecDNulAddRows")]
public class SecDNulAddRow
{
    [Key]
    public int Id { get; set; }

    public string? Body { get; set; }
}

[Table("SecDNulAttrRows")]
public class SecDNulAttrRow
{
    [Key]
    public int Id { get; set; }

    [DefaultValue("before\0after")]
    public string? Body { get; set; }
}

public class SecDNulStringDefaultTests
{
    [Fact]
    public void AddColumnBackfillsAStringDefaultWithAnEmbeddedNul()
    {
        using TestDatabase db = new();
        db.Execute("CREATE TABLE \"SecDNulAddRows\" (\"Id\" INTEGER PRIMARY KEY)");
        db.Execute("INSERT INTO \"SecDNulAddRows\" (\"Id\") VALUES (1)");

        db.Schema.AddColumn<SecDNulAddRow>("Body", "before\0after");

        List<SecDNulAddRow> rows = db.Table<SecDNulAddRow>().ToList();
        Assert.Single(rows);
        Assert.Equal("before\0after", rows[0].Body);
    }

    [Fact]
    public void CreateTableAppliesAnAttributeDefaultWithAnEmbeddedNul()
    {
        using TestDatabase db = new();

        db.Schema.CreateTable<SecDNulAttrRow>();
        db.Execute("INSERT INTO \"SecDNulAttrRows\" (\"Id\") VALUES (1)");

        List<SecDNulAttrRow> rows = db.Table<SecDNulAttrRow>().ToList();
        Assert.Single(rows);
        Assert.Equal("before\0after", rows[0].Body);
    }
}
