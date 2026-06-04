using SQLite.Framework;
using SQLite.Framework.Attributes;
using SQLite.Framework.Enums;
using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Helpers;
using SQLite.Framework.Tests.Entities;
using System.Globalization;

namespace SQLite.Framework.Tests;

public class JsonTakeWhereOrderBugTests
{
    [Fact]
    public void TakeThenWhereFiltersBeforeLimit()
    {
        using TestDatabase db = new(b => b.TypeConverters[typeof(List<int>)] = new SQLiteJsonConverter<List<int>>(TestJsonContext.Default.ListInt32));
        db.Table<jsonTakeWhereRow>().Schema.CreateTable();
        db.Table<jsonTakeWhereRow>().Add(new jsonTakeWhereRow { Id = 1, Numbers = [5, 3, 8, 1, 9, 2] });
        List<int> source = [5, 3, 8, 1, 9, 2];
        List<int> oracle = source.Take(4).Where(x => x > 2).ToList();
        List<int> actual = db.Table<jsonTakeWhereRow>().Select(r => r.Numbers.Take(4).Where(x => x > 2)).First().ToList();
        Assert.Equal(oracle, actual);
    }
}

public class jsonTakeWhereRow { [System.ComponentModel.DataAnnotations.Key] public int Id { get; set; } public List<int> Numbers { get; set; } = []; }
