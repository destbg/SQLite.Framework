using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text.Json.Serialization;
using SQLite.Framework;
using SQLite.Framework.Enums;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("j19ev_rows")]
public sealed class Json19evRow
{
    [Key]
    public int Id { get; set; }

    public List<int> Numbers { get; set; } = [];
}

public enum Json19evEmptyEnum;

public enum Json19evPlainFruit
{
    Pear = 0,
    Apple = 1,
}

public sealed class Json19evPoint
{
    public int X { get; set; }
}

[JsonSerializable(typeof(List<int>))]
[JsonSerializable(typeof(List<long>))]
[JsonSerializable(typeof(List<double>))]
[JsonSerializable(typeof(List<bool>))]
[JsonSerializable(typeof(List<string>))]
[JsonSerializable(typeof(List<int?>))]
[JsonSerializable(typeof(List<decimal>))]
[JsonSerializable(typeof(List<Json19evEmptyEnum>))]
[JsonSerializable(typeof(List<Json19evPlainFruit>))]
[JsonSerializable(typeof(List<char>))]
internal partial class Json19evContext : JsonSerializerContext;

public class JsonInlineArrayElementVariantTests
{
    private static TestDatabase CreateDb(CharStorageMode charStorage = CharStorageMode.Text, EnumStorageMode enumStorage = EnumStorageMode.Integer)
    {
        TestDatabase db = new(b =>
        {
            b.UseCharStorage(charStorage);
            b.UseEnumStorage(enumStorage);
            b.TypeConverters[typeof(List<int>)] = new SQLiteJsonConverter<List<int>>(Json19evContext.Default.ListInt32);
            b.TypeConverters[typeof(List<long>)] = new SQLiteJsonConverter<List<long>>(Json19evContext.Default.ListInt64);
            b.TypeConverters[typeof(List<double>)] = new SQLiteJsonConverter<List<double>>(Json19evContext.Default.ListDouble);
            b.TypeConverters[typeof(List<bool>)] = new SQLiteJsonConverter<List<bool>>(Json19evContext.Default.ListBoolean);
            b.TypeConverters[typeof(List<string>)] = new SQLiteJsonConverter<List<string>>(Json19evContext.Default.ListString);
            b.TypeConverters[typeof(List<int?>)] = new SQLiteJsonConverter<List<int?>>(Json19evContext.Default.ListNullableInt32);
            b.TypeConverters[typeof(List<decimal>)] = new SQLiteJsonConverter<List<decimal>>(Json19evContext.Default.ListDecimal);
            b.TypeConverters[typeof(List<Json19evEmptyEnum>)] = new SQLiteJsonConverter<List<Json19evEmptyEnum>>(Json19evContext.Default.ListJson19evEmptyEnum);
            b.TypeConverters[typeof(List<Json19evPlainFruit>)] = new SQLiteJsonConverter<List<Json19evPlainFruit>>(Json19evContext.Default.ListJson19evPlainFruit);
            b.TypeConverters[typeof(List<char>)] = new SQLiteJsonConverter<List<char>>(Json19evContext.Default.ListChar);
        });
        db.Table<Json19evRow>().Schema.CreateTable();
        db.Table<Json19evRow>().Add(new Json19evRow { Id = 1, Numbers = [1, 2] });
        return db;
    }

