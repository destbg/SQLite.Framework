using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("ColumnCaseProbe")]
public class ColumnCaseProbeRow
{
    [Key]
    public int Id { get; set; }

    public string BodyText { get; set; } = "";
}

public class SchemaColumnExistsNameCaseTests
{
    [Fact]
    public void FindsAColumnWhenTheAskedNameDiffersInCase()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<ColumnCaseProbeRow>();

        Assert.True(db.Schema.ColumnExists<ColumnCaseProbeRow>("bodytext"));
        Assert.True(db.Schema.ColumnExists<ColumnCaseProbeRow>("BODYTEXT"));
        Assert.False(db.Schema.ColumnExists<ColumnCaseProbeRow>("BodyTextMissing"));
    }
}
