using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using SQLite.Framework;
using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

file sealed class JsonSkipElementAtRow
{
    [Key]
    public int Id { get; set; }

    public List<int> Numbers { get; set; } = [];
}

public class JsonSkipElementAtOffsetTests
{
    private static readonly List<int> Seed = [10, 20, 30, 40, 50, 60, 70, 80, 90];

    private static TestDatabase CreateDb()
    {
        TestDatabase db = new(b =>
            b.TypeConverters[typeof(List<int>)] =
                new SQLiteJsonConverter<List<int>>(TestJsonContext.Default.ListInt32));
        db.Table<JsonSkipElementAtRow>().Schema.CreateTable();
        db.Table<JsonSkipElementAtRow>().Add(new JsonSkipElementAtRow { Id = 1, Numbers = [.. Seed] });
        return db;
    }

    [Fact]
    public void SkipThenElementAtComposesOffsets()
    {
        using TestDatabase db = CreateDb();

        int oracle = Seed.Skip(2).ElementAt(3);
        int actual = db.Table<JsonSkipElementAtRow>()
            .Select(r => r.Numbers.Skip(2).ElementAt(3))
            .First();

        Assert.Equal(oracle, actual);
    }

    [Fact]
    public void SkipThenTakeThenElementAtComposesOffsets()
    {
        using TestDatabase db = CreateDb();

        int oracle = Seed.Skip(1).Take(5).ElementAt(2);
        int actual = db.Table<JsonSkipElementAtRow>()
            .Select(r => r.Numbers.Skip(1).Take(5).ElementAt(2))
            .First();

        Assert.Equal(oracle, actual);
    }
}
