using System.ComponentModel.DataAnnotations;
using SQLite.Framework.Enums;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

file sealed class UpperBangStringConverter : ISQLiteTypeConverter
{
    public SQLiteColumnType ColumnType => SQLiteColumnType.Text;
    public object? ToDatabase(object? value) => value is string s ? s.ToUpperInvariant() : value;
    public object? FromDatabase(object? value) => value is string s ? s + "!" : value;
}

file sealed class PlusThousandUIntConverter : ISQLiteTypeConverter
{
    public SQLiteColumnType ColumnType => SQLiteColumnType.Integer;
    public object? ToDatabase(object? value) => value;
    public object? FromDatabase(object? value) => value is long l ? (uint)(l + 1000) : value;
}

file sealed class StringRow
{
    [Key]
    public int Id { get; set; }
    public string Name { get; set; } = "";
}

file sealed class UIntRow
{
    [Key]
    public int Id { get; set; }
    public uint Value { get; set; }
}

public class ConverterEntityReadTests
{
    [Fact]
    public void StringConverter_AppliedOnEntityRead()
    {
        UpperBangStringConverter converter = new();
        using TestDatabase db = new(b => b.AddTypeConverter(typeof(string), converter));
        db.Table<StringRow>().Schema.CreateTable();
        db.Table<StringRow>().Add(new StringRow { Id = 1, Name = "hello" });

        string expected = (string)converter.FromDatabase(converter.ToDatabase("hello"))!;
        string entityRead = db.Table<StringRow>().First().Name;

        Assert.Equal("HELLO!", expected);
        Assert.Equal(expected, entityRead);
    }

    [Fact]
    public void StringConverter_EntityAndProjectionReadAgree()
    {
        using TestDatabase db = new(b => b.AddTypeConverter(typeof(string), new UpperBangStringConverter()));
        db.Table<StringRow>().Schema.CreateTable();
        db.Table<StringRow>().Add(new StringRow { Id = 1, Name = "hello" });

        string entityRead = db.Table<StringRow>().First().Name;
        string projectionRead = db.Table<StringRow>().Select(e => e.Name).First();

        Assert.Equal(projectionRead, entityRead);
    }

    [Fact]
    public void StringConverter_MultipleRows_AllConvertedOnEntityRead()
    {
        using TestDatabase db = new(b => b.AddTypeConverter(typeof(string), new UpperBangStringConverter()));
        db.Table<StringRow>().Schema.CreateTable();
        db.Table<StringRow>().Add(new StringRow { Id = 1, Name = "a" });
        db.Table<StringRow>().Add(new StringRow { Id = 2, Name = "b" });

        List<string> names = db.Table<StringRow>().OrderBy(r => r.Id).Select(r => r).ToList().Select(r => r.Name).ToList();

        Assert.Equal(["A!", "B!"], names);
    }

    [Fact]
    public void UIntConverter_AppliedOnEntityAndProjectionRead()
    {
        using TestDatabase db = new(b => b.AddTypeConverter(typeof(uint), new PlusThousandUIntConverter()));
        db.Table<UIntRow>().Schema.CreateTable();
        db.Table<UIntRow>().Add(new UIntRow { Id = 1, Value = 5 });

        uint entityRead = db.Table<UIntRow>().First().Value;
        uint projectionRead = db.Table<UIntRow>().Select(r => r.Value).First();

        Assert.Equal(1005u, entityRead);
        Assert.Equal(1005u, projectionRead);
    }

    [Fact]
    public void NoConverter_EntityReadUnchanged()
    {
        using TestDatabase db = new();
        db.Table<StringRow>().Schema.CreateTable();
        db.Table<StringRow>().Add(new StringRow { Id = 1, Name = "plain" });

        Assert.Equal("plain", db.Table<StringRow>().First().Name);
    }
}
