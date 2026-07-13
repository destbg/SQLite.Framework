using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text.Json.Serialization;
using SQLite.Framework;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("j19es_baskets")]
public sealed class Json19esBasketRow
{
    [Key]
    public int Id { get; set; }

    public List<Json19evPlainFruit> Fruits { get; set; } = [];

    public List<Json19evEmptyEnum> Blanks { get; set; } = [];

    public List<SnFruit?> MaybeFruits { get; set; } = [];
}

[JsonSourceGenerationOptions(UseStringEnumConverter = true)]
[JsonSerializable(typeof(List<SnFruit?>))]
internal partial class Json19esNullableFruitContext : JsonSerializerContext;

public class JsonEnumStorageVariantTests
{
    private static TestDatabase Seed(out List<Json19esBasketRow> rows)
    {
        TestDatabase db = new(b =>
        {
            b.TypeConverters[typeof(List<Json19evPlainFruit>)] = new SQLiteJsonConverter<List<Json19evPlainFruit>>(Json19evContext.Default.ListJson19evPlainFruit);
            b.TypeConverters[typeof(List<Json19evEmptyEnum>)] = new SQLiteJsonConverter<List<Json19evEmptyEnum>>(Json19evContext.Default.ListJson19evEmptyEnum);
            b.TypeConverters[typeof(List<SnFruit?>)] = new SQLiteJsonConverter<List<SnFruit?>>(Json19esNullableFruitContext.Default.ListNullableSnFruit);
        });
        db.Table<Json19esBasketRow>().Schema.CreateTable();
        rows =
        [
            new Json19esBasketRow
            {
                Id = 1,
                Fruits = [Json19evPlainFruit.Pear, Json19evPlainFruit.Apple],
                Blanks = [(Json19evEmptyEnum)3, (Json19evEmptyEnum)1],
                MaybeFruits = [SnFruit.Pear, SnFruit.Apple]
            }
        ];
        db.Table<Json19esBasketRow>().AddRange(rows);
        return db;
    }

    [Fact]
    public void MinOverNumberStoredEnumJsonListMatchesLinq()
    {
        using TestDatabase db = Seed(out List<Json19esBasketRow> rows);

        Json19evPlainFruit expected = rows[0].Fruits.Min();
        Json19evPlainFruit actual = db.Table<Json19esBasketRow>().Select(r => r.Fruits.Min()).First();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void MaxOverEmptyEnumTypeJsonListMatchesLinq()
    {
        using TestDatabase db = Seed(out List<Json19esBasketRow> rows);

        Json19evEmptyEnum expected = rows[0].Blanks.Max();
        Json19evEmptyEnum actual = db.Table<Json19esBasketRow>().Select(r => r.Blanks.Max()).First();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void OrderByOverNumberStoredEnumJsonListMatchesLinq()
    {
        using TestDatabase db = Seed(out List<Json19esBasketRow> rows);

        List<Json19evPlainFruit> expected = rows[0].Fruits.OrderByDescending(f => f).ToList();
        List<Json19evPlainFruit> actual = db.Table<Json19esBasketRow>()
            .Select(r => r.Fruits.OrderByDescending(f => f).ToList())
            .First();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void CapturedStringEnumConstantInLiteralMatchesLinq()
    {
        using TestDatabase db = Seed(out List<Json19esBasketRow> rows);
        SnFruit? fruit = SnFruit.Apple;

        List<SnFruit?> expected = rows[0].MaybeFruits.SelectMany(f => new[] { fruit }).ToList();
        List<SnFruit?> actual = db.Table<Json19esBasketRow>()
            .Select(r => r.MaybeFruits.SelectMany(f => new[] { fruit }).ToList())
            .First();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ThenByOverStringEnumJsonListMatchesLinq()
    {
        using TestDatabase db = Seed(out List<Json19esBasketRow> rows);

        List<SnFruit?> expected = rows[0].MaybeFruits.OrderBy(f => f.HasValue).ThenByDescending(f => f).ToList();
        List<SnFruit?> actual = db.Table<Json19esBasketRow>()
            .Select(r => r.MaybeFruits.OrderBy(f => f.HasValue).ThenByDescending(f => f).ToList())
            .First();

        Assert.Equal(expected, actual);
    }
}
