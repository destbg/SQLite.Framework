using System;
using System.Collections.Generic;
using System.Linq;
using SQLite.Framework;
using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class SemanticsJsonRow
{
    [System.ComponentModel.DataAnnotations.Key]
    public int Id { get; set; }

    public List<int> Numbers { get; set; } = [];
}

public class SqliteSemanticsTests
{
    private static TestDatabase JsonDb(params int[] numbers)
    {
        TestDatabase db = new(b =>
            b.TypeConverters[typeof(List<int>)] =
                new SQLiteJsonConverter<List<int>>(TestJsonContext.Default.ListInt32));
        db.Table<SemanticsJsonRow>().Schema.CreateTable();
        db.Table<SemanticsJsonRow>().Add(new SemanticsJsonRow { Id = 1, Numbers = numbers.ToList() });
        return db;
    }

    [Fact]
    public void JsonSingle_OverManyElements_ReturnsDefault()
    {
        using TestDatabase db = JsonDb(1, 2);

        int actual = db.Table<SemanticsJsonRow>().Select(r => r.Numbers.Single()).First();

        Assert.Equal(default(int), actual);
    }

    [Fact]
    public void JsonFirst_OverEmptyArray_ReturnsDefault()
    {
        using TestDatabase db = JsonDb();

        int actual = db.Table<SemanticsJsonRow>().Select(r => r.Numbers.First()).First();

        Assert.Equal(default(int), actual);
    }

    [Fact]
    public void GroupedMin_OverEmptyFilter_ReturnsDefault()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().Add(new Book { Id = 1, Title = "a", AuthorId = 1, Price = 10 });
        db.Table<Book>().Add(new Book { Id = 2, Title = "b", AuthorId = 1, Price = 20 });
        db.Table<Book>().Add(new Book { Id = 3, Title = "c", AuthorId = 2, Price = 30 });

        List<double> actual = db.Table<Book>()
            .GroupBy(b => b.AuthorId)
            .Select(g => g.Where(x => x.Price > 100000d).Min(x => x.Price))
            .ToList();

        Assert.Equal(new List<double> { default, default }, actual);
    }

    [Fact]
    public void NullableValueAccess_OverNullRow_ReturnsDefault()
    {
        using TestDatabase db = new();
        db.Table<NullableEntity>().Schema.CreateTable();
        int?[] values = [null, 5];
        for (int i = 0; i < values.Length; i++)
        {
            db.Table<NullableEntity>().Add(new NullableEntity { Id = i + 1, Value = values[i] });
        }

        List<int> actual = db.Table<NullableEntity>()
            .OrderBy(x => x.Id)
            .Select(x => x.Value!.Value)
            .ToList();

        Assert.Equal(new List<int> { default, 5 }, actual);
    }

    [Fact]
    public void FloatArithmetic_UsesDoublePrecision()
    {
        using TestDatabase db = new();
        db.Table<NumericType>().Schema.CreateTable();
        float[] values = [0.1f, 0.2f, 0.3f];
        for (int i = 0; i < values.Length; i++)
        {
            db.Table<NumericType>().Add(new NumericType { Id = i + 1, FloatValue = values[i] });
        }

        List<float> expected = values.Select(v => (float)((double)v * (double)v + (double)0.1f)).ToList();
        List<float> actual = db.Table<NumericType>()
            .OrderBy(n => n.Id)
            .Select(n => n.FloatValue * n.FloatValue + 0.1f)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void GroupBy_ReturnsGroupsInKeyOrder()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        (int Id, int AuthorId)[] rows = [(1, 3), (2, 1), (3, 2), (4, 3), (5, 1)];
        foreach ((int id, int authorId) in rows)
        {
            db.Table<Book>().Add(new Book { Id = id, Title = "t" + id, AuthorId = authorId, Price = id });
        }

        List<int> expected = rows.Select(r => r.AuthorId).Distinct().OrderBy(k => k).ToList();
        List<int> actual = db.Table<Book>().GroupBy(b => b.AuthorId).Select(g => g.Key).ToList();

        Assert.Equal(expected, actual);
    }
}
