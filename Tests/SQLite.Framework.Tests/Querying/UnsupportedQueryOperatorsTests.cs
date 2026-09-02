using System;
using System.Linq;
using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class UnsupportedQueryOperatorsTests
{
    [Fact]
    public void Last_Throws()
    {
        using TestDatabase db = Seed();
        Assert.Throws<NotSupportedException>(() => db.Table<Book>().OrderBy(b => b.Id).Last());
    }

    [Fact]
    public void LastOrDefault_Throws()
    {
        using TestDatabase db = Seed();
        Assert.Throws<NotSupportedException>(() => db.Table<Book>().OrderBy(b => b.Id).Select(b => b.Id).LastOrDefault());
    }

    [Fact]
    public void OrderMatchesObjects()
    {
        using TestDatabase db = Seed();
        List<int> expected = Enumerable.Range(1, 4).Select(i => i % 2).Order().ToList();
        IQueryable<int> query = db.Table<Book>().Select(b => b.AuthorId).Order();

        Assert.Equal(expected, query.ToList());

        SQLiteCommand command = query.ToSqlCommand();
        Assert.Equal("SELECT b0.\"BookAuthorId\" AS \"AuthorId\"\nFROM \"Books\" AS b0\nORDER BY b0.\"BookAuthorId\" ASC", command.CommandText.Replace("\r\n", "\n"));
    }

    [Fact]
    public void OrderDescendingMatchesObjects()
    {
        using TestDatabase db = Seed();
        List<double> expected = Enumerable.Range(1, 4).Select(i => i * 10.0).OrderDescending().ToList();
        IQueryable<double> query = db.Table<Book>().Select(b => b.Price).OrderDescending();

        Assert.Equal(expected, query.ToList());

        SQLiteCommand command = query.ToSqlCommand();
        Assert.Equal("SELECT b0.\"BookPrice\" AS \"Price\"\nFROM \"Books\" AS b0\nORDER BY b0.\"BookPrice\" DESC", command.CommandText.Replace("\r\n", "\n"));
    }

    [Fact]
    public void OrderDescendingWithCustomComparerThrows()
    {
        using TestDatabase db = Seed();

        NotSupportedException exception = Assert.Throws<NotSupportedException>(() => db.Table<Book>()
            .Select(b => b.Title)
            .OrderDescending(StringComparer.OrdinalIgnoreCase)
            .ToList());

        Assert.Contains("IComparer", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void OrderWithCustomComparerThrows()
    {
        using TestDatabase db = Seed();

        NotSupportedException exception = Assert.Throws<NotSupportedException>(() => db.Table<Book>()
            .Select(b => b.Title)
            .Order(StringComparer.OrdinalIgnoreCase)
            .ToList());

        Assert.Contains("IComparer", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void OrderOverEntityThrows()
    {
        using TestDatabase db = Seed();
        Assert.Throws<NotSupportedException>(() => db.Table<Book>().Order().ToList());
    }

    [Fact]
    public void OrderOverClientValueThrows()
    {
        using TestDatabase db = Seed();
        Assert.Throws<NotSupportedException>(() => db.Table<Book>().Select(b => DateTime.IsLeapYear(b.Id)).Order().ToList());
    }

    [Fact]
    public void OrderOverUlongMatchesObjects()
    {
        using TestDatabase db = new();
        db.Table<NumericType>().Schema.CreateTable();
        ulong[] values = [ulong.MaxValue, 0, (ulong)long.MaxValue + 1, 1, (ulong)long.MaxValue];
        for (int i = 0; i < values.Length; i++)
        {
            db.Table<NumericType>().Add(new NumericType { Id = i + 1, ULongValue = values[i] });
        }

        List<ulong> expected = values.Order().ToList();
        IQueryable<ulong> query = db.Table<NumericType>().Select(x => x.ULongValue).Order();

        Assert.Equal(expected, query.ToList());

        SQLiteCommand command = query.ToSqlCommand();
        Assert.Equal("SELECT n0.\"ULongValue\" AS \"ULongValue\"\nFROM \"NumericTypes\" AS n0\nORDER BY (n0.\"ULongValue\") < 0 ASC, n0.\"ULongValue\" ASC", command.CommandText.Replace("\r\n", "\n"));
    }

    [Fact]
    public void OrderOverUlongAfterUnionMatchesObjects()
    {
        using TestDatabase db = new();
        db.Table<NumericType>().Schema.CreateTable();
        NumericType[] rows =
        [
            new NumericType { Id = 1, ULongValue = ulong.MaxValue },
            new NumericType { Id = 2, ULongValue = 0 },
            new NumericType { Id = 3, ULongValue = (ulong)long.MaxValue + 1 },
            new NumericType { Id = 4, ULongValue = 1 },
            new NumericType { Id = 5, ULongValue = (ulong)long.MaxValue }
        ];
        db.Table<NumericType>().AddRange(rows);

        List<ulong> expected = rows.Where(row => row.Id <= 3).Select(row => row.ULongValue)
            .Union(rows.Where(row => row.Id > 3).Select(row => row.ULongValue))
            .Order()
            .ToList();
        List<ulong> actual = db.Table<NumericType>().Where(row => row.Id <= 3).Select(row => row.ULongValue)
            .Union(db.Table<NumericType>().Where(row => row.Id > 3).Select(row => row.ULongValue))
            .Order()
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void OrderOverEnumAfterUnionMatchesObjects()
    {
        using TestDatabase db = Seed();

        List<DayOfWeek> expected = Enumerable.Range(1, 4)
            .Where(id => id <= 2)
            .Select(id => (DayOfWeek)(id % 2))
            .Union(Enumerable.Range(1, 4).Where(id => id > 2).Select(id => (DayOfWeek)(id % 2)))
            .Order()
            .ToList();
        List<DayOfWeek> actual = db.Table<Book>().Where(book => book.Id <= 2)
            .Select(book => (DayOfWeek)book.AuthorId)
            .Union(db.Table<Book>().Where(book => book.Id > 2).Select(book => (DayOfWeek)book.AuthorId))
            .Order()
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void MaxBy_MinBy_Throw()
    {
        using TestDatabase db = Seed();
        Assert.Throws<NotSupportedException>(() => db.Table<Book>().MaxBy(b => b.Price));
        Assert.Throws<NotSupportedException>(() => db.Table<Book>().MinBy(b => b.Price));
    }

    [Fact]
    public void DistinctBy_Throws()
    {
        using TestDatabase db = Seed();
        Assert.Throws<NotSupportedException>(() => db.Table<Book>().DistinctBy(b => b.AuthorId).ToList());
    }

    [Fact]
    public void SkipLast_TakeLast_Throw()
    {
        using TestDatabase db = Seed();
        Assert.Throws<NotSupportedException>(() => db.Table<Book>().OrderBy(b => b.Id).SkipLast(1).ToList());
        Assert.Throws<NotSupportedException>(() => db.Table<Book>().OrderBy(b => b.Id).TakeLast(1).ToList());
    }

    [Fact]
    public void Append_Prepend_Throw()
    {
        using TestDatabase db = Seed();
        Assert.Throws<NotSupportedException>(() => db.Table<Book>().Select(b => b.Id).Append(99).ToList());
        Assert.Throws<NotSupportedException>(() => db.Table<Book>().Select(b => b.Id).Prepend(99).ToList());
    }

    [Fact]
    public void Chunk_Throws()
    {
        using TestDatabase db = Seed();
        Assert.Throws<NotSupportedException>(() => db.Table<Book>().Select(b => b.Id).Chunk(2).ToList());
    }

    [Fact]
    public void SkipWhile_TakeWhile_Throw()
    {
        using TestDatabase db = Seed();
        Assert.Throws<NotSupportedException>(() => db.Table<Book>().OrderBy(b => b.Id).SkipWhile(b => b.Id < 2).ToList());
        Assert.Throws<NotSupportedException>(() => db.Table<Book>().OrderBy(b => b.Id).TakeWhile(b => b.Id < 2).ToList());
    }

    [Fact]
    public void ExceptBy_UnionBy_IntersectBy_Throw()
    {
        using TestDatabase db = Seed();
        int[] keys = [1];
        Assert.Throws<NotSupportedException>(() => db.Table<Book>().ExceptBy(keys, b => b.AuthorId).ToList());
        Assert.Throws<NotSupportedException>(() => db.Table<Book>().UnionBy(db.Table<Book>(), b => b.AuthorId).ToList());
        Assert.Throws<NotSupportedException>(() => db.Table<Book>().IntersectBy(keys, b => b.AuthorId).ToList());
    }

    private static TestDatabase Seed()
    {
        TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        for (int i = 1; i <= 4; i++)
        {
            db.Table<Book>().Add(new Book { Id = i, Title = "t" + i, AuthorId = i % 2, Price = i * 10 });
        }
        return db;
    }
}
