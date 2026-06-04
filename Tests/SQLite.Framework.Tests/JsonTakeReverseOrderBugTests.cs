using SQLite.Framework;
using SQLite.Framework.Attributes;
using SQLite.Framework.Enums;
using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Helpers;
using SQLite.Framework.Tests.Entities;
using System.Globalization;

namespace SQLite.Framework.Tests;

public class JsonTakeReverseOrderBugTests
{
    [Fact]
    public void TakeThenReverseReversesAfterLimit()
    {
        using TestDatabase db = new(b => b.TypeConverters[typeof(List<int>)] = new SQLiteJsonConverter<List<int>>(TestJsonContext.Default.ListInt32));
        db.Table<jsonTakeReverseRow>().Schema.CreateTable();
        db.Table<jsonTakeReverseRow>().Add(new jsonTakeReverseRow { Id = 1, Numbers = [10, 20, 30, 40, 50] });
        List<int> source = [10, 20, 30, 40, 50];
        List<int> oracle = source.Take(3).Reverse().ToList();
        List<int> actual = db.Table<jsonTakeReverseRow>().Select(r => r.Numbers.Take(3).Reverse()).First().ToList();
        Assert.Equal(oracle, actual);
    }
}

public class jsonTakeReverseRow { [System.ComponentModel.DataAnnotations.Key] public int Id { get; set; } public List<int> Numbers { get; set; } = []; }
