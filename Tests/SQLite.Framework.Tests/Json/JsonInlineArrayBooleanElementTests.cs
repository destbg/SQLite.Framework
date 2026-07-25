using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text.Json.Serialization;
using SQLite.Framework;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("j19da_bool_rows")]
public sealed class Json19daBoolRow
{
    [Key]
    public int Id { get; set; }

    public List<int> Numbers { get; set; } = [];

    public List<bool> Flags { get; set; } = [];
}

[JsonSerializable(typeof(List<int>))]
[JsonSerializable(typeof(List<bool>))]
[JsonSerializable(typeof(List<bool[]>))]
[JsonSerializable(typeof(List<List<bool>>))]
internal partial class Json19daBoolContext : JsonSerializerContext;

public class JsonInlineArrayBooleanElementTests
{
    private static List<int> SrcNumbers => [1, 2, 3];

    private static List<bool> SrcFlags => [true, false, true];

    private static TestDatabase CreateDb()
    {
        TestDatabase db = new(b =>
        {
            b.TypeConverters[typeof(List<int>)] =
                new SQLiteJsonConverter<List<int>>(Json19daBoolContext.Default.ListInt32);
            b.TypeConverters[typeof(List<bool>)] =
                new SQLiteJsonConverter<List<bool>>(Json19daBoolContext.Default.ListBoolean);
            b.TypeConverters[typeof(List<bool[]>)] =
                new SQLiteJsonConverter<List<bool[]>>(Json19daBoolContext.Default.ListBooleanArray);
            b.TypeConverters[typeof(List<List<bool>>)] =
                new SQLiteJsonConverter<List<List<bool>>>(Json19daBoolContext.Default.ListListBoolean);
        });
        db.Table<Json19daBoolRow>().Schema.CreateTable();
        db.Table<Json19daBoolRow>().Add(new Json19daBoolRow
        {
            Id = 1,
            Numbers = SrcNumbers,
            Flags = SrcFlags
        });
        return db;
    }

    [Fact]
    public void ComputedBooleanElementsInLiteral()
    {
        using TestDatabase db = CreateDb();

        List<bool[]> expected = SrcNumbers.Select(x => new[] { x > 1, x == 2 }).ToList();
        List<bool[]> actual = db.Table<Json19daBoolRow>()
            .Select(r => r.Numbers.Select(x => new[] { x > 1, x == 2 }).ToList())
            .First();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void BooleanElementReferenceInLiteral()
    {
        using TestDatabase db = CreateDb();

        List<bool[]> expected = SrcFlags.Select(b => new[] { b, !b }).ToList();
        List<bool[]> actual = db.Table<Json19daBoolRow>()
            .Select(r => r.Flags.Select(b => new[] { b, !b }).ToList())
            .First();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void BooleanElementsInListInitLiteral()
    {
        using TestDatabase db = CreateDb();

        List<List<bool>> expected = SrcNumbers.Select(x => new List<bool> { x > 1 }).ToList();
        List<List<bool>> actual = db.Table<Json19daBoolRow>()
            .Select(r => r.Numbers.Select(x => new List<bool> { x > 1 }).ToList())
            .First();

        Assert.Equal(expected, actual);
    }
}
