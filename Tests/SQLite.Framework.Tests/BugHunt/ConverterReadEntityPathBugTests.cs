using System.ComponentModel.DataAnnotations;
using SQLite.Framework.Enums;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests.BugHunt;

internal sealed class UpperBangStringConverter : ISQLiteTypeConverter
{
    public SQLiteColumnType ColumnType => SQLiteColumnType.Text;

    public object? ToDatabase(object? value)
    {
        return value is string s ? s.ToUpperInvariant() : value;
    }

    public object? FromDatabase(object? value)
    {
        return value is string s ? s + "!" : value;
    }
}

internal sealed class ConverterReadRow
{
    [Key]
    public int Id { get; set; }

    public string Name { get; set; } = "";
}

public class ConverterReadEntityPathBugTests
{
    [Fact]
    public void RegisteredStringConverter_AppliedOnEntityRead_LikeProjection()
    {
        UpperBangStringConverter converter = new();
        using TestDatabase db = new(b => b.AddTypeConverter(typeof(string), converter));
        db.Table<ConverterReadRow>().Schema.CreateTable();
        db.Table<ConverterReadRow>().Add(new ConverterReadRow { Id = 1, Name = "hello" });

        string expected = (string)converter.FromDatabase(converter.ToDatabase("hello"))!;
        string entityRead = db.Table<ConverterReadRow>().First().Name;

        Assert.Equal(expected, entityRead);
    }

    [Fact]
    public void RegisteredStringConverter_EntityAndProjectionReadAgree()
    {
        UpperBangStringConverter converter = new();
        using TestDatabase db = new(b => b.AddTypeConverter(typeof(string), converter));
        db.Table<ConverterReadRow>().Schema.CreateTable();
        db.Table<ConverterReadRow>().Add(new ConverterReadRow { Id = 1, Name = "hello" });

        string entityRead = db.Table<ConverterReadRow>().First().Name;
        string projectionRead = db.Table<ConverterReadRow>().Select(e => e.Name).First();

        Assert.Equal(projectionRead, entityRead);
    }
}
