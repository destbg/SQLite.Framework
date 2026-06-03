using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
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

file sealed class YesNoBoolConverter : ISQLiteTypeConverter
{
    public SQLiteColumnType ColumnType => SQLiteColumnType.Text;
    public object? ToDatabase(object? value) => value is bool b ? (b ? "yes" : "no") : value;
    public object? FromDatabase(object? value) => value is string s ? s == "yes" : value;
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

[Table("BoolRow")]
file sealed class BoolRow
{
    [Key]
    public int Id { get; set; }
    public bool Flag { get; set; }
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

    [Fact]
    public void Converter_OverridesBuiltInBoolHandling_OnWriteAndRead()
    {
        using TestDatabase db = new(b => b.AddTypeConverter(typeof(bool), new YesNoBoolConverter()));
        db.Table<BoolRow>().Schema.CreateTable();
        db.Table<BoolRow>().Add(new BoolRow { Id = 1, Flag = true });
        db.Table<BoolRow>().Add(new BoolRow { Id = 2, Flag = false });

        List<string> stored = db.Query<string>("SELECT Flag FROM BoolRow ORDER BY Id", []).ToList();
        Assert.Equal(["yes", "no"], stored);

        bool entityRead = db.Table<BoolRow>().First(r => r.Id == 1).Flag;
        bool projectionRead = db.Table<BoolRow>().Where(r => r.Id == 2).Select(r => r.Flag).First();

        Assert.True(entityRead);
        Assert.False(projectionRead);
    }
}
