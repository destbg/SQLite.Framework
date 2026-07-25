using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using SQLite.Framework;
using SQLite.Framework.Enums;
using SQLite.Framework.Tests.Helpers;
using Xunit;

namespace SQLite.Framework.Tests;

public readonly struct CnvNeMaybe : IEquatable<CnvNeMaybe>
{
    public CnvNeMaybe(bool present, int value)
    {
        Present = present;
        Value = value;
    }

    public bool Present { get; }
    public int Value { get; }

    public bool Equals(CnvNeMaybe other) => Present == other.Present && Value == other.Value;
    public override bool Equals(object? obj) => obj is CnvNeMaybe m && Equals(m);
    public override int GetHashCode() => HashCode.Combine(Present, Value);
    public static bool operator ==(CnvNeMaybe a, CnvNeMaybe b) => a.Equals(b);
    public static bool operator !=(CnvNeMaybe a, CnvNeMaybe b) => !a.Equals(b);
}

public sealed class CnvNeMaybeConverter : ISQLiteTypeConverter
{
    public SQLiteColumnType ColumnType => SQLiteColumnType.Integer;

    public object? ToDatabase(object? value)
    {
        return value is CnvNeMaybe m ? (m.Present ? (object)(long)m.Value : null) : null;
    }

    public object? FromDatabase(object? value)
    {
        return value is long l ? new CnvNeMaybe(true, (int)l) : new CnvNeMaybe(false, -7);
    }
}

public sealed class CnvNeMaybeRow
{
    [Key]
    public int Id { get; set; }

    public CnvNeMaybe Score { get; set; }
}

public sealed class ConverterNullMappedInequalityParityTests
{
    [Fact]
    public void InequalityAgainstNullMappedValueMatchesLinqToObjects()
    {
        using ModelTestDatabase db = new(
            model => model.Entity<CnvNeMaybeRow>().IsRequired(r => r.Score, false),
            b => b.AddTypeConverter<CnvNeMaybe>(new CnvNeMaybeConverter()));
        db.Table<CnvNeMaybeRow>().Schema.CreateTable();

        CnvNeMaybe sentinel = new(false, -7);
        db.Table<CnvNeMaybeRow>().Add(new CnvNeMaybeRow { Id = 1, Score = sentinel });
        db.Table<CnvNeMaybeRow>().Add(new CnvNeMaybeRow { Id = 2, Score = new CnvNeMaybe(true, 5) });
        db.Table<CnvNeMaybeRow>().Add(new CnvNeMaybeRow { Id = 3, Score = new CnvNeMaybe(true, 9) });

        List<CnvNeMaybeRow> roundTripped = db.Table<CnvNeMaybeRow>().OrderBy(r => r.Id).ToList();
        List<int> expected = roundTripped.Where(r => r.Score != sentinel).Select(r => r.Id).OrderBy(x => x).ToList();

        List<int> actual = db.Table<CnvNeMaybeRow>().Where(r => r.Score != sentinel).Select(r => r.Id).OrderBy(x => x).ToList();

        Assert.Equal(expected, actual);
    }
}
