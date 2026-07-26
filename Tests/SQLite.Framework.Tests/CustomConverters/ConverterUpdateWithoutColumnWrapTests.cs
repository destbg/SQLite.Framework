using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Enums;
using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public readonly record struct H22xTag(string Value);

public sealed class H22xTagConverter : ISQLiteTypeConverter
{
    public SQLiteColumnType ColumnType => SQLiteColumnType.Text;

    public string? ParameterSqlExpression => "upper({0})";

    public string? ColumnSqlExpression => "{0}";

    public object? ToDatabase(object? value)
    {
        return value is H22xTag tag ? tag.Value : null;
    }

    public object? FromDatabase(object? value)
    {
        return value is string text ? new H22xTag(text) : default(H22xTag);
    }
}

public readonly record struct H22xPlainTag(string Value);

public sealed class H22xPlainTagConverter : ISQLiteTypeConverter
{
    public SQLiteColumnType ColumnType => SQLiteColumnType.Text;

    public object? ToDatabase(object? value)
    {
        return value is H22xPlainTag tag ? tag.Value : null;
    }

    public object? FromDatabase(object? value)
    {
        return value is string text ? new H22xPlainTag(text) : default(H22xPlainTag);
    }
}

[Table("H22xTaggedRows")]
public class H22xTaggedRow
{
    [Key]
    public int Id { get; set; }

    public H22xTag Tag { get; set; }
}

[Table("H22xPlainTaggedRows")]
public class H22xPlainTaggedRow
{
    [Key]
    public int Id { get; set; }

    public H22xPlainTag Tag { get; set; }
}

public class ConverterUpdateWithoutColumnWrapTests
{
    [Fact]
    public void UpdateWithComputedConverterValueSkipsTheWriteWrap()
    {
        using TestDatabase db = new(b => b.TypeConverters[typeof(H22xTag)] = new H22xTagConverter());
        db.Table<H22xTaggedRow>().Schema.CreateTable();
        db.Table<H22xTaggedRow>().Add(new H22xTaggedRow { Id = 1, Tag = new H22xTag("old") });
        H22xTag first = new("fresh");
        H22xTag second = new("other");

        int updated = db.Table<H22xTaggedRow>()
            .Where(r => r.Id == 1)
            .ExecuteUpdate(s => s.Set(r => r.Tag, r => r.Id == 1 ? first : second));

        Assert.Equal(1, updated);
        Assert.Equal(new H22xTag("FRESH"), db.Table<H22xTaggedRow>().Single().Tag);
    }

    [Fact]
    public void UpdateWithConverterWithoutParameterWrapBindsThePlainValue()
    {
        using TestDatabase db = new(b => b.TypeConverters[typeof(H22xPlainTag)] = new H22xPlainTagConverter());
        db.Table<H22xPlainTaggedRow>().Schema.CreateTable();
        db.Table<H22xPlainTaggedRow>().Add(new H22xPlainTaggedRow { Id = 1, Tag = new H22xPlainTag("old") });
        H22xPlainTag first = new("fresh");
        H22xPlainTag second = new("other");

        int updated = db.Table<H22xPlainTaggedRow>()
            .Where(r => r.Id == 1)
            .ExecuteUpdate(s => s.Set(r => r.Tag, r => r.Id == 1 ? first : second));

        Assert.Equal(1, updated);
        Assert.Equal(new H22xPlainTag("fresh"), db.Table<H22xPlainTaggedRow>().Single().Tag);
    }
}
