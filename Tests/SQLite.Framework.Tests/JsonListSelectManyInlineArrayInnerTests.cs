using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text.Json.Serialization;
using SQLite.Framework;
using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("json_selectmany_inline_array")]
public sealed class JsonSelectManyInlineArrayRow
{
    [Key]
    public int Id { get; set; }

    public List<int> Numbers { get; set; } = [];
}

[JsonSerializable(typeof(List<int>))]
internal partial class JsonSelectManyInlineArrayContext : JsonSerializerContext;

public class JsonListSelectManyInlineArrayInnerTests
{
    private static TestDatabase NumbersDb(List<int> src)
    {
        TestDatabase db = new(b =>
            b.TypeConverters[typeof(List<int>)] =
                new SQLiteJsonConverter<List<int>>(JsonSelectManyInlineArrayContext.Default.ListInt32));
        db.Table<JsonSelectManyInlineArrayRow>().Schema.CreateTable();
        db.Table<JsonSelectManyInlineArrayRow>().Add(new JsonSelectManyInlineArrayRow { Id = 1, Numbers = src });
        return db;
    }

    [Fact]
    public void SelectManyInlineArrayInner()
    {
        List<int> src = [3, 1, 2];
        using TestDatabase db = NumbersDb(src);

        List<int> oracle = src.SelectMany(x => new[] { x, x * 10 }).ToList();
        List<int> actual = db.Table<JsonSelectManyInlineArrayRow>()
            .Select(r => r.Numbers.SelectMany(x => new[] { x, x * 10 }).ToList())
            .First();

        Assert.Equal(oracle, actual);
    }

    [Fact]
    public void SelectManyInlineArrayInnerWithResultSelector()
    {
        List<int> src = [3, 1, 2];
        using TestDatabase db = NumbersDb(src);

        List<int> oracle = src.SelectMany(x => new[] { x, x * 10 }, (x, y) => x + y).ToList();
        List<int> actual = db.Table<JsonSelectManyInlineArrayRow>()
            .Select(r => r.Numbers.SelectMany(x => new[] { x, x * 10 }, (x, y) => x + y).ToList())
            .First();

        Assert.Equal(oracle, actual);
    }
}
