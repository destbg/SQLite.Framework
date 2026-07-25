using System;
using System.Linq;
using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class UnsupportedQueryOperatorsTests
{
    private static TestDatabase Seed()
    {
        TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        for (int i = 1; i <= 4; i++)
            db.Table<Book>().Add(new Book { Id = i, Title = "t" + i, AuthorId = i % 2, Price = i * 10 });
        return db;
    }

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
    public void Order_Throws()
    {
        using TestDatabase db = Seed();
        Assert.Throws<NotSupportedException>(() => db.Table<Book>().Select(b => b.Price).Order().ToList());
    }

    [Fact]
    public void OrderDescending_Throws()
    {
        using TestDatabase db = Seed();
        Assert.Throws<NotSupportedException>(() => db.Table<Book>().Select(b => b.Price).OrderDescending().ToList());
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
}
