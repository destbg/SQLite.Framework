using System.Collections.Generic;
using System.Linq;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using SQLite.Framework;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[JsonSerializable(typeof(List<int>))]
[JsonSerializable(typeof(List<string>))]
internal partial class JsonSetGroupContext : JsonSerializerContext;

internal sealed class JsonSetGroupRow
{
    [Key] public int Id { get; set; }
    public List<int> Nums { get; set; } = [];
    public List<string> Tags { get; set; } = [];
}

public class JsonCollectionSetAndGroupParityTests
{
    private static TestDatabase CreateDb()
    {
        TestDatabase db = new(b =>
        {
            b.TypeConverters[typeof(List<int>)] = new SQLiteJsonConverter<List<int>>(JsonSetGroupContext.Default.ListInt32);
            b.TypeConverters[typeof(List<string>)] = new SQLiteJsonConverter<List<string>>(JsonSetGroupContext.Default.ListString);
        });
        db.Table<JsonSetGroupRow>().Schema.CreateTable();
        return db;
    }

    [Fact]
    public void DistinctThenGroupByThenCount_MatchesLinqToObjects()
    {
        using TestDatabase db = CreateDb();
        List<JsonSetGroupRow> rows = new() { new() { Id = 1, Nums = new() { 1, 2, 3, 4, 2, 4 } } };
        db.Table<JsonSetGroupRow>().AddRange(rows);

        List<int> expected = rows.OrderBy(r => r.Id).Select(r => r.Nums.Distinct().GroupBy(x => x % 2).Count()).ToList();
        List<int> actual = db.Table<JsonSetGroupRow>().OrderBy(r => r.Id).Select(r => r.Nums.Distinct().GroupBy(x => x % 2).Count()).ToList();
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ExceptWithNullInExclusionList_KeepsNonNullSourceElements()
    {
        using TestDatabase db = CreateDb();
        List<JsonSetGroupRow> rows = new() { new() { Id = 1, Tags = new() { "a", "c" } } };
        db.Table<JsonSetGroupRow>().AddRange(rows);

        List<string> exclusion = new() { "b", null! };
        List<List<string>> expected = rows.OrderBy(r => r.Id).Select(r => r.Tags.Except(exclusion).OrderBy(x => x).ToList()).ToList();
        List<List<string>> actual = db.Table<JsonSetGroupRow>().OrderBy(r => r.Id).Select(r => r.Tags.Except(exclusion).OrderBy(x => x).ToList()).ToList();
        Assert.Equal(expected, actual);
    }
}
