using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[JsonSerializable(typeof(List<DateTime>))]
internal partial class H25gAppendDateContext : JsonSerializerContext;

[Table("H25gAppendDateRows")]
public class H25gAppendDateRow
{
    [Key]
    public int Id { get; set; }

    public List<DateTime> Dates { get; set; } = [];
}

public class JsonListAppendedTemporalElementTests
{
    private static readonly DateTime Extra = new(2024, 9, 9, 10, 11, 12);

    [Fact]
    public void AppendingADateToAJsonListReadsBackAllThreeDates()
    {
        using TestDatabase db = Setup(nameof(AppendingADateToAJsonListReadsBackAllThreeDates));

        List<DateTime> expected = Dates().Append(Extra).ToList();
        List<DateTime> actual = db.Table<H25gAppendDateRow>()
            .Select(r => r.Dates.Append(Extra).ToList())
            .First();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ContainsFindsADateAppendedToAJsonList()
    {
        using TestDatabase db = Setup(nameof(ContainsFindsADateAppendedToAJsonList));

        bool expected = Dates().Append(Extra).Contains(Extra);
        bool actual = db.Table<H25gAppendDateRow>()
            .Select(r => r.Dates.Append(Extra).Contains(Extra))
            .First();

        Assert.Equal(expected, actual);
    }

    private static List<DateTime> Dates()
    {
        return [new DateTime(2024, 1, 15, 1, 2, 3), new DateTime(2024, 5, 6, 7, 8, 9)];
    }

    private static TestDatabase Setup(string methodName)
    {
        TestDatabase db = new(b => b.AddJsonContext(H25gAppendDateContext.Default), methodName);
        db.Table<H25gAppendDateRow>().Schema.CreateTable();
        db.Table<H25gAppendDateRow>().Add(new H25gAppendDateRow { Id = 1, Dates = Dates() });
        return db;
    }
}
