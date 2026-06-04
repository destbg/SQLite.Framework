using SQLite.Framework;
using SQLite.Framework.Attributes;
using SQLite.Framework.Enums;
using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Helpers;
using SQLite.Framework.Tests.Entities;
using System.Globalization;

namespace SQLite.Framework.Tests;

public class JsonTakeLastPredicateBugTests
{
    [Fact]
    public void TakeThenLastWithPredicateFiltersInsideWindow()
    {
        using TestDatabase db = new(b => b.TypeConverters[typeof(List<int>)] = new SQLiteJsonConverter<List<int>>(TestJsonContext.Default.ListInt32));
        db.Table<jsonTakeLastPredRow>().Schema.CreateTable();
        db.Table<jsonTakeLastPredRow>().Add(new jsonTakeLastPredRow { Id = 1, Numbers = [5, 3, 8, 1, 9, 2] });
        List<int> source = [5, 3, 8, 1, 9, 2];
        int oracle = source.Take(4).Last(x => x > 2);
        int actual = db.Table<jsonTakeLastPredRow>().Select(r => r.Numbers.Take(4).Last(x => x > 2)).First();
        Assert.Equal(oracle, actual);
    }
}

public class jsonTakeLastPredRow { [System.ComponentModel.DataAnnotations.Key] public int Id { get; set; } public List<int> Numbers { get; set; } = []; }
