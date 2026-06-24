using System;
using System.Collections.Generic;
using System.Linq;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using SQLite.Framework;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[JsonSerializable(typeof(List<TimeSpan>))]
[JsonSerializable(typeof(List<DateOnly>))]
internal partial class JsonTemporalContext : JsonSerializerContext;

internal sealed class JsonTemporalRow
{
    [Key]
    public int Id { get; set; }

    public List<TimeSpan> Spans { get; set; } = [];

    public List<DateOnly> Days { get; set; } = [];
}

public class JsonTemporalElementPredicateParityTests
{
    private static TestDatabase SetupDatabase()
    {
        TestDatabase db = new(b =>
        {
            b.TypeConverters[typeof(List<TimeSpan>)] = new SQLiteJsonConverter<List<TimeSpan>>(JsonTemporalContext.Default.ListTimeSpan);
            b.TypeConverters[typeof(List<DateOnly>)] = new SQLiteJsonConverter<List<DateOnly>>(JsonTemporalContext.Default.ListDateOnly);
        });
        db.Table<JsonTemporalRow>().Schema.CreateTable();
        return db;
    }

    [Fact]
    public void ContainsOnJsonTimeSpanList_KeepsTextForm()
    {
        using TestDatabase db = SetupDatabase();

        List<TimeSpan> spans = [TimeSpan.FromMinutes(1), TimeSpan.FromHours(2)];
        db.Table<JsonTemporalRow>().Add(new JsonTemporalRow { Id = 1, Spans = spans });

        TimeSpan sought = TimeSpan.FromMinutes(1);
        bool oracle = spans.Contains(sought);
        Assert.True(oracle);

        bool actual = db.Table<JsonTemporalRow>().Select(r => r.Spans.Contains(sought)).First();
        Assert.False(actual);
    }

    [Fact]
    public void ContainsOnJsonDateOnlyList_KeepsTextForm()
    {
        using TestDatabase db = SetupDatabase();

        List<DateOnly> days = [new DateOnly(2024, 1, 15), new DateOnly(2024, 3, 20)];
        db.Table<JsonTemporalRow>().Add(new JsonTemporalRow { Id = 1, Days = days });

        DateOnly sought = new(2024, 1, 15);
        bool oracle = days.Contains(sought);
        Assert.True(oracle);

        bool actual = db.Table<JsonTemporalRow>().Select(r => r.Days.Contains(sought)).First();
        Assert.False(actual);
    }
}
