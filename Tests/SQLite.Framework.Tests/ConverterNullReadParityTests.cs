using System;
using System.ComponentModel.DataAnnotations;
using SQLite.Framework;
using SQLite.Framework.Enums;
using SQLite.Framework.Tests.Helpers;
using Xunit;

namespace SQLite.Framework.Tests;

public sealed class SmcEdgeNullToValueConverter : ISQLiteTypeConverter
{
    public SQLiteColumnType ColumnType => SQLiteColumnType.Integer;

    public object? ToDatabase(object? value)
    {
        return value is SmcEdgeMaybe m ? (m.Present ? (object)(long)m.Value : null) : null;
    }

    public object? FromDatabase(object? value)
    {
        return value is long l ? new SmcEdgeMaybe(true, (int)l) : new SmcEdgeMaybe(false, -7);
    }
}

public readonly struct SmcEdgeMaybe : IEquatable<SmcEdgeMaybe>
{
    public SmcEdgeMaybe(bool present, int value)
    {
        Present = present;
        Value = value;
    }

    public bool Present { get; }
    public int Value { get; }

    public bool Equals(SmcEdgeMaybe other) => Present == other.Present && Value == other.Value;
    public override bool Equals(object? obj) => obj is SmcEdgeMaybe m && Equals(m);
    public override int GetHashCode() => HashCode.Combine(Present, Value);
    public static bool operator ==(SmcEdgeMaybe a, SmcEdgeMaybe b) => a.Equals(b);
    public static bool operator !=(SmcEdgeMaybe a, SmcEdgeMaybe b) => !a.Equals(b);
}

public sealed class SmcEdgeMaybeRow
{
    [Key]
    public int Id { get; set; }
    public SmcEdgeMaybe Score { get; set; }
}

public sealed class ConverterNullReadParityTests
{
    [Fact]
    public void ConverterNotRunOnStoredNull()
    {
        using ModelTestDatabase db = new(
            model => model.Entity<SmcEdgeMaybeRow>().IsRequired(r => r.Score, false),
            b => b.AddTypeConverter<SmcEdgeMaybe>(new SmcEdgeNullToValueConverter()));
        db.Table<SmcEdgeMaybeRow>().Schema.CreateTable();
        db.Execute("INSERT INTO SmcEdgeMaybeRow (\"Id\", \"Score\") VALUES (1, NULL)");

        SmcEdgeMaybe expected = default;

        SmcEdgeMaybe actual = db.Table<SmcEdgeMaybeRow>().Select(r => r.Score).First();

        Assert.Equal(expected, actual);
        Assert.Equal(new SmcEdgeMaybe(false, 0), actual);
    }
}
