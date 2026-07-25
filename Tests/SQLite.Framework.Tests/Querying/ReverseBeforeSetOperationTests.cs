using System;
using System.Collections.Generic;
using System.Linq;
using SQLite.Framework;
using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

file sealed class ReverseSetOpRow
{
    [System.ComponentModel.DataAnnotations.Key]
    public int Id { get; set; }
}

public class ReverseBeforeSetOperationTests
{
    private static readonly int[] Ids = [1, 2, 3, 4, 5];

    private static TestDatabase CreateDb()
    {
        TestDatabase db = new();
        db.Table<ReverseSetOpRow>().Schema.CreateTable();
        db.Table<ReverseSetOpRow>().AddRange(Ids.Select(id => new ReverseSetOpRow { Id = id }).ToList());
        return db;
    }

    [Fact]
    public void ReverseBeforeConcatThrowsClearError()
    {
        using TestDatabase db = CreateDb();
        Assert.Throws<NotSupportedException>(() =>
            db.Table<ReverseSetOpRow>().Select(x => x.Id).Where(x => x <= 3).Reverse()
                .Concat(db.Table<ReverseSetOpRow>().Select(x => x.Id).Where(x => x >= 4))
                .ToList());
    }

    [Fact]
    public void ReverseBeforeUnionThrowsClearError()
    {
        using TestDatabase db = CreateDb();
        Assert.Throws<NotSupportedException>(() =>
            db.Table<ReverseSetOpRow>().Select(x => x.Id).Where(x => x <= 3).Reverse()
                .Union(db.Table<ReverseSetOpRow>().Select(x => x.Id).Where(x => x >= 3))
                .ToList());
    }

    [Fact]
    public void ReverseBeforeIntersectThrowsClearError()
    {
        using TestDatabase db = CreateDb();
        Assert.Throws<NotSupportedException>(() =>
            db.Table<ReverseSetOpRow>().Select(x => x.Id).Where(x => x <= 4).Reverse()
                .Intersect(db.Table<ReverseSetOpRow>().Select(x => x.Id).Where(x => x >= 2))
                .ToList());
    }

    [Fact]
    public void ReverseBeforeExceptThrowsClearError()
    {
        using TestDatabase db = CreateDb();
        Assert.Throws<NotSupportedException>(() =>
            db.Table<ReverseSetOpRow>().Select(x => x.Id).Where(x => x <= 4).Reverse()
                .Except(db.Table<ReverseSetOpRow>().Select(x => x.Id).Where(x => x >= 4))
                .ToList());
    }

    [Fact]
    public void ConcatWithoutReverseStillWorks()
    {
        using TestDatabase db = CreateDb();

        List<int> oracle = Ids.Where(x => x <= 3)
            .Concat(Ids.Where(x => x >= 4))
            .OrderBy(x => x)
            .ToList();
        List<int> actual = db.Table<ReverseSetOpRow>().Select(x => x.Id).Where(x => x <= 3)
            .Concat(db.Table<ReverseSetOpRow>().Select(x => x.Id).Where(x => x >= 4))
            .OrderBy(x => x)
            .ToList();

        Assert.Equal(oracle, actual);
    }
}
