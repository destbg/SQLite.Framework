using SQLite.Framework;
using SQLite.Framework.Attributes;
using SQLite.Framework.Enums;
using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Helpers;
using SQLite.Framework.Tests.Entities;
using System.Globalization;

namespace SQLite.Framework.Tests;

public class DoubleNegateMalformedSqlBugTests
{
    [Fact]
    public void DoubleNegateMalformedSql()
    {
        using TestDatabase db = new();
        db.Table<sqlcoreDoubleNegateRow>().Schema.CreateTable();
        List<sqlcoreDoubleNegateRow> rows = new() { new sqlcoreDoubleNegateRow { Id = 1, Value = 5 }, new sqlcoreDoubleNegateRow { Id = 2, Value = -3 } };
        db.Table<sqlcoreDoubleNegateRow>().AddRange(rows);
        List<int> oracle = rows.Select(x => -(-x.Value)).OrderBy(v => v).ToList();
        List<int> actual = db.Table<sqlcoreDoubleNegateRow>().Select(x => -(-x.Value)).OrderBy(v => v).ToList();
        Assert.Equal(oracle, actual);
    }
}

public class sqlcoreDoubleNegateRow { [System.ComponentModel.DataAnnotations.Key] public int Id { get; set; } public int Value { get; set; } }
