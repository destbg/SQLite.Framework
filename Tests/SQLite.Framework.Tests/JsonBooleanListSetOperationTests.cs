using System.ComponentModel.DataAnnotations;
using SQLite.Framework;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

internal sealed class JsonBoolSetOpRow
{
    [Key]
    public int Id { get; set; }

    public List<bool> Flags { get; set; } = [];
}

public class JsonBooleanListSetOperationTests
{
    private static readonly List<bool> Seed = [true, false, true];

    private static TestDatabase CreateDb()
    {
        TestDatabase db = new(b => b.TypeConverters[typeof(List<bool>)] =
            new SQLiteJsonConverter<List<bool>>(TestJsonContext.Default.ListBoolean));
        db.Table<JsonBoolSetOpRow>().Schema.CreateTable();
        db.Table<JsonBoolSetOpRow>().Add(new JsonBoolSetOpRow { Id = 1, Flags = Seed });
        return db;
    }

    [Fact]
    public void ConcatBooleanListRoundTrips()
    {
        using TestDatabase db = CreateDb();
        List<bool> other = [true];

        List<bool> expected = Seed.Concat(other).ToList();

        Assert.Equal([true, false, true, true], expected);

        List<bool> actual = db.Table<JsonBoolSetOpRow>()
            .Select(r => r.Flags.Concat(other).ToList())
            .First();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void UnionBooleanListRoundTrips()
    {
        using TestDatabase db = CreateDb();
        List<bool> other = [false];

        List<bool> expected = Seed.Union(other).OrderBy(b => b).ToList();

        Assert.Equal([false, true], expected);

        List<bool> actual = db.Table<JsonBoolSetOpRow>()
            .Select(r => r.Flags.Union(other).ToList())
            .First()
            .OrderBy(b => b)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ExceptBooleanListRoundTrips()
    {
        using TestDatabase db = CreateDb();
        List<bool> other = [false];

        List<bool> expected = Seed.Except(other).ToList();

        Assert.Equal([true], expected);

        List<bool> actual = db.Table<JsonBoolSetOpRow>()
            .Select(r => r.Flags.Except(other).ToList())
            .First();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void GetRangeBooleanListRoundTrips()
    {
        using TestDatabase db = CreateDb();

        List<bool> expected = Seed.GetRange(0, 2);

        Assert.Equal([true, false], expected);

        List<bool> actual = db.Table<JsonBoolSetOpRow>()
            .Select(r => r.Flags.GetRange(0, 2))
            .First();

        Assert.Equal(expected, actual);
    }
}
