using System;
using System.Collections.Generic;
using System.Linq;
using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class AdditionalQueryParityTests
{
    private static readonly JoinLeftRow[] JoinLefts =
    [
        new JoinLeftRow { Id = 1, Key = 1, Tag = "L1" },
        new JoinLeftRow { Id = 2, Key = null, Tag = "L2" },
        new JoinLeftRow { Id = 3, Key = 9, Tag = "L3" },
        new JoinLeftRow { Id = 4, Key = 1, Tag = "L4" },
    ];

    private static readonly JoinRightRow[] JoinRights =
    [
        new JoinRightRow { Id = 1, Key = 1, RightTag = "R1" },
        new JoinRightRow { Id = 2, Key = null, RightTag = "R2" },
        new JoinRightRow { Id = 3, Key = 1, RightTag = "R3" },
    ];

    private static readonly UlongEnumRow[] ULongRows =
    [
        new UlongEnumRow { Id = 1, Value = SampleUlongEnum.Max },
        new UlongEnumRow { Id = 2, Value = SampleUlongEnum.Mid },
        new UlongEnumRow { Id = 3, Value = SampleUlongEnum.AboveLongMax },
        new UlongEnumRow { Id = 4, Value = SampleUlongEnum.Zero },
    ];

    [Fact]
    public void ObjectCast_Int_PreservesRuntimeType()
    {
        using TestDatabase db = new();
        db.Table<NumericType>().Schema.CreateTable();
        db.Table<NumericType>().Add(new NumericType { Id = 1, IntValue = 42 });

        object expected = new[] { new { IntValue = 42 } }.Select(x => (object)x.IntValue).First();
        object actual = db.Table<NumericType>().Select(x => (object)x.IntValue).First();

        Assert.Equal(expected.GetType(), actual.GetType());
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ObjectCast_Long_PreservesRuntimeType()
    {
        using TestDatabase db = new();
        db.Table<NumericType>().Schema.CreateTable();
        db.Table<NumericType>().Add(new NumericType { Id = 1, LongValue = 9000000000L });

        object expected = new[] { new { LongValue = 9000000000L } }.Select(x => (object)x.LongValue).First();
        object actual = db.Table<NumericType>().Select(x => (object)x.LongValue).First();

        Assert.Equal(expected.GetType(), actual.GetType());
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ObjectCast_Short_PreservesRuntimeType()
    {
        using TestDatabase db = new();
        db.Table<NumericType>().Schema.CreateTable();
        db.Table<NumericType>().Add(new NumericType { Id = 1, ShortValue = 7 });

        object expected = new[] { new { ShortValue = (short)7 } }.Select(x => (object)x.ShortValue).First();
        object actual = db.Table<NumericType>().Select(x => (object)x.ShortValue).First();

        Assert.Equal(expected.GetType(), actual.GetType());
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ObjectCast_Double_PreservesRuntimeType()
    {
        using TestDatabase db = new();
        db.Table<NumericType>().Schema.CreateTable();
        db.Table<NumericType>().Add(new NumericType { Id = 1, DoubleValue = 1.5 });

        object expected = new[] { new { DoubleValue = 1.5 } }.Select(x => (object)x.DoubleValue).First();
        object actual = db.Table<NumericType>().Select(x => (object)x.DoubleValue).First();

        Assert.Equal(expected.GetType(), actual.GetType());
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ObjectCast_Bool_PreservesRuntimeType()
    {
        using TestDatabase db = new();
        db.Table<Author>().Schema.CreateTable();
        db.Table<Author>().Add(new Author { Id = 1, Name = "n", Email = "e", BirthDate = new DateTime(2000, 1, 1) });

        object expected = new[] { new { Id = 1 } }.Select(x => (object)(x.Id == 1)).First();
        object actual = db.Table<Author>().Select(x => (object)(x.Id == 1)).First();

        Assert.Equal(expected.GetType(), actual.GetType());
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void DateTime_FractionalAdds_MatchDotNet()
    {
        using TestDatabase db = new();
        db.Table<Author>().Schema.CreateTable();
        DateTime baseValue = new(2000, 1, 2, 3, 4, 5, DateTimeKind.Unspecified);
        db.Table<Author>().Add(new Author { Id = 1, Name = "n", Email = "e", BirthDate = baseValue });

        List<long> expected = new[] { baseValue }.Select(b => new[]
        {
            b.AddSeconds(0.123456789).Ticks,
            b.AddMilliseconds(0.5).Ticks,
            b.AddMinutes(0.25).Ticks,
            b.AddHours(0.1).Ticks,
            b.AddDays(0.123456789).Ticks,
            b.AddMicroseconds(1.5).Ticks,
        }).First().ToList();

        List<long> actual = db.Table<Author>().Where(x => x.Id == 1).Select(x => new[]
        {
            x.BirthDate.AddSeconds(0.123456789).Ticks,
            x.BirthDate.AddMilliseconds(0.5).Ticks,
            x.BirthDate.AddMinutes(0.25).Ticks,
            x.BirthDate.AddHours(0.1).Ticks,
            x.BirthDate.AddDays(0.123456789).Ticks,
            x.BirthDate.AddMicroseconds(1.5).Ticks,
        }).First().ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void DateTime_FractionalAddSeconds_Scalar_MatchesDotNet()
    {
        using TestDatabase db = new();
        db.Table<Author>().Schema.CreateTable();
        DateTime baseValue = new(2010, 6, 7, 8, 9, 10, DateTimeKind.Unspecified);
        db.Table<Author>().Add(new Author { Id = 1, Name = "n", Email = "e", BirthDate = baseValue });

        long expected = new[] { baseValue }.Select(b => b.AddSeconds(2.345).Ticks).First();
        long actual = db.Table<Author>().Where(x => x.Id == 1).Select(x => x.BirthDate.AddSeconds(2.345).Ticks).First();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void OrderBy_WithCustomComparer_Throws()
    {
        using TestDatabase db = new();
        db.Table<Author>().Schema.CreateTable();
        db.Table<Author>().Add(new Author { Id = 1, Name = "n", Email = "e", BirthDate = new DateTime(2000, 1, 1) });

        IComparer<int> comparer = Comparer<int>.Create((a, b) => a.CompareTo(b));

        Assert.Throws<NotSupportedException>(() =>
            db.Table<Author>().OrderBy(x => x.Id, comparer).ToList());
    }

    [Fact]
    public void OrderByDescending_WithCustomComparer_Throws()
    {
        using TestDatabase db = new();
        db.Table<Author>().Schema.CreateTable();
        db.Table<Author>().Add(new Author { Id = 1, Name = "n", Email = "e", BirthDate = new DateTime(2000, 1, 1) });

        IComparer<int> comparer = Comparer<int>.Create((a, b) => a.CompareTo(b));

        Assert.Throws<NotSupportedException>(() =>
            db.Table<Author>().OrderByDescending(x => x.Id, comparer).ToList());
    }

    [Fact]
    public void ThenBy_WithCustomComparer_Throws()
    {
        using TestDatabase db = new();
        db.Table<Author>().Schema.CreateTable();
        db.Table<Author>().Add(new Author { Id = 1, Name = "n", Email = "e", BirthDate = new DateTime(2000, 1, 1) });

        IComparer<int> comparer = Comparer<int>.Create((a, b) => a.CompareTo(b));

        Assert.Throws<NotSupportedException>(() =>
            db.Table<Author>().OrderBy(x => x.Name).ThenBy(x => x.Id, comparer).ToList());
    }

    [Fact]
    public void ULongEnum_OrderByDescending_UsesUnsignedOrder()
    {
        using TestDatabase db = new();
        db.Table<UlongEnumRow>().Schema.CreateTable();
        db.Table<UlongEnumRow>().AddRange(ULongRows);

        List<int> expected = ULongRows.OrderByDescending(x => x.Value).ThenBy(x => x.Id).Select(x => x.Id).ToList();
        List<int> actual = db.Table<UlongEnumRow>().OrderByDescending(x => x.Value).ThenBy(x => x.Id).Select(x => x.Id).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ULongEnum_AllComparisons_MatchDotNet()
    {
        using TestDatabase db = new();
        db.Table<UlongEnumRow>().Schema.CreateTable();
        db.Table<UlongEnumRow>().AddRange(ULongRows);

        List<int> gtExpected = ULongRows.Where(x => x.Value > SampleUlongEnum.Mid).OrderBy(x => x.Id).Select(x => x.Id).ToList();
        List<int> gtActual = db.Table<UlongEnumRow>().Where(x => x.Value > SampleUlongEnum.Mid).OrderBy(x => x.Id).Select(x => x.Id).ToList();
        Assert.Equal(gtExpected, gtActual);

        List<int> geExpected = ULongRows.Where(x => x.Value >= SampleUlongEnum.AboveLongMax).OrderBy(x => x.Id).Select(x => x.Id).ToList();
        List<int> geActual = db.Table<UlongEnumRow>().Where(x => x.Value >= SampleUlongEnum.AboveLongMax).OrderBy(x => x.Id).Select(x => x.Id).ToList();
        Assert.Equal(geExpected, geActual);

        List<int> ltExpected = ULongRows.Where(x => x.Value < SampleUlongEnum.AboveLongMax).OrderBy(x => x.Id).Select(x => x.Id).ToList();
        List<int> ltActual = db.Table<UlongEnumRow>().Where(x => x.Value < SampleUlongEnum.AboveLongMax).OrderBy(x => x.Id).Select(x => x.Id).ToList();
        Assert.Equal(ltExpected, ltActual);

        List<int> leExpected = ULongRows.Where(x => x.Value <= SampleUlongEnum.Mid).OrderBy(x => x.Id).Select(x => x.Id).ToList();
        List<int> leActual = db.Table<UlongEnumRow>().Where(x => x.Value <= SampleUlongEnum.Mid).OrderBy(x => x.Id).Select(x => x.Id).ToList();
        Assert.Equal(leExpected, leActual);
    }

    [Fact]
    public void InnerJoin_NullableKey_MatchesDotNet()
    {
        using TestDatabase db = new();
        db.Table<JoinLeftRow>().Schema.CreateTable();
        db.Table<JoinRightRow>().Schema.CreateTable();
        db.Table<JoinLeftRow>().AddRange(JoinLefts);
        db.Table<JoinRightRow>().AddRange(JoinRights);

        List<string> expected = (from l in JoinLefts
                join r in JoinRights on l.Key equals r.Key
                select l.Tag + "-" + r.RightTag).OrderBy(s => s).ToList();
        List<string> actual = (from l in db.Table<JoinLeftRow>()
                join r in db.Table<JoinRightRow>() on l.Key equals r.Key
                select l.Tag + "-" + r.RightTag).ToList().OrderBy(s => s).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void LeftJoin_NullableKey_MatchesDotNet()
    {
        using TestDatabase db = new();
        db.Table<JoinLeftRow>().Schema.CreateTable();
        db.Table<JoinRightRow>().Schema.CreateTable();
        db.Table<JoinLeftRow>().AddRange(JoinLefts);
        db.Table<JoinRightRow>().AddRange(JoinRights);

        List<string> expected = (from l in JoinLefts
                join r in JoinRights on l.Key equals r.Key into g
                from r in g.DefaultIfEmpty()
                select l.Tag + "-" + (r == null ? "none" : r.RightTag)).OrderBy(s => s).ToList();
        List<string> actual = (from l in db.Table<JoinLeftRow>()
                join r in db.Table<JoinRightRow>() on l.Key equals r.Key into g
                from r in g.DefaultIfEmpty()
                select l.Tag + "-" + (r == null ? "none" : r.RightTag)).ToList().OrderBy(s => s).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void InnerJoin_NonNullKey_StillMatches()
    {
        using TestDatabase db = new();
        db.Table<JoinLeftRow>().Schema.CreateTable();
        db.Table<JoinRightRow>().Schema.CreateTable();
        db.Table<JoinLeftRow>().AddRange(JoinLefts);
        db.Table<JoinRightRow>().AddRange(JoinRights);

        List<string> expected = (from l in JoinLefts
                join r in JoinRights on l.Tag equals r.RightTag
                select l.Tag).OrderBy(s => s).ToList();
        List<string> actual = (from l in db.Table<JoinLeftRow>()
                join r in db.Table<JoinRightRow>() on l.Tag equals r.RightTag
                select l.Tag).ToList().OrderBy(s => s).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void CapturedSubquery_Contains_MatchesDotNet()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        Book[] books =
        [
            new Book { Id = 1, Title = "a", AuthorId = 10, Price = 5.0 },
            new Book { Id = 2, Title = "b", AuthorId = 20, Price = 50.0 },
            new Book { Id = 3, Title = "c", AuthorId = 10, Price = 60.0 },
            new Book { Id = 4, Title = "d", AuthorId = 30, Price = 7.0 },
        ];
        db.Table<Book>().AddRange(books);

        IEnumerable<int> cheapMem = books.Where(b => b.Price < 10).Select(b => b.AuthorId);
        List<int> expected = books.Where(b => cheapMem.Contains(b.AuthorId)).Select(b => b.Id).OrderBy(i => i).ToList();

        IQueryable<int> cheap = db.Table<Book>().Where(b => b.Price < 10).Select(b => b.AuthorId);
        List<int> actual = db.Table<Book>().Where(b => cheap.Contains(b.AuthorId)).Select(b => b.Id).OrderBy(i => i).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void CapturedSubquery_Any_MatchesDotNet()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        Book[] books =
        [
            new Book { Id = 1, Title = "a", AuthorId = 10, Price = 5.0 },
            new Book { Id = 2, Title = "b", AuthorId = 20, Price = 50.0 },
        ];
        db.Table<Book>().AddRange(books);

        IQueryable<Book> cheap = db.Table<Book>().Where(b => b.Price < 10);
        bool expected = books.Where(b => b.Price < 10).Any(b => b.AuthorId == 10);
        bool actual = cheap.Any(b => b.AuthorId == 10);

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void CapturedSubquery_WithWhereChain_Contains_MatchesDotNet()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        Book[] books =
        [
            new Book { Id = 1, Title = "a", AuthorId = 10, Price = 5.0 },
            new Book { Id = 2, Title = "b", AuthorId = 20, Price = 6.0 },
            new Book { Id = 3, Title = "c", AuthorId = 30, Price = 60.0 },
        ];
        db.Table<Book>().AddRange(books);

        IQueryable<int> cheap = db.Table<Book>().Where(b => b.Price < 100).Select(b => b.AuthorId);
        List<int> expected = books.Where(b => books.Where(x => x.Price < 100).Select(x => x.AuthorId).Where(a => a < 25).Contains(b.AuthorId)).Select(b => b.Id).OrderBy(i => i).ToList();
        List<int> actual = db.Table<Book>().Where(b => cheap.Where(a => a < 25).Contains(b.AuthorId)).Select(b => b.Id).OrderBy(i => i).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void CapturedTableLocal_InSubquery_MatchesDotNet()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        Book[] books =
        [
            new Book { Id = 1, Title = "a", AuthorId = 10, Price = 5.0 },
            new Book { Id = 2, Title = "b", AuthorId = 20, Price = 50.0 },
            new Book { Id = 3, Title = "c", AuthorId = 10, Price = 60.0 },
        ];
        db.Table<Book>().AddRange(books);

        SQLiteTable<Book> table = db.Table<Book>();
        List<int> expected = books.Where(b => books.Where(x => x.Price < 10).Select(x => x.AuthorId).Contains(b.AuthorId)).Select(b => b.Id).OrderBy(i => i).ToList();
        List<int> actual = db.Table<Book>().Where(b => table.Where(x => x.Price < 10).Select(x => x.AuthorId).Contains(b.AuthorId)).Select(b => b.Id).OrderBy(i => i).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ArrayProjection_PlainColumns_MatchesDotNet()
    {
        using TestDatabase db = new();
        db.Table<NumericType>().Schema.CreateTable();
        db.Table<NumericType>().AddRange(new[]
        {
            new NumericType { Id = 1, IntValue = 10, ShortValue = 3 },
            new NumericType { Id = 2, IntValue = -4, ShortValue = 9 },
        });

        List<int[]> expected = new[]
        {
            new { Id = 1, IntValue = 10 },
            new { Id = 2, IntValue = -4 },
        }.OrderBy(x => x.Id).Select(x => new[] { x.Id, x.IntValue }).ToList();
        List<int[]> actual = db.Table<NumericType>().OrderBy(x => x.Id).Select(x => new[] { x.Id, x.IntValue }).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ArrayProjection_ComputedElements_MatchesDotNet()
    {
        using TestDatabase db = new();
        db.Table<NumericType>().Schema.CreateTable();
        db.Table<NumericType>().AddRange(new[]
        {
            new NumericType { Id = 1, IntValue = 10 },
            new NumericType { Id = 2, IntValue = -3 },
            new NumericType { Id = 3, IntValue = 0 },
        });

        List<int[]> expected = new[]
        {
            new { Id = 1, IntValue = 10 },
            new { Id = 2, IntValue = -3 },
            new { Id = 3, IntValue = 0 },
        }.OrderBy(x => x.Id).Select(x => new[] { x.IntValue, -x.IntValue, x.IntValue * 2, x.IntValue + 1 }).ToList();
        List<int[]> actual = db.Table<NumericType>().OrderBy(x => x.Id).Select(x => new[] { x.IntValue, -x.IntValue, x.IntValue * 2, x.IntValue + 1 }).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ArrayProjection_LongTicksElements_MatchesDotNet()
    {
        using TestDatabase db = new();
        db.Table<NumericType>().Schema.CreateTable();
        db.Table<NumericType>().Add(new NumericType { Id = 1, IntValue = 1 });

        long[] expected = new[] { new { Id = 1 } }
            .Select(x => new[] { TimeSpan.FromDays(1.5).Ticks, TimeSpan.FromHours(2.0).Ticks, TimeSpan.FromMinutes(3.0).Ticks })
            .First();
        long[] actual = db.Table<NumericType>().Where(x => x.Id == 1)
            .Select(x => new[] { TimeSpan.FromDays(1.5).Ticks, TimeSpan.FromHours(2.0).Ticks, TimeSpan.FromMinutes(3.0).Ticks })
            .First();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ArrayProjection_StringElements_MatchesDotNet()
    {
        using TestDatabase db = new();
        db.Table<Author>().Schema.CreateTable();
        db.Table<Author>().AddRange(new[]
        {
            new Author { Id = 1, Name = "Ann", Email = "ann@x", BirthDate = new DateTime(2000, 1, 1) },
            new Author { Id = 2, Name = "Bob", Email = "bob@x", BirthDate = new DateTime(2001, 1, 1) },
        });

        List<string[]> expected = new[]
        {
            new { Id = 1, Name = "Ann", Email = "ann@x" },
            new { Id = 2, Name = "Bob", Email = "bob@x" },
        }.OrderBy(x => x.Id).Select(x => new[] { x.Name, x.Email }).ToList();
        List<string[]> actual = db.Table<Author>().OrderBy(x => x.Id).Select(x => new[] { x.Name, x.Email }).ToList();

        Assert.Equal(expected, actual);
    }
}
