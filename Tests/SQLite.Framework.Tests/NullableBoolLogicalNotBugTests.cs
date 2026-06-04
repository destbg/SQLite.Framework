using SQLite.Framework;
using SQLite.Framework.Attributes;
using SQLite.Framework.Enums;
using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Helpers;
using SQLite.Framework.Tests.Entities;
using System.Globalization;

namespace SQLite.Framework.Tests;

public class NullableBoolLogicalNotBugTests
{
    [Fact]
    public void NullableBoolLogicalNotUsesBitwise()
    {
        using TestDatabase db = new();
        db.Table<sqlcoreNullableNotRow>().Schema.CreateTable();
        List<sqlcoreNullableNotRow> rows = new() { new sqlcoreNullableNotRow { Id = 1, Flag = true }, new sqlcoreNullableNotRow { Id = 2, Flag = false }, new sqlcoreNullableNotRow { Id = 3, Flag = null } };
        db.Table<sqlcoreNullableNotRow>().AddRange(rows);
        List<int> oracle = rows.Where(x => (!x.Flag) ?? false).Select(x => x.Id).OrderBy(i => i).ToList();
        List<int> actual = db.Table<sqlcoreNullableNotRow>().Where(x => (!x.Flag) ?? false).Select(x => x.Id).OrderBy(i => i).ToList();
        Assert.Equal(oracle, actual);
    }
}

public class sqlcoreNullableNotRow { [System.ComponentModel.DataAnnotations.Key] public int Id { get; set; } public bool? Flag { get; set; } }
