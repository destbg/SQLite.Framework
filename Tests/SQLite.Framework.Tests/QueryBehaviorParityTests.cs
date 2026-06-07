using System;
using System.Collections.Generic;
using System.Linq;
using System.ComponentModel.DataAnnotations;
using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public enum SampleUlongEnum : ulong
{
    Zero = 0,
    Mid = 5,
    AboveLongMax = 9223372036854775809,
    Max = 18446744073709551615
}

public class UlongEnumRow
{
    [Key]
    public int Id { get; set; }

    public SampleUlongEnum Value { get; set; }
}

public class JoinLeftRow
{
    [Key]
    public int Id { get; set; }

    public int? Key { get; set; }

    public string Tag { get; set; } = "";
}

public class JoinRightRow
{
    [Key]
    public int Id { get; set; }

    public int? Key { get; set; }

    public string RightTag { get; set; } = "";
}

public class ArrayProjectionRow
{
    [Key]
    public int Id { get; set; }

    public int Value { get; set; }
}

public class QueryBehaviorParityTests
{
    private static readonly Book[] SubqueryBooks =
    [
        new Book { Id = 1, Title = "a", AuthorId = 10, Price = 5.0 },
        new Book { Id = 2, Title = "b", AuthorId = 20, Price = 50.0 },
        new Book { Id = 3, Title = "c", AuthorId = 10, Price = 60.0 },
        new Book { Id = 4, Title = "d", AuthorId = 30, Price = 7.0 },
    ];

    private static readonly JoinLeftRow[] Lefts =
    [
        new JoinLeftRow { Id = 1, Key = 1, Tag = "L1" },
        new JoinLeftRow { Id = 2, Key = null, Tag = "L2" },
        new JoinLeftRow { Id = 3, Key = 9, Tag = "L3" },
    ];

    private static readonly JoinRightRow[] Rights =
    [
        new JoinRightRow { Id = 1, Key = 1, RightTag = "R1" },
        new JoinRightRow { Id = 2, Key = null, RightTag = "R2" },
    ];

    private static readonly UlongEnumRow[] ULongRows =
    [
        new UlongEnumRow { Id = 1, Value = SampleUlongEnum.Max },
        new UlongEnumRow { Id = 2, Value = SampleUlongEnum.Mid },
        new UlongEnumRow { Id = 3, Value = SampleUlongEnum.AboveLongMax },
        new UlongEnumRow { Id = 4, Value = SampleUlongEnum.Zero },
    ];

    [Fact]
    public void SubqueryStoredInVariable_Contains_FiltersByInClause()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().AddRange(SubqueryBooks);

        IEnumerable<int> cheapAuthorIdsMem = SubqueryBooks.Where(b => b.Price < 10).Select(b => b.AuthorId);
        List<int> expected = SubqueryBooks.Where(b => cheapAuthorIdsMem.Contains(b.AuthorId)).Select(b => b.Id).OrderBy(i => i).ToList();

