using System;
using System.Collections.Generic;
using System.Linq;
using System.ComponentModel.DataAnnotations;
using SQLite.Framework;
using SQLite.Framework.Enums;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

internal sealed class MathSignDecimalRow
{
    [Key]
    public int Id { get; set; }

    public decimal Value { get; set; }
}

public class MathSignDecimalTextStorageParityTests
{
    [Fact]
    public void MathSignOfTextStoredDecimal_MatchesLinqToObjects()
    {
        using TestDatabase db = new(b => b.UseDecimalStorage(DecimalStorageMode.Text));
        db.Table<MathSignDecimalRow>().Schema.CreateTable();

        List<MathSignDecimalRow> rows =
        [
            new MathSignDecimalRow { Id = 1, Value = 0.0m },
            new MathSignDecimalRow { Id = 2, Value = -2.5m },
            new MathSignDecimalRow { Id = 3, Value = 2.5m },
        ];
        foreach (MathSignDecimalRow row in rows)
        {
            db.Table<MathSignDecimalRow>().Add(row);
        }

        List<int> oracle = rows.OrderBy(r => r.Id).Select(e => Math.Sign(e.Value)).ToList();
        List<int> actual = db.Table<MathSignDecimalRow>().OrderBy(r => r.Id).Select(e => Math.Sign(e.Value)).ToList();

        Assert.Equal(oracle, actual);
    }

    [Fact]
    public void MathSignOfRealStoredDecimal_MatchesLinqToObjects()
    {
        using TestDatabase db = new();
        db.Table<MathSignDecimalRow>().Schema.CreateTable();

        List<MathSignDecimalRow> rows =
        [
            new MathSignDecimalRow { Id = 1, Value = 0.0m },
            new MathSignDecimalRow { Id = 2, Value = -2.5m },
            new MathSignDecimalRow { Id = 3, Value = 2.5m },
        ];
        foreach (MathSignDecimalRow row in rows)
        {
            db.Table<MathSignDecimalRow>().Add(row);
        }

        List<int> oracle = rows.OrderBy(r => r.Id).Select(e => Math.Sign(e.Value)).ToList();
        List<int> actual = db.Table<MathSignDecimalRow>().OrderBy(r => r.Id).Select(e => Math.Sign(e.Value)).ToList();

        Assert.Equal(oracle, actual);
    }

    [Fact]
    public void MathMinMaxClampOfTextStoredDecimal_MatchesLinqToObjects()
    {
        using TestDatabase db = new(b => b.UseDecimalStorage(DecimalStorageMode.Text));
        db.Table<MathSignDecimalRow>().Schema.CreateTable();
        db.Table<MathSignDecimalRow>().Add(new MathSignDecimalRow { Id = 1, Value = 10.0m });

        decimal min = db.Table<MathSignDecimalRow>().Select(e => Math.Min(e.Value, 5m)).First();
        decimal max = db.Table<MathSignDecimalRow>().Select(e => Math.Max(e.Value, 5m)).First();
        decimal clamp = db.Table<MathSignDecimalRow>().Select(e => Math.Clamp(e.Value, 6m, 8m)).First();

        Assert.Equal(Math.Min(10.0m, 5m), min);
        Assert.Equal(Math.Max(10.0m, 5m), max);
        Assert.Equal(Math.Clamp(10.0m, 6m, 8m), clamp);
    }
}
