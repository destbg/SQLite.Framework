using System.ComponentModel.DataAnnotations;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

internal sealed class StaticCounterRow
{
    [Key]
    public int Id { get; set; }

    public required string Name { get; set; }

    public static int Shared { get; set; }
}

internal sealed class StaticSelfRow
{
    [Key]
    public int Id { get; set; }

    public required string Name { get; set; }

    public static StaticSelfRow? Empty { get; set; }
}

public class StaticPropertyColumnMappingTests
{
    [Fact]
    public void StaticPropertyIsNotMappedToColumn()
    {
        using TestDatabase db = new();
        db.Table<StaticCounterRow>().Schema.CreateTable();

        List<string> columns = db
            .Query<string>("SELECT name FROM pragma_table_info('StaticCounterRow') ORDER BY name")
            .ToList();
        Assert.Equal(["Id", "Name"], columns);
    }

    [Fact]
    public void StaticSelfTypedPropertyDoesNotBlockEntity()
    {
        using TestDatabase db = new();

        Exception? ex = Record.Exception(() => db.Table<StaticSelfRow>().Schema.CreateTable());
        Assert.Null(ex);
    }
}
