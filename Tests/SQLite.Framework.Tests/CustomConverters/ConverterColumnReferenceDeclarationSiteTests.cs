using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using SQLite.Framework;
using SQLite.Framework.Enums;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public readonly struct H24qRefPoints
{
    public H24qRefPoints(int n)
    {
        N = n;
    }

    public int N { get; }

    public static bool operator ==(H24qRefPoints a, H24qRefPoints b) => a.N == b.N;

    public static bool operator !=(H24qRefPoints a, H24qRefPoints b) => a.N != b.N;

    public override bool Equals(object? obj) => obj is H24qRefPoints p && p.N == N;

    public override int GetHashCode() => N;
}

public sealed class H24qRefPointsConverter : ISQLiteTypeConverter
{
    public SQLiteColumnType ColumnType => SQLiteColumnType.Integer;

    public string ParameterSqlExpression => "(({0}) + 1000)";

    public string ColumnSqlExpression => "(({0}) - 1000)";

    public object? ToDatabase(object? value) => value is H24qRefPoints p ? (long)p.N : null;

    public object? FromDatabase(object? value) => value is long l ? new H24qRefPoints((int)l) : new H24qRefPoints(0);
}

[Table("H24qRefComputedRows")]
public class H24qRefComputedRow
{
    [Key]
    public int Id { get; set; }

    public H24qRefPoints Pts { get; set; }

    public H24qRefPoints Mirror { get; set; }
}

[Table("H24qRefCheckRows")]
public class H24qRefCheckRow
{
    [Key]
    public int Id { get; set; }

    public H24qRefPoints Pts { get; set; }
}

[Table("H24qRefIndexRows")]
public class H24qRefIndexRow
{
    [Key]
    public int Id { get; set; }

    public H24qRefPoints Pts { get; set; }

    public string Name { get; set; } = "";
}

public class ConverterColumnReferenceDeclarationSiteTests
{
    [Fact]
    public void ComputedColumnReadingItsSourceByColumnReferenceKeepsTheValue()
    {
        using ModelTestDatabase db = new(
            mb => mb.Entity<H24qRefComputedRow>()
                .Computed(r => r.Mirror, r => SQLiteColumn.Of<H24qRefPoints>(r, "Pts")),
            b => b.AddTypeConverter<H24qRefPoints>(new H24qRefPointsConverter()));
        db.Table<H24qRefComputedRow>().Schema.CreateTable();

        List<H24qRefComputedRow> rows =
        [
            new H24qRefComputedRow { Id = 1, Pts = new H24qRefPoints(5) },
            new H24qRefComputedRow { Id = 2, Pts = new H24qRefPoints(7) }
        ];
        db.Table<H24qRefComputedRow>().AddRange(rows);

        List<int> expected = rows.OrderBy(r => r.Id).Select(r => r.Pts.N).ToList();
        List<int> actual = db.Table<H24qRefComputedRow>()
            .OrderBy(r => r.Id)
            .Select(r => r.Mirror)
            .ToList()
            .Select(p => p.N)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void CheckConstraintOverAColumnReferenceRejectsTheSameRowAsTheProperty()
    {
        H24qRefPoints five = new(5);
        using ModelTestDatabase db = new(
            mb => mb.Entity<H24qRefCheckRow>().Check(r => SQLiteColumn.Of<H24qRefPoints>(r, "Pts") != five),
            b => b.AddTypeConverter<H24qRefPoints>(new H24qRefPointsConverter()));
        db.Table<H24qRefCheckRow>().Schema.CreateTable();

        db.Table<H24qRefCheckRow>().Add(new H24qRefCheckRow { Id = 1, Pts = new H24qRefPoints(7) });
        Assert.ThrowsAny<Exception>(() =>
            db.Table<H24qRefCheckRow>().Add(new H24qRefCheckRow { Id = 2, Pts = new H24qRefPoints(5) }));

        Assert.Equal(1, db.Table<H24qRefCheckRow>().Count());
    }

    [Fact]
    public void PartialUniqueIndexFilteredByAColumnReferenceCoversTheSameRows()
    {
        H24qRefPoints five = new(5);
        using ModelTestDatabase db = new(
            mb => mb.Entity<H24qRefIndexRow>()
                .Index(r => r.Name, name: "h24q_ref_idx", unique: true, filter: r => SQLiteColumn.Of<H24qRefPoints>(r, "Pts") == five),
            b => b.AddTypeConverter<H24qRefPoints>(new H24qRefPointsConverter()));
        db.Table<H24qRefIndexRow>().Schema.CreateTable();

        db.Table<H24qRefIndexRow>().Add(new H24qRefIndexRow { Id = 1, Pts = new H24qRefPoints(5), Name = "x" });
        db.Table<H24qRefIndexRow>().Add(new H24qRefIndexRow { Id = 2, Pts = new H24qRefPoints(7), Name = "x" });
        Assert.ThrowsAny<Exception>(() =>
            db.Table<H24qRefIndexRow>().Add(new H24qRefIndexRow { Id = 3, Pts = new H24qRefPoints(5), Name = "x" }));

        Assert.Equal(2, db.Table<H24qRefIndexRow>().Count());
    }
}
