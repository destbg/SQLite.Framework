using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using SQLite.Framework;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H24qJsonDateRows")]
public class H24qJsonDateRow
{
    [Key]
    public int Id { get; set; }

    public List<DateTime> Dates { get; set; } = [];
}

public class H24qJsonDateProjection
{
    public int Id { get; set; }

    public DateTime First { get; set; }
}

public class JsonElementThroughCteComparisonTests
{
    [Fact]
    public void JsonDateElementCarriedThroughACteStillMatchesAConstant()
    {
        using TestDatabase db = Setup();
        DateTime sought = new(2023, 6, 1);

        int expected = Rows().Count(r => r.Dates.First() == sought);

        SQLiteCte<H24qJsonDateProjection> cte = db.With(() => db.Table<H24qJsonDateRow>()
            .Select(r => new H24qJsonDateProjection { Id = r.Id, First = r.Dates.First() }));

        int actual = cte.Count(x => x.First == sought);

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void JsonDateElementReadDirectlyMatchesAConstant()
    {
        using TestDatabase db = Setup();
        DateTime sought = new(2023, 6, 1);

        int expected = Rows().Count(r => r.Dates.First() == sought);
        int actual = db.Table<H24qJsonDateRow>().Count(r => r.Dates.First() == sought);

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ScalarJsonDateCteStillMatchesAConstant()
    {
        using TestDatabase db = Setup();
        DateTime sought = new(2023, 6, 1);

        int expected = Rows().Select(r => r.Dates.First()).Count(d => d == sought);

        SQLiteCte<DateTime> cte = db.With(() => db.Table<H24qJsonDateRow>().Select(r => r.Dates.First()));

        int actual = cte.Count(d => d == sought);

        Assert.Equal(expected, actual);
    }

    private static List<H24qJsonDateRow> Rows()
    {
        return
        [
            new H24qJsonDateRow
            {
                Id = 1,
                Dates = [new DateTime(2023, 6, 1), new DateTime(2024, 1, 15)]
            },
            new H24qJsonDateRow
            {
                Id = 2,
                Dates = [new DateTime(2024, 3, 20), new DateTime(2025, 4, 2)]
            }
        ];
    }

    private static TestDatabase Setup()
    {
        TestDatabase db = new(b => b.TypeConverters[typeof(List<DateTime>)] =
            new SQLiteJsonConverter<List<DateTime>>(TestJsonContext.Default.ListDateTime));
        db.Table<H24qJsonDateRow>().Schema.CreateTable();
        db.Table<H24qJsonDateRow>().AddRange(Rows());
        return db;
    }
}