        IQueryable<int> cheapAuthorIds = db.Table<Book>().Where(b => b.Price < 10).Select(b => b.AuthorId);
        List<int> actual = db.Table<Book>().Where(b => cheapAuthorIds.Contains(b.AuthorId)).Select(b => b.Id).OrderBy(i => i).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void InnerJoin_NullableKey_NullDoesNotMatchNull()
    {
        using TestDatabase db = new();
        db.Table<JoinLeftRow>().Schema.CreateTable();
        db.Table<JoinRightRow>().Schema.CreateTable();
        db.Table<JoinLeftRow>().AddRange(Lefts);
        db.Table<JoinRightRow>().AddRange(Rights);

        List<string> expected = (from l in Lefts
                join r in Rights on l.Key equals r.Key
                select l.Tag + "-" + r.RightTag).OrderBy(s => s).ToList();
        List<string> actual = (from l in db.Table<JoinLeftRow>()
                join r in db.Table<JoinRightRow>() on l.Key equals r.Key
                select l.Tag + "-" + r.RightTag).ToList().OrderBy(s => s).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void LeftJoin_NullableKey_NullDoesNotMatchNull()
    {
        using TestDatabase db = new();
        db.Table<JoinLeftRow>().Schema.CreateTable();
        db.Table<JoinRightRow>().Schema.CreateTable();
        db.Table<JoinLeftRow>().AddRange(Lefts);
        db.Table<JoinRightRow>().AddRange(Rights);

        List<string> expected = (from l in Lefts
                join r in Rights on l.Key equals r.Key into g
                from r in g.DefaultIfEmpty()
                select l.Tag + "-" + (r == null ? "none" : r.RightTag)).OrderBy(s => s).ToList();
        List<string> actual = (from l in db.Table<JoinLeftRow>()
                join r in db.Table<JoinRightRow>() on l.Key equals r.Key into g
                from r in g.DefaultIfEmpty()
                select l.Tag + "-" + (r == null ? "none" : r.RightTag)).ToList().OrderBy(s => s).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ObjectCast_OfIntColumn_PreservesInt32RuntimeType()
    {
        using TestDatabase db = new();
        db.Table<NumericType>().Schema.CreateTable();
        db.Table<NumericType>().Add(new NumericType { Id = 1, IntValue = 7 });

        object expected = (object)7;
        object actual = db.Table<NumericType>().Where(x => x.Id == 1).Select(x => (object)x.IntValue).First();

        Assert.Equal(expected.GetType(), actual.GetType());
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ULongBackedEnum_OrderBy_UsesUnsignedOrder()
    {
        using TestDatabase db = new();
        db.Table<UlongEnumRow>().Schema.CreateTable();
        db.Table<UlongEnumRow>().AddRange(ULongRows);

        List<int> expected = ULongRows.OrderBy(x => x.Value).ThenBy(x => x.Id).Select(x => x.Id).ToList();
        List<int> actual = db.Table<UlongEnumRow>().OrderBy(x => x.Value).ThenBy(x => x.Id).Select(x => x.Id).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ULongBackedEnum_GreaterThan_UsesUnsignedCompare()
    {
        using TestDatabase db = new();
        db.Table<UlongEnumRow>().Schema.CreateTable();
        db.Table<UlongEnumRow>().AddRange(ULongRows);

        List<int> expected = ULongRows.Where(x => x.Value > SampleUlongEnum.Mid).OrderBy(x => x.Id).Select(x => x.Id).ToList();
        List<int> actual = db.Table<UlongEnumRow>().Where(x => x.Value > SampleUlongEnum.Mid).OrderBy(x => x.Id).Select(x => x.Id).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void TimeSpanTicks_InArrayProjection_DoesNotThrow()
    {
        using TestDatabase db = new();
        db.Table<NumericType>().Schema.CreateTable();
        db.Table<NumericType>().Add(new NumericType { Id = 1, IntValue = 1 });

        long[] expected = [TimeSpan.FromDays(1.5).Ticks, TimeSpan.FromHours(2.0).Ticks];
        long[] actual = db.Table<NumericType>().Where(x => x.Id == 1)
            .Select(x => new[] { TimeSpan.FromDays(1.5).Ticks, TimeSpan.FromHours(2.0).Ticks })
            .First();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ArrayProjection_WithComputedElements_ComputesEachElement()
    {
        using TestDatabase db = new();
        db.Table<ArrayProjectionRow>().Schema.CreateTable();
        db.Table<ArrayProjectionRow>().AddRange(new[]
        {
            new ArrayProjectionRow { Id = 1, Value = 10 },
            new ArrayProjectionRow { Id = 2, Value = -3 },
            new ArrayProjectionRow { Id = 3, Value = 0 },
        });

        List<int[]> expected = new ArrayProjectionRow[]
        {
            new() { Id = 1, Value = 10 },
            new() { Id = 2, Value = -3 },
            new() { Id = 3, Value = 0 },
        }.OrderBy(x => x.Id).Select(x => new[] { x.Value, -x.Value, x.Value * 2 }).ToList();
        List<int[]> actual = db.Table<ArrayProjectionRow>().OrderBy(x => x.Id).Select(x => new[] { x.Value, -x.Value, x.Value * 2 }).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void DateTimeAddSecondsFractional_MatchesDotNet()
    {
        using TestDatabase db = new();
        db.Table<Author>().Schema.CreateTable();
        DateTime baseValue = new(2000, 1, 2, 3, 4, 5, DateTimeKind.Unspecified);
        db.Table<Author>().Add(new Author { Id = 1, Name = "n", Email = "e", BirthDate = baseValue });

        long expected = baseValue.AddSeconds(0.123456789).Ticks;
        long actual = db.Table<Author>().Where(x => x.Id == 1).Select(x => x.BirthDate.AddSeconds(0.123456789).Ticks).First();

        Assert.Equal(expected, actual);
    }
}
