using System.Collections.Generic;
using System.Linq;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("ctor_selection_convenience_rows")]
public class ConvenienceConstructorRow
{
    public ConvenienceConstructorRow()
    {
    }

    public ConvenienceConstructorRow(int id, string code)
    {
        Id = id;
        Code = code.Trim();
    }

    [Key]
    public int Id { get; set; }

    public string Code { get; set; } = "";

    public string Note { get; } = "n";
}

public class EntityConstructorSelectionTests
{
    [Fact]
    public void EntityWithConvenienceConstructorReadsBackStoredColumnValues()
    {
        using TestDatabase db = new();
        db.Table<ConvenienceConstructorRow>().Schema.CreateTable();
        db.Table<ConvenienceConstructorRow>().Add(new ConvenienceConstructorRow { Id = 1, Code = " abc " });

        List<ConvenienceConstructorRow> source = [new ConvenienceConstructorRow { Id = 1, Code = " abc " }];
        List<string> expected = source.Select(r => r.Code).ToList();

        List<string> actual = db.Table<ConvenienceConstructorRow>().ToList().Select(r => r.Code).ToList();

        Assert.Equal(expected, actual);
    }
}
