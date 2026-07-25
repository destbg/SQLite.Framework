using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text.Json.Serialization;
using SQLite.Framework;
using SQLite.Framework.Enums;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("j19db_temporal_rows")]
public sealed class Json19dbTemporalRow
{
    [Key]
    public int Id { get; set; }

    public DateTime Stamp { get; set; }

    public List<int> Numbers { get; set; } = [];

    public List<DateTime> Dates { get; set; } = [];
}

[Table("j19db_decimal_rows")]
public sealed class Json19dbDecimalRow
{
    [Key]
    public int Id { get; set; }

    public List<decimal> Values { get; set; } = [];
}

[JsonSerializable(typeof(List<int>))]
[JsonSerializable(typeof(List<DateTime>))]
[JsonSerializable(typeof(List<DateTime[]>))]
[JsonSerializable(typeof(List<decimal>))]
[JsonSerializable(typeof(List<decimal[]>))]
internal partial class Json19dbStorageFormContext : JsonSerializerContext;

public class JsonInlineArrayStorageFormElementTests
{
    private static readonly DateTime BaseDate = new(2024, 3, 1, 10, 30, 0);

    private static List<DateTime> SrcDates => [BaseDate, BaseDate.AddDays(1)];

    private static List<int> SrcNumbers => [1, 2, 3];

    private static TestDatabase CreateTemporalDb()
    {
        TestDatabase db = new(b =>
        {
            b.TypeConverters[typeof(List<int>)] =
                new SQLiteJsonConverter<List<int>>(Json19dbStorageFormContext.Default.ListInt32);
            b.TypeConverters[typeof(List<DateTime>)] =
                new SQLiteJsonConverter<List<DateTime>>(Json19dbStorageFormContext.Default.ListDateTime);
            b.TypeConverters[typeof(List<DateTime[]>)] =
                new SQLiteJsonConverter<List<DateTime[]>>(Json19dbStorageFormContext.Default.ListDateTimeArray);
        });
        db.Table<Json19dbTemporalRow>().Schema.CreateTable();
        db.Table<Json19dbTemporalRow>().Add(new Json19dbTemporalRow
        {
            Id = 1,
            Stamp = BaseDate.AddHours(2),
            Numbers = SrcNumbers,
            Dates = SrcDates
        });
        return db;
    }

    private static TestDatabase CreateDecimalDb()
    {
        TestDatabase db = new(b =>
        {
            b.UseDecimalStorage(DecimalStorageMode.Text);
            b.TypeConverters[typeof(List<decimal>)] =
                new SQLiteJsonConverter<List<decimal>>(Json19dbStorageFormContext.Default.ListDecimal);
            b.TypeConverters[typeof(List<decimal[]>)] =
                new SQLiteJsonConverter<List<decimal[]>>(Json19dbStorageFormContext.Default.ListDecimalArray);
        });
        db.Table<Json19dbDecimalRow>().Schema.CreateTable();
        db.Table<Json19dbDecimalRow>().Add(new Json19dbDecimalRow
        {
            Id = 1,
            Values = [1.5m, 2.5m]
        });
        return db;
    }

    [Fact]
    public void CapturedDateInLiteral()
    {
        using TestDatabase db = CreateTemporalDb();
        DateTime captured = BaseDate.AddDays(3);

        List<DateTime[]> expected = SrcDates.Select(d => new[] { d, captured }).ToList();
        List<DateTime[]> actual = db.Table<Json19dbTemporalRow>()
            .Select(r => r.Dates.Select(d => new[] { d, captured }).ToList())
            .First();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void DateColumnOfRowInLiteralKeepsStorageForm()
    {
        using TestDatabase db = CreateTemporalDb();

        Assert.ThrowsAny<Exception>(() => db.Table<Json19dbTemporalRow>()
            .Select(r => r.Numbers.Select(x => new[] { r.Stamp }).ToList())
            .First());
    }

    [Fact]
    public void CapturedDecimalTextStorageInLiteral()
    {
        using TestDatabase db = CreateDecimalDb();
        decimal captured = 9.25m;
        List<decimal> src = [1.5m, 2.5m];

        List<decimal[]> expected = src.Select(v => new[] { v, captured }).ToList();
        List<decimal[]> actual = db.Table<Json19dbDecimalRow>()
            .Select(r => r.Values.Select(v => new[] { v, captured }).ToList())
            .First();

        Assert.Equal(expected, actual);
    }
}
