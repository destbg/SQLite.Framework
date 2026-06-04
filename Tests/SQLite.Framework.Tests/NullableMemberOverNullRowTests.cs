using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using SQLite.Framework;
using SQLite.Framework.Enums;
using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Enums;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class NullableMemberOverNullRowTests
{
    [Fact]
    public void NullableMemberOverNullRow_00()
    {
        using TestDatabase db = new();
        db.Table<NullableEntity>().Schema.CreateTable();
        db.Table<NullableEntity>().Add(new NullableEntity { Id = 1, Value = null });
        db.Table<NullableEntity>().Add(new NullableEntity { Id = 2, Value = 7 });

        List<string?> actual = db.Table<NullableEntity>()
            .OrderBy(e => e.Id)
            .Select(e => e.Value.ToString())
            .ToList();

        List<NullableEntity> source = new()
        {
            new NullableEntity { Id = 1, Value = null },
            new NullableEntity { Id = 2, Value = 7 }
        };
        List<string?> oracle = source
            .OrderBy(e => e.Id)
            .Select(e => e.Value.ToString())
            .ToList();

        Assert.Equal(oracle, actual);
    }

    [Fact]
    public void NullableMemberOverNullRow_01()
    {
        using TestDatabase db = new();
        db.Table<NullableDoubleEntity>().Schema.CreateTable();
        db.Table<NullableDoubleEntity>().Add(new NullableDoubleEntity { Id = 1, Value = null });

        List<string?> actual = db.Table<NullableDoubleEntity>()
            .Select(e => e.Value.ToString())
            .ToList();

        List<NullableDoubleEntity> source = new()
        {
            new NullableDoubleEntity { Id = 1, Value = null }
        };
        List<string?> oracle = source
            .Select(e => e.Value.ToString())
            .ToList();

        Assert.Equal(oracle, actual);
    }

}
public class NullableDoubleEntity
{
    [System.ComponentModel.DataAnnotations.Key]
    public int Id { get; set; }

    public double? Value { get; set; }
}
