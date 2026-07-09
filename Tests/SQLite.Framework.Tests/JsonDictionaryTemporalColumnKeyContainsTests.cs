using System;
using System.Collections.Generic;
using System.Linq;
using System.ComponentModel.DataAnnotations;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

internal sealed class TemporalColumnDictRow
{
    [Key]
    public int Id { get; set; }

    public DateTime When { get; set; }

    public Dictionary<DateTime, int> Map { get; set; } = [];
}

public class JsonDictionaryTemporalColumnKeyContainsTests
{
    private static readonly DateTime Key = new(2024, 5, 6, 7, 8, 9);

    private static TestDatabase Seed()
    {
        TestDatabase db = new(b => b.AddJsonContext(TemporalDictKeyContext.Default));
        db.Table<TemporalColumnDictRow>().Schema.CreateTable();
        db.Table<TemporalColumnDictRow>().Add(new TemporalColumnDictRow
        {
            Id = 1,
            When = Key,
            Map = new Dictionary<DateTime, int> { [Key] = 10 }
        });
        return db;
    }

    [Fact]
    public void KeysContainsTemporalColumnDoesNotMatch()
    {
        using TestDatabase db = Seed();

        TemporalColumnDictRow local = new() { When = Key, Map = new Dictionary<DateTime, int> { [Key] = 10 } };
        Assert.Contains(local.When, local.Map.Keys);

        bool actual = db.Table<TemporalColumnDictRow>().Select(r => r.Map.Keys.Contains(r.When)).First();

        Assert.False(actual);
    }

    [Fact]
    public void KeysContainsTemporalConstantMatches()
    {
        using TestDatabase db = Seed();

        bool actual = db.Table<TemporalColumnDictRow>().Select(r => r.Map.Keys.Contains(Key)).First();

        Assert.True(actual);
    }
}
