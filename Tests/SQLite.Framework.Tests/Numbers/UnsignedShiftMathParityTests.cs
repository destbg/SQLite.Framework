using System;
using System.Collections.Generic;
using System.Linq;
using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class UnsignedShiftMathParityTests
{
    private static readonly ulong[] Values =
    [
        0UL, 1UL, 2UL, 7UL, 255UL, 256UL, 1000000UL,
        0x7FFFFFFFFFFFFFFFUL, 0x8000000000000000UL, 0xFFFFFFFFFFFFFFFFUL,
        0xAAAAAAAAAAAAAAAAUL, 0x5555555555555555UL, 0x123456789ABCDEF0UL,
        (1UL << 63) + 1UL, ulong.MaxValue - 1UL, 4294967296UL
    ];

    private static void Seed(TestDatabase db)
    {
        db.Table<NumericType>().Schema.CreateTable();
        for (int i = 0; i < Values.Length; i++)
        {
            db.Table<NumericType>().Add(new NumericType { Id = i + 1, ULongValue = Values[i] });
        }
    }

    [Fact]
    public void UlongRightShift_AcrossValuesAndCounts_MatchesDotNet()
    {
        using TestDatabase db = new();
        Seed(db);

        foreach (int shift in new[] { 0, 1, 2, 3, 4, 7, 8, 15, 16, 31, 32, 33, 47, 48, 62, 63 })
        {
            List<ulong> expected = Values.Select(v => v >> shift).ToList();
            List<ulong> actual = db.Table<NumericType>()
                .OrderBy(n => n.Id)
                .Select(n => n.ULongValue >> shift)
                .ToList();

            Assert.Equal(expected, actual);
        }
    }

    [Fact]
    public void MathMin_Ulong_AcrossPairs_MatchesDotNet()
    {
        using TestDatabase db = new();
        Seed(db);

        foreach (ulong other in new[] { 0UL, 1UL, 1000UL, 0x8000000000000000UL, ulong.MaxValue })
        {
            List<ulong> expected = Values.Select(v => Math.Min(v, other)).ToList();
            List<ulong> actual = db.Table<NumericType>()
                .OrderBy(n => n.Id)
                .Select(n => Math.Min(n.ULongValue, other))
                .ToList();

            Assert.Equal(expected, actual);
        }
    }

    [Fact]
    public void MathMax_Ulong_AcrossPairs_MatchesDotNet()
    {
        using TestDatabase db = new();
        Seed(db);

        foreach (ulong other in new[] { 0UL, 1UL, 1000UL, 0x8000000000000000UL, ulong.MaxValue })
        {
            List<ulong> expected = Values.Select(v => Math.Max(v, other)).ToList();
            List<ulong> actual = db.Table<NumericType>()
                .OrderBy(n => n.Id)
                .Select(n => Math.Max(n.ULongValue, other))
                .ToList();

            Assert.Equal(expected, actual);
        }
    }

    [Fact]
    public void MathClamp_Ulong_AcrossRanges_MatchesDotNet()
    {
        using TestDatabase db = new();
        Seed(db);

        (ulong Min, ulong Max)[] ranges =
        [
            (100UL, 2000UL),
            (0UL, ulong.MaxValue),
            (0x8000000000000000UL, 0xC000000000000000UL),
            (1UL, 0x7FFFFFFFFFFFFFFFUL)
        ];

        foreach ((ulong min, ulong max) in ranges)
        {
            List<ulong> expected = Values.Select(v => Math.Clamp(v, min, max)).ToList();
            List<ulong> actual = db.Table<NumericType>()
                .OrderBy(n => n.Id)
                .Select(n => Math.Clamp(n.ULongValue, min, max))
                .ToList();

            Assert.Equal(expected, actual);
        }
    }
}
