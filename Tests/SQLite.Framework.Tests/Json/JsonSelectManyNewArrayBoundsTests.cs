using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text.Json.Serialization;
using SQLite.Framework;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("j19dd_bounds_rows")]
public sealed class Json19ddBoundsRow
{
    [Key]
    public int Id { get; set; }

    public List<int> Numbers { get; set; } = [];
}

[JsonSerializable(typeof(List<int>))]
[JsonSerializable(typeof(List<int[]>))]
internal partial class Json19ddBoundsContext : JsonSerializerContext;

public class JsonSelectManyNewArrayBoundsTests
{
    private static List<int> SrcNumbers => [1, 2, 3];

    private static TestDatabase CreateDb()
    {
        TestDatabase db = new(b =>
        {
            b.TypeConverters[typeof(List<int>)] =
                new SQLiteJsonConverter<List<int>>(Json19ddBoundsContext.Default.ListInt32);
            b.TypeConverters[typeof(List<int[]>)] =
                new SQLiteJsonConverter<List<int[]>>(Json19ddBoundsContext.Default.ListInt32Array);
        });
        db.Table<Json19ddBoundsRow>().Schema.CreateTable();
        db.Table<Json19ddBoundsRow>().Add(new Json19ddBoundsRow
        {
            Id = 1,
            Numbers = SrcNumbers
        });
        return db;
    }

    [Fact]
    public void SelectManyEmptyArrayWithZeroBound()
    {
        using TestDatabase db = CreateDb();

        List<int> expected = SrcNumbers.SelectMany(x => new int[0]).ToList();
        List<int> actual = db.Table<Json19ddBoundsRow>()
            .Select(r => r.Numbers.SelectMany(x => new int[0]).ToList())
            .First();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void SelectManyArrayWithNonZeroBound()
    {
        using TestDatabase db = CreateDb();

        List<int> expected = SrcNumbers.SelectMany(x => new int[2]).ToList();
        List<int> actual = db.Table<Json19ddBoundsRow>()
            .Select(r => r.Numbers.SelectMany(x => new int[2]).ToList())
            .First();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void SelectArrayWithZeroBound()
    {
        using TestDatabase db = CreateDb();

        List<int[]> expected = SrcNumbers.Select(x => new int[0]).ToList();
        List<int[]> actual = db.Table<Json19ddBoundsRow>()
            .Select(r => r.Numbers.Select(x => new int[0]).ToList())
            .First();

        Assert.Equal(expected, actual);
    }
}
