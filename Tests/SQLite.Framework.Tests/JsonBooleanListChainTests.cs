using System.ComponentModel.DataAnnotations;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

internal sealed class JsonFlagsRow
{
    [Key]
    public int Id { get; set; }

    public List<bool> Flags { get; set; } = [];
}

public class JsonBooleanListChainTests
{
    private static readonly List<bool> Seed = [true, false, true];

    private static TestDatabase SetupDatabase()
    {
        TestDatabase db = new(b => b.TypeConverters[typeof(List<bool>)] =
            new SQLiteJsonConverter<List<bool>>(TestJsonContext.Default.ListBoolean));
        db.Table<JsonFlagsRow>().Schema.CreateTable();
        db.Table<JsonFlagsRow>().Add(new JsonFlagsRow { Id = 1, Flags = Seed });
        return db;
    }

    [Fact]
    public void WhereOverBooleanListReturnsFilteredList()
    {
        using TestDatabase db = SetupDatabase();

        List<bool> expected = Seed.Where(b => b).ToList();

        Assert.Equal([true, true], expected);

        List<bool> actual = db.Table<JsonFlagsRow>()
            .Select(r => r.Flags.Where(b => b).ToList())
            .First();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void SkipOverBooleanListReturnsRemainingElements()
    {
        using TestDatabase db = SetupDatabase();

        List<bool> expected = Seed.Skip(1).ToList();

        Assert.Equal([false, true], expected);

        List<bool> actual = db.Table<JsonFlagsRow>()
            .Select(r => r.Flags.Skip(1).ToList())
            .First();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void DistinctOverBooleanListReturnsDistinctValues()
    {
        using TestDatabase db = SetupDatabase();

        List<bool> expected = Seed.Distinct().ToList();

        Assert.Equal([true, false], expected);

        List<bool> actual = db.Table<JsonFlagsRow>()
            .Select(r => r.Flags.Distinct().ToList())
            .First();

        Assert.Equal(expected, actual);
    }
}
