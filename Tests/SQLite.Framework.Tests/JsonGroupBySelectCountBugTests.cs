using SQLite.Framework;
using SQLite.Framework.Attributes;
using SQLite.Framework.Enums;
using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Helpers;
using SQLite.Framework.Tests.Entities;
using System.Globalization;

namespace SQLite.Framework.Tests;

public class JsonGroupBySelectCountBugTests
{
    [Fact]
    public void GroupBySelectCountThrowsAggregateMisuse()
    {
        using TestDatabase db = new(b => b.TypeConverters[typeof(List<int>)] = new SQLiteJsonConverter<List<int>>(TestJsonContext.Default.ListInt32));
        db.Table<jsonGroupSelectRow>().Schema.CreateTable();
        db.Table<jsonGroupSelectRow>().Add(new jsonGroupSelectRow { Id = 1, Numbers = [5, 3, 5, 8, 3, 3] });
        List<int> source = [5, 3, 5, 8, 3, 3];
        List<int> oracle = source.GroupBy(x => x).Select(g => g.Count()).OrderBy(c => c).ToList();
        List<int> actual = db.Table<jsonGroupSelectRow>().Select(r => r.Numbers.GroupBy(x => x).Select(g => g.Count())).First().OrderBy(c => c).ToList();
        Assert.Equal(oracle, actual);
    }
}

public class jsonGroupSelectRow { [System.ComponentModel.DataAnnotations.Key] public int Id { get; set; } public List<int> Numbers { get; set; } = []; }
