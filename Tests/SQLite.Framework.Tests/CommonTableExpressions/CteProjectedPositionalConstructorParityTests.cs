using System;
using System.Linq;
using System.ComponentModel.DataAnnotations;
using SQLite.Framework;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

internal sealed class CteCtorSource
{
    [Key]
    public int Id { get; set; }

    public int A { get; set; }

    public int B { get; set; }
}

internal sealed class CteCtorProjection
{
    public int Value { get; set; }

    public int Next { get; set; }

    public CteCtorProjection()
    {
    }

    public CteCtorProjection(int first, int second)
    {
        Value = first;
        Next = second;
    }
}

public class CteProjectedPositionalConstructorParityTests
{
    [Fact]
    public void NonRecursiveCteWithMismatchedPositionalConstructor_DoesNotReadBack()
    {
        using TestDatabase db = new();
        db.Table<CteCtorSource>().Schema.CreateTable();
        db.Table<CteCtorSource>().Add(new CteCtorSource { Id = 1, A = 7, B = 9 });

        SQLiteCte<CteCtorProjection> cte = db.With(() => db.Table<CteCtorSource>().Select(s => new CteCtorProjection(s.A, s.B)));

        Assert.ThrowsAny<Exception>(() => (from c in cte select new { c.Value, c.Next }).ToList());
    }

    [Fact]
    public void NonRecursiveCteWithMemberInitProjection_ReadsBack()
    {
        using TestDatabase db = new();
        db.Table<CteCtorSource>().Schema.CreateTable();
        db.Table<CteCtorSource>().Add(new CteCtorSource { Id = 1, A = 7, B = 9 });

        SQLiteCte<CteCtorProjection> cte = db.With(() => db.Table<CteCtorSource>().Select(s => new CteCtorProjection { Value = s.A, Next = s.B }));
        var actual = (from c in cte select new { c.Value, c.Next }).Single();

        Assert.Equal(7, actual.Value);
        Assert.Equal(9, actual.Next);
    }
}
