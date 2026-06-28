using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using SQLite.Framework;
using SQLite.Framework.Enums;
using SQLite.Framework.Tests.Helpers;
using Xunit;

namespace SQLite.Framework.Tests;

public readonly struct CnvEqMaybe : IEquatable<CnvEqMaybe>
{
    public CnvEqMaybe(bool present, int value)
    {
        Present = present;
        Value = value;
    }

    public bool Present { get; }
    public int Value { get; }

    public bool Equals(CnvEqMaybe other) => Present == other.Present && Value == other.Value;
    public override bool Equals(object? obj) => obj is CnvEqMaybe m && Equals(m);
    public override int GetHashCode() => HashCode.Combine(Present, Value);
    public static bool operator ==(CnvEqMaybe a, CnvEqMaybe b) => a.Equals(b);
    public static bool operator !=(CnvEqMaybe a, CnvEqMaybe b) => !a.Equals(b);
}

public sealed class CnvEqMaybeConverter : ISQLiteTypeConverter
{
    public SQLiteColumnType ColumnType => SQLiteColumnType.Integer;

    public object? ToDatabase(object? value)
    {
        return value is CnvEqMaybe m ? (m.Present ? (object)(long)m.Value : null) : null;
    }

    public object? FromDatabase(object? value)
    {
        return value is long l ? new CnvEqMaybe(true, (int)l) : new CnvEqMaybe(false, -7);
    }
}

public sealed class CnvEqMaybeRow
{
    [Key]
    public int Id { get; set; }

    public CnvEqMaybe Score { get; set; }
}

public sealed class ConverterNullMappedEqualityParityTests
{
    [Fact]
    public void EqualityAgainstNullMappedValueMatchesLinqToObjects()
    {
        using ModelTestDatabase db = new(
            model => model.Entity<CnvEqMaybeRow>().IsRequired(r => r.Score, false),
            b => b.AddTypeConverter<CnvEqMaybe>(new CnvEqMaybeConverter()));
        db.Table<CnvEqMaybeRow>().Schema.CreateTable();

        CnvEqMaybe sentinel = new(false, -7);
        db.Table<CnvEqMaybeRow>().Add(new CnvEqMaybeRow { Id = 1, Score = sentinel });
        db.Table<CnvEqMaybeRow>().Add(new CnvEqMaybeRow { Id = 2, Score = new CnvEqMaybe(true, 5) });

        List<CnvEqMaybeRow> roundTripped = db.Table<CnvEqMaybeRow>().OrderBy(r => r.Id).ToList();
        List<int> expected = roundTripped.Where(r => r.Score == sentinel).Select(r => r.Id).OrderBy(x => x).ToList();

        List<int> actual = db.Table<CnvEqMaybeRow>().Where(r => r.Score == sentinel).Select(r => r.Id).OrderBy(x => x).ToList();
        List<int> actualReversed = db.Table<CnvEqMaybeRow>().Where(r => sentinel == r.Score).Select(r => r.Id).OrderBy(x => x).ToList();

        Assert.Equal(expected, actual);
        Assert.Equal(expected, actualReversed);
    }
}
