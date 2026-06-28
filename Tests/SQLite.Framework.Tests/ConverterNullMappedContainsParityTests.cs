using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using SQLite.Framework;
using SQLite.Framework.Enums;
using SQLite.Framework.Tests.Helpers;
using Xunit;

namespace SQLite.Framework.Tests;

public readonly struct CnvInMaybe : IEquatable<CnvInMaybe>
{
    public CnvInMaybe(bool present, int value)
    {
        Present = present;
        Value = value;
    }

    public bool Present { get; }
    public int Value { get; }

    public bool Equals(CnvInMaybe other) => Present == other.Present && Value == other.Value;
    public override bool Equals(object? obj) => obj is CnvInMaybe m && Equals(m);
    public override int GetHashCode() => HashCode.Combine(Present, Value);
    public static bool operator ==(CnvInMaybe a, CnvInMaybe b) => a.Equals(b);
    public static bool operator !=(CnvInMaybe a, CnvInMaybe b) => !a.Equals(b);
}

public sealed class CnvInMaybeConverter : ISQLiteTypeConverter
{
    public SQLiteColumnType ColumnType => SQLiteColumnType.Integer;

    public object? ToDatabase(object? value)
    {
        return value is CnvInMaybe m ? (m.Present ? (object)(long)m.Value : null) : null;
    }

    public object? FromDatabase(object? value)
    {
        return value is long l ? new CnvInMaybe(true, (int)l) : new CnvInMaybe(false, -7);
    }
}

public sealed class CnvInMaybeRow
{
    [Key]
    public int Id { get; set; }

    public CnvInMaybe Score { get; set; }
}

public sealed class ConverterNullMappedContainsParityTests
{
    [Fact]
    public void ContainsListWithNullMappedValueMatchesLinqToObjects()
    {
        using ModelTestDatabase db = new(
            model => model.Entity<CnvInMaybeRow>().IsRequired(r => r.Score, false),
            b => b.AddTypeConverter<CnvInMaybe>(new CnvInMaybeConverter()));
        db.Table<CnvInMaybeRow>().Schema.CreateTable();

        CnvInMaybe sentinel = new(false, -7);
        db.Table<CnvInMaybeRow>().Add(new CnvInMaybeRow { Id = 1, Score = sentinel });
        db.Table<CnvInMaybeRow>().Add(new CnvInMaybeRow { Id = 2, Score = new CnvInMaybe(true, 5) });

        List<CnvInMaybe> wanted = new() { sentinel };

        List<CnvInMaybeRow> roundTripped = db.Table<CnvInMaybeRow>().OrderBy(r => r.Id).ToList();
        List<int> expected = roundTripped.Where(r => wanted.Contains(r.Score)).Select(r => r.Id).OrderBy(x => x).ToList();

        List<int> actual = db.Table<CnvInMaybeRow>().Where(r => wanted.Contains(r.Score)).Select(r => r.Id).OrderBy(x => x).ToList();

        Assert.Equal(expected, actual);
    }
}
