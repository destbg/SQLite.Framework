using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[JsonSerializable(typeof(List<int>))]
internal partial class TableSelectManyListContext : JsonSerializerContext;

internal sealed class TableSelectManyRow
{
    [Key]
    public int Id { get; set; }

    public List<int> Values { get; set; } = [];
}

public class JsonListTableLevelSelectManyTests
{
    [Fact]
    public void SelectManyOverJsonListColumnThrowsWithClearMessage()
    {
        using TestDatabase db = new(b => b.AddJsonContext(TableSelectManyListContext.Default));
        db.Table<TableSelectManyRow>().Schema.CreateTable();
        db.Table<TableSelectManyRow>().Add(new TableSelectManyRow { Id = 1, Values = [1, 2] });

        Exception? ex = Record.Exception(() => db.Table<TableSelectManyRow>()
            .SelectMany(r => r.Values)
            .ToList());

        Assert.IsType<NotSupportedException>(ex);
        Assert.Equal("SelectMany over the JSON collection column 'Values' is not supported at the query level.", ex.Message);
    }
}
