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

    [Fact]
    public void NullableMemberOverNullRow_Float()
    {
        using TestDatabase db = new();
        db.Table<NullableFloatEntity>().Schema.CreateTable();
        db.Table<NullableFloatEntity>().Add(new NullableFloatEntity { Id = 1, Value = 1.5f });
        db.Table<NullableFloatEntity>().Add(new NullableFloatEntity { Id = 2, Value = null });

        List<string?> actual = db.Table<NullableFloatEntity>().OrderBy(e => e.Id).Select(e => e.Value.ToString()).ToList();
        List<string?> oracle = new List<NullableFloatEntity> { new() { Id = 1, Value = 1.5f }, new() { Id = 2, Value = null } }
            .OrderBy(e => e.Id).Select(e => e.Value.ToString()).ToList();

        Assert.Equal(oracle, actual);
    }

    [Fact]
    public void NullableMemberOverNullRow_Decimal()
    {
        using TestDatabase db = new();
        db.Table<NullableDecimalEntity>().Schema.CreateTable();
        db.Table<NullableDecimalEntity>().Add(new NullableDecimalEntity { Id = 1, Value = 9.99m });
        db.Table<NullableDecimalEntity>().Add(new NullableDecimalEntity { Id = 2, Value = null });

        List<string?> actual = db.Table<NullableDecimalEntity>().OrderBy(e => e.Id).Select(e => e.Value.ToString()).ToList();
        List<string?> oracle = new List<NullableDecimalEntity> { new() { Id = 1, Value = 9.99m }, new() { Id = 2, Value = null } }
            .OrderBy(e => e.Id).Select(e => e.Value.ToString()).ToList();

        Assert.Equal(oracle, actual);
    }

}

public class NullableDoubleEntity
{
    [System.ComponentModel.DataAnnotations.Key]
    public int Id { get; set; }

    public double? Value { get; set; }
}

public class NullableFloatEntity
{
    [System.ComponentModel.DataAnnotations.Key]
    public int Id { get; set; }

    public float? Value { get; set; }
}

public class NullableDecimalEntity
{
    [System.ComponentModel.DataAnnotations.Key]
    public int Id { get; set; }

    public decimal? Value { get; set; }
}
