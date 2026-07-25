using System.ComponentModel.DataAnnotations;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

internal sealed class LabeledRow
{
    private string sink = "";

    [Key]
    public int Id { get; set; }

    public required string Name { get; set; }

    public string Label => Name + "!";

    public string Discarded
    {
        set => sink = value;
    }

    public string Sink => sink;
}

public class GetterOnlyPropertyColumnMappingTests
{
    [Fact]
    public void GetterOnlyPropertyIsNotMappedToColumn()
    {
        using TestDatabase db = new();
        db.Table<LabeledRow>().Schema.CreateTable();

        List<string> columns = db
            .Query<string>("SELECT name FROM pragma_table_info('LabeledRow') ORDER BY name")
            .ToList();
        Assert.Equal(["Id", "Name"], columns);
    }
}
