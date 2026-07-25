using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text.Json.Serialization;
using SQLite.Framework;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[JsonSerializable(typeof(List<int>))]
[JsonSerializable(typeof(List<string>))]
internal partial class H21dJsonListContext : JsonSerializerContext;

[Table("H21dJsonListRows")]
public class H21dJsonListRow
{
    [Key]
    public int Id { get; set; }

    public List<int> Nums { get; set; } = [];

    public List<string> Tags { get; set; } = [];
}

public class StringConcatJsonListSourceParityTests
{
    private static List<H21dJsonListRow> Rows()
    {
        return
        [
            new H21dJsonListRow { Id = 1, Nums = [1, 2, 3], Tags = ["a", "b"] }
        ];
    }

    private static TestDatabase Setup()
    {
        TestDatabase db = new(b =>
        {
            b.TypeConverters[typeof(List<int>)] = new SQLiteJsonConverter<List<int>>(H21dJsonListContext.Default.ListInt32);
            b.TypeConverters[typeof(List<string>)] = new SQLiteJsonConverter<List<string>>(H21dJsonListContext.Default.ListString);
        });
        db.Table<H21dJsonListRow>().Schema.CreateTable();
        db.Table<H21dJsonListRow>().AddRange(Rows());
        return db;
    }

    [Fact]
    public void ConcatOverJsonStringListMatchesLinq()
    {
        using TestDatabase db = Setup();

        List<string> expected = Rows()
            .OrderBy(r => r.Id)
            .Select(r => string.Concat(r.Tags))
            .ToList();

        List<string> actual = db.Table<H21dJsonListRow>()
            .OrderBy(r => r.Id)
            .Select(r => string.Concat(r.Tags))
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ConcatOverJsonIntListMatchesLinq()
    {
        using TestDatabase db = Setup();

        List<string> expected = Rows()
            .OrderBy(r => r.Id)
            .Select(r => string.Concat(r.Nums))
            .ToList();

        List<string> actual = db.Table<H21dJsonListRow>()
            .OrderBy(r => r.Id)
            .Select(r => string.Concat(r.Nums))
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void JoinOverJsonStringListMatchesLinq()
    {
        using TestDatabase db = Setup();

        List<string> expected = Rows()
            .OrderBy(r => r.Id)
            .Select(r => string.Join(",", r.Tags))
            .ToList();

        List<string> actual = db.Table<H21dJsonListRow>()
            .OrderBy(r => r.Id)
            .Select(r => string.Join(",", r.Tags))
            .ToList();

        Assert.Equal(expected, actual);
    }
}
