using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework;
using SQLite.Framework.Enums;
using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public readonly struct PlainTagValue
{
    public PlainTagValue(string text)
    {
        Text = text;
    }

    public string Text { get; }
}

public sealed class PlainTagConverter : ISQLiteTypeConverter
{
    public SQLiteColumnType ColumnType => SQLiteColumnType.Text;

    public object? ToDatabase(object? value)
    {
        return value is PlainTagValue v ? v.Text : null;
    }

    public object? FromDatabase(object? value)
    {
        return value is string s ? new PlainTagValue(s) : new PlainTagValue("");
    }
}

[Table("reconvert_unsupported_rows")]
public sealed class ReconvertUnsupportedRow
{
    [Key]
    public int Id { get; set; }

    public PlainTagValue Tag { get; set; }

    public PlainTagValue? NullableTag { get; set; }

    public int Count { get; set; }
}

public class MigrateReconvertUnsupportedConverterTests
{
    [Fact]
    public void ReconvertThrowsWhenConverterHasNoReEncodableForm()
    {
        using TestDatabase db = new(b => b.AddTypeConverter<PlainTagValue>(new PlainTagConverter()));
        db.Table<ReconvertUnsupportedRow>().Schema.CreateTable();

        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(
            () => db.Table<ReconvertUnsupportedRow>().Schema.Migrate(MigrateMode.Rebuild, m => m.Reconvert(x => x.Tag)));

        Assert.Contains("Tag", ex.Message);
    }

    [Fact]
    public void ReconvertThrowsWhenColumnHasNoConverter()
    {
        using TestDatabase db = new(b => b.AddTypeConverter<PlainTagValue>(new PlainTagConverter()));
        db.Table<ReconvertUnsupportedRow>().Schema.CreateTable();

        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(
            () => db.Table<ReconvertUnsupportedRow>().Schema.Migrate(MigrateMode.Rebuild, m => m.Reconvert(x => x.Count)));

        Assert.Contains("Count", ex.Message);
    }

    [Fact]
    public void ReconvertResolvesTheConverterOfANullableValueColumn()
    {
        using TestDatabase db = new(b => b.AddTypeConverter<PlainTagValue>(new PlainTagConverter()));
        db.Table<ReconvertUnsupportedRow>().Schema.CreateTable();

        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(
            () => db.Table<ReconvertUnsupportedRow>().Schema.Migrate(MigrateMode.Rebuild, m => m.Reconvert(x => x.NullableTag)));

        Assert.Contains("NullableTag", ex.Message);
    }
}
