using SQLite.Framework;
using SQLite.Framework.Attributes;
using SQLite.Framework.Enums;
using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Helpers;
using SQLite.Framework.Tests.Entities;
using System.Globalization;

namespace SQLite.Framework.Tests;

public class IntEqualsBoxedLongBugTests
{
    [Fact]
    public void IntEqualsBoxedLongMatchesDotNet()
    {
        using TestDatabase db = new();
        db.Table<sqlmethIntEqualsBoxedRow>().Schema.CreateTable();
        List<sqlmethIntEqualsBoxedRow> rows = new()
        {
            new sqlmethIntEqualsBoxedRow { Id = 1, Value = 5 },
            new sqlmethIntEqualsBoxedRow { Id = 2, Value = 6 },
        };
        db.Table<sqlmethIntEqualsBoxedRow>().AddRange(rows);
        long target = 5L;
        List<int> actual = db.Table<sqlmethIntEqualsBoxedRow>()
            .Where(x => x.Value.Equals(target))
            .OrderBy(x => x.Id)
            .Select(x => x.Id)
            .ToList();
        List<int> oracle = rows
            .Where(x => x.Value.Equals(target))
            .OrderBy(x => x.Id)
            .Select(x => x.Id)
            .ToList();
        Assert.Equal(oracle, actual);
    }
}

public class sqlmethIntEqualsBoxedRow { [System.ComponentModel.DataAnnotations.Key] public int Id { get; set; } public int Value { get; set; } }
