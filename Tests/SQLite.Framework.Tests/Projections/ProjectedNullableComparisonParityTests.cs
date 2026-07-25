using System;
using System.Collections.Generic;
using System.Linq;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public enum ProjEdgeKind
{
    None = 0,
    Low = 1,
    High = 2
}

[Table("ProjEdgeRows")]
public class ProjEdgeRow
{
    [Key]
    [Column("Id")]
    public int Id { get; set; }

    [Column("IntVal")]
    public int IntVal { get; set; }

    [Column("LongVal")]
    public long LongVal { get; set; }

    [Column("DblVal")]
    public double DblVal { get; set; }

    [Column("Text")]
    public string Text { get; set; } = "";

    [Column("NullableInt")]
    public int? NullableInt { get; set; }

    [Column("NullableLong")]
    public long? NullableLong { get; set; }

    [Column("NullableDouble")]
    public double? NullableDouble { get; set; }

    [Column("NullableText")]
    public string? NullableText { get; set; }

    [Column("NullableFlag")]
    public bool? NullableFlag { get; set; }

    [Column("Flag")]
    public bool Flag { get; set; }

    [Column("Kind")]
    public ProjEdgeKind Kind { get; set; }
}

public class ProjectedNullableComparisonParityTests
{
    private static readonly ProjEdgeRow[] Data =
    [
        new ProjEdgeRow
        {
            Id = 1,
            IntVal = 10,
            LongVal = 100L,
            DblVal = 1.5,
            Text = "hello",
            NullableInt = 42,
            NullableLong = 9L,
            NullableDouble = 2.75,
            NullableText = "world",
            NullableFlag = true,
            Flag = true,
            Kind = ProjEdgeKind.Low
        },
        new ProjEdgeRow
        {
            Id = 2,
            IntVal = 20,
            LongVal = 200L,
            DblVal = -2.5,
            Text = "abc",
            NullableInt = null,
            NullableLong = null,
            NullableDouble = null,
            NullableText = null,
            NullableFlag = null,
            Flag = false,
            Kind = ProjEdgeKind.High
        },
        new ProjEdgeRow
        {
            Id = 3,
            IntVal = 5,
            LongVal = 50L,
            DblVal = 0.0,
            Text = "",
            NullableInt = 0,
            NullableLong = 0L,
            NullableDouble = 0.0,
            NullableText = "",
            NullableFlag = false,
            Flag = true,
            Kind = ProjEdgeKind.None
        },
    ];

    private static TestDatabase Db()
    {
        TestDatabase db = new();
        db.Table<ProjEdgeRow>().Schema.CreateTable();
        db.Table<ProjEdgeRow>().AddRange(Data);
        return db;
    }

    private static void Same<T>(Func<IEnumerable<ProjEdgeRow>, IEnumerable<T>> enumerable, Func<IQueryable<ProjEdgeRow>, IQueryable<T>> queryable)
    {
        using TestDatabase db = Db();
        List<T> expected = enumerable(Data.OrderBy(x => x.Id)).ToList();
        List<T> actual = queryable(db.Table<ProjEdgeRow>().OrderBy(x => x.Id)).ToList();
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ProjectedLiftedGreaterNegatedInWhere()
        => Same(q => q.Select(x => new { x.Id, B = x.NullableInt > 8 }).Where(a => !a.B).Select(a => a.Id),
                q => q.Select(x => new { x.Id, B = x.NullableInt > 8 }).Where(a => !a.B).Select(a => a.Id));

    [Fact]
    public void ProjectedLiftedGreaterComparedFalseInWhere()
        => Same(q => q.Select(x => new { x.Id, B = x.NullableInt > 8 }).Where(a => a.B == false).Select(a => a.Id),
                q => q.Select(x => new { x.Id, B = x.NullableInt > 8 }).Where(a => a.B == false).Select(a => a.Id));

    [Fact]
    public void ProjectedLiftedGreaterNotEqualTrueInWhere()
        => Same(q => q.Select(x => new { x.Id, B = x.NullableInt > 8 }).Where(a => a.B != true).Select(a => a.Id),
                q => q.Select(x => new { x.Id, B = x.NullableInt > 8 }).Where(a => a.B != true).Select(a => a.Id));

    [Fact]
    public void ProjectedLiftedLessOrEqualNegatedInWhere()
        => Same(q => q.Select(x => new { x.Id, B = x.NullableInt <= 8 }).Where(a => !a.B).Select(a => a.Id),
                q => q.Select(x => new { x.Id, B = x.NullableInt <= 8 }).Where(a => !a.B).Select(a => a.Id));

    [Fact]
    public void ProjectedLiftedTwoNullableNegatedInWhere()
        => Same(q => q.Select(x => new { x.Id, B = x.NullableInt >= x.NullableLong }).Where(a => !a.B).Select(a => a.Id),
                q => q.Select(x => new { x.Id, B = x.NullableInt >= x.NullableLong }).Where(a => !a.B).Select(a => a.Id));

    [Fact]
    public void SingleLiftedComparisonNegatedInWhere()
        => Same(q => q.Select(x => x.NullableInt > 8).Where(b => !b),
                q => q.Select(x => x.NullableInt > 8).Where(b => !b));

    [Fact]
    public void SingleLiftedComparisonNegatedInSelect()
        => Same(q => q.Select(x => x.NullableInt > 8).Select(b => !b),
                q => q.Select(x => x.NullableInt > 8).Select(b => !b));

    [Fact]
    public void ProjectedLiftedComparisonNegatedInSelect()
        => Same(q => q.Select(x => new { x.Id, B = x.NullableInt > 8 }).Select(a => !a.B),
                q => q.Select(x => new { x.Id, B = x.NullableInt > 8 }).Select(a => !a.B));
}
