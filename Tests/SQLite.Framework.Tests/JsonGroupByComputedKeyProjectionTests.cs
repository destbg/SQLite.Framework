using System.ComponentModel.DataAnnotations;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

internal sealed class JsonModuloGroupRow
{
    [Key]
    public int Id { get; set; }

    public List<int> Numbers { get; set; } = [];
}

public class JsonGroupByComputedKeyProjectionTests
{
    private static readonly List<int> Seed = [7, 14, 3, 22, 3, 9, 14];

    private static TestDatabase SetupDatabase()
    {
        TestDatabase db = new(b => b.TypeConverters[typeof(List<int>)] =
            new SQLiteJsonConverter<List<int>>(TestJsonContext.Default.ListInt32));
        db.Table<JsonModuloGroupRow>().Schema.CreateTable();
        db.Table<JsonModuloGroupRow>().Add(new JsonModuloGroupRow { Id = 1, Numbers = Seed });
        return db;
    }

    [Fact]
    public void KeyProjectionReturnsTheGroupKeys()
    {
        using TestDatabase db = SetupDatabase();

        List<int> expected = Seed.GroupBy(x => x % 5).Select(g => g.Key).OrderBy(k => k).ToList();

        Assert.Equal([2, 3, 4], expected);

        List<int> actual = db.Table<JsonModuloGroupRow>()
            .Select(r => r.Numbers.GroupBy(x => x % 5).Select(g => g.Key))
            .First()
            .OrderBy(k => k)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void KeyArithmeticProjectionUsesTheKeyValue()
    {
        using TestDatabase db = SetupDatabase();

        List<int> expected = Seed.GroupBy(x => x % 5).Select(g => g.Key * 10).OrderBy(k => k).ToList();

        Assert.Equal([20, 30, 40], expected);

        List<int> actual = db.Table<JsonModuloGroupRow>()
            .Select(r => r.Numbers.GroupBy(x => x % 5).Select(g => g.Key * 10))
            .First()
            .OrderBy(k => k)
            .ToList();

        Assert.Equal(expected, actual);
    }
}
