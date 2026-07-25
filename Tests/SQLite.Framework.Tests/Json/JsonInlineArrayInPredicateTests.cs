using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text.Json.Serialization;
using SQLite.Framework;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("j19de_predicate_rows")]
public sealed class Json19dePredicateRow
{
    [Key]
    public int Id { get; set; }

    public List<int> Numbers { get; set; } = [];
}

[JsonSerializable(typeof(List<int>))]
[JsonSerializable(typeof(int[]))]
internal partial class Json19dePredicateContext : JsonSerializerContext;

public class JsonInlineArrayInPredicateTests
{
    private static List<int> SrcNumbers => [1, 2, 3];

    private static TestDatabase CreateDb()
    {
        TestDatabase db = new(b =>
        {
            b.TypeConverters[typeof(List<int>)] =
                new SQLiteJsonConverter<List<int>>(Json19dePredicateContext.Default.ListInt32);
            b.TypeConverters[typeof(int[])] =
                new SQLiteJsonConverter<int[]>(Json19dePredicateContext.Default.Int32Array);
        });
        db.Table<Json19dePredicateRow>().Schema.CreateTable();
        db.Table<Json19dePredicateRow>().Add(new Json19dePredicateRow
        {
            Id = 1,
            Numbers = SrcNumbers
        });
        return db;
    }

    [Fact]
    public void LiteralInsideWherePredicate()
    {
        using TestDatabase db = CreateDb();

        List<int> expected = SrcNumbers.Where(x => new[] { x, 6 }.Max() > 5).ToList();
        List<int> actual = db.Table<Json19dePredicateRow>()
            .Select(r => r.Numbers.Where(x => new[] { x, 6 }.Max() > 5).ToList())
            .First();

        Assert.Equal(expected, actual);
    }
}
