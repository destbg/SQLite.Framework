using SQLite.Framework;
using SQLite.Framework.Attributes;
using SQLite.Framework.Enums;
using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Helpers;
using SQLite.Framework.Tests.Entities;
using System.Globalization;

namespace SQLite.Framework.Tests;

public class InlineArrayInConstructorBugTests
{
    [Fact]
    public void InlineArrayInsideConstructorContainsDoesNotThrow()
    {
        using TestDatabase db = new();
        db.Table<helpersArrRow>().Schema.CreateTable();
        db.Table<helpersArrRow>().Add(new helpersArrRow { Id = 1, Value = 2 });
        db.Table<helpersArrRow>().Add(new helpersArrRow { Id = 2, Value = 9 });
        List<helpersArrRow> data = new()
        {
            new helpersArrRow { Id = 1, Value = 2 },
            new helpersArrRow { Id = 2, Value = 9 }
        };
        List<int> oracle = data.Where(x => new helpersArrHolder(new[] { 1, 2, 3 }).Items.Contains(x.Value)).Select(x => x.Id).ToList();
        List<int> actual = db.Table<helpersArrRow>().Where(x => new helpersArrHolder(new[] { 1, 2, 3 }).Items.Contains(x.Value)).Select(x => x.Id).ToList();
        Assert.Equal(oracle, actual);
    }
}

public class helpersArrHolder { public int[] Items; public helpersArrHolder(int[] items) { Items = items; } }

public class helpersArrRow { [System.ComponentModel.DataAnnotations.Key] public int Id { get; set; } public int Value { get; set; } }