    [Fact]
    public void BoundsArrayOfLongMatchesLinq()
    {
        using TestDatabase db = CreateDb();

        List<long> expected = new List<int> { 1, 2 }.SelectMany(x => new long[2]).ToList();
        List<long> actual = db.Table<Json19evRow>()
            .Select(r => r.Numbers.SelectMany(x => new long[2]).ToList())
            .First();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void BoundsArrayOfDoubleMatchesLinq()
    {
        using TestDatabase db = CreateDb();

        List<double> expected = new List<int> { 1, 2 }.SelectMany(x => new double[1]).ToList();
        List<double> actual = db.Table<Json19evRow>()
            .Select(r => r.Numbers.SelectMany(x => new double[1]).ToList())
            .First();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void BoundsArrayOfBoolMatchesLinq()
    {
        using TestDatabase db = CreateDb();

        List<bool> expected = new List<int> { 1, 2 }.SelectMany(x => new bool[1]).ToList();
        List<bool> actual = db.Table<Json19evRow>()
            .Select(r => r.Numbers.SelectMany(x => new bool[1]).ToList())
            .First();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void BoundsArrayOfStringMatchesLinq()
    {
        using TestDatabase db = CreateDb();

        List<string> expected = new List<int> { 1, 2 }.SelectMany(x => new string[1]).ToList();
        List<string> actual = db.Table<Json19evRow>()
            .Select(r => r.Numbers.SelectMany(x => new string[1]).ToList())
            .First();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void BoundsArrayOfNullableIntMatchesLinq()
    {
        using TestDatabase db = CreateDb();

        List<int?> expected = new List<int> { 1, 2 }.SelectMany(x => new int?[1]).ToList();
        List<int?> actual = db.Table<Json19evRow>()
            .Select(r => r.Numbers.SelectMany(x => new int?[1]).ToList())
            .First();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void BoundsArrayOfDecimalMatchesLinq()
    {
        using TestDatabase db = CreateDb();

        List<decimal> expected = new List<int> { 1, 2 }.SelectMany(x => new decimal[1]).ToList();
        List<decimal> actual = db.Table<Json19evRow>()
            .Select(r => r.Numbers.SelectMany(x => new decimal[1]).ToList())
            .First();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void MultiDimensionalBoundsArrayThrows()
    {
        using TestDatabase db = CreateDb();

        NotSupportedException ex = Assert.Throws<NotSupportedException>(() => db.Table<Json19evRow>()
            .Select(r => r.Numbers.Select(x => new int[2, 2]).ToList())
            .First());

        Assert.Equal("A multi-dimensional array is not supported inside a JSON collection query. Use a one-dimensional array.", ex.Message);
    }

    [Fact]
    public void ComputedLengthBoundsArrayThrows()
    {
        using TestDatabase db = CreateDb();

        NotSupportedException ex = Assert.Throws<NotSupportedException>(() => db.Table<Json19evRow>()
            .Select(r => r.Numbers.Select(x => new int[x]).ToList())
            .First());

        Assert.Equal("A new array with a computed length is not supported inside a JSON collection query. Use a constant or captured length.", ex.Message);
    }

    [Fact]
    public void UnsupportedDefaultElementBoundsArrayThrows()
    {
        using TestDatabase db = CreateDb();

        NotSupportedException ex = Assert.Throws<NotSupportedException>(() => db.Table<Json19evRow>()
            .Select(r => r.Numbers.Select(x => new DateTime[1]).ToList())
            .First());

        Assert.Equal("A new array of 'DateTime' filled with default elements is not supported inside a JSON collection query. List the elements explicitly instead.", ex.Message);
    }

    [Fact]
    public void ZeroLengthBoundsArrayMatchesLinq()
    {
        using TestDatabase db = CreateDb();

        List<int> expected = new List<int> { 1, 2 }.SelectMany(x => new int[0]).ToList();
        List<int> actual = db.Table<Json19evRow>()
            .Select(r => r.Numbers.SelectMany(x => new int[0]).ToList())
            .First();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void CapturedObjectWithoutConverterInLiteralThrows()
    {
        using TestDatabase db = CreateDb();
        Json19evPoint point = new() { X = 5 };

        NotSupportedException ex = Assert.Throws<NotSupportedException>(() => db.Table<Json19evRow>()
            .Select(r => r.Numbers.Select(x => new object[] { point }).ToList())
            .First());

        Assert.Equal("A captured value of type 'Json19evPoint' inside an inline array needs a registered JSON converter for that type so it can be written as a JSON element.", ex.Message);
    }

    [Fact]
    public void CapturedCharIntegerStorageInLiteralMatchesLinq()
    {
        using TestDatabase db = CreateDb(CharStorageMode.Integer);
        char letter = 'b';

        List<char> expected = new List<int> { 1, 2 }.SelectMany(x => new[] { letter }).ToList();
        List<char> actual = db.Table<Json19evRow>()
            .Select(r => r.Numbers.SelectMany(x => new[] { letter }).ToList())
            .First();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void CapturedEnumTextStorageInLiteralMatchesLinq()
    {
        using TestDatabase db = CreateDb(enumStorage: EnumStorageMode.Text);
        Json19evPlainFruit fruit = Json19evPlainFruit.Apple;

        List<Json19evPlainFruit> expected = new List<int> { 1, 2 }.SelectMany(x => new[] { fruit }).ToList();
        List<Json19evPlainFruit> actual = db.Table<Json19evRow>()
            .Select(r => r.Numbers.SelectMany(x => new[] { fruit }).ToList())
            .First();

        Assert.Equal(expected, actual);
    }
}
