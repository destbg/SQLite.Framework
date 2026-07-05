using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using SQLite.Framework.Exceptions;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[JsonSerializable(typeof(List<int>))]
internal partial class WindowRemapListContext : JsonSerializerContext;

internal sealed class WindowRemapListRow
{
    [Key]
    public int Id { get; set; }

    public List<int> Numbers { get; set; } = [];
}

public class JsonListWindowRemapOrderTests
{
    private static TestDatabase Seed(out List<int> local)
    {
        local = [30, 10, 50, 20];
        TestDatabase db = new(b => b.AddJsonContext(WindowRemapListContext.Default));
        db.Table<WindowRemapListRow>().Schema.CreateTable();
        db.Table<WindowRemapListRow>().Add(new WindowRemapListRow { Id = 1, Numbers = [30, 10, 50, 20] });
        return db;
    }

    [Fact]
    public void OrderByTakeSkipReverseMatchesLinqToObjects()
    {
        using TestDatabase db = Seed(out List<int> local);

        List<int> expected = local.OrderBy(x => x).Take(3).Skip(1).Reverse().ToList();
        List<int> actual = db.Table<WindowRemapListRow>().Select(r => r.Numbers.OrderBy(x => x).Take(3).Skip(1).Reverse().ToList()).First();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void OrderByTakeSkipLastMatchesLinqToObjects()
    {
        using TestDatabase db = Seed(out List<int> local);

        int expected = local.OrderBy(x => x).Take(3).Skip(1).Last();
        int actual = db.Table<WindowRemapListRow>().Select(r => r.Numbers.OrderBy(x => x).Take(3).Skip(1).Last()).First();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void OrderBySelectTakeReverseMatchesLinqToObjects()
    {
        using TestDatabase db = Seed(out List<int> local);

        List<int> expected = local.OrderBy(x => x % 3).Select(x => x * 2).Take(2).Reverse().ToList();
        List<int> actual = db.Table<WindowRemapListRow>().Select(r => r.Numbers.OrderBy(x => x % 3).Select(x => x * 2).Take(2).Reverse().ToList()).First();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void FilteredElementAtOrDefaultColumnIndexMatchesLinqToObjects()
    {
        using TestDatabase db = Seed(out List<int> local);

        int expected = local.Where(x => x > 10).ElementAtOrDefault(1);
        int actual = db.Table<WindowRemapListRow>().Select(r => r.Numbers.Where(x => x > 10).ElementAtOrDefault(r.Id)).First();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void FilteredElementAtColumnIndexThrows()
    {
        using TestDatabase db = Seed(out List<int> _);

        Assert.Throws<SQLiteException>(() =>
            db.Table<WindowRemapListRow>().Select(r => r.Numbers.Where(x => x > 10).ElementAt(r.Id)).First());
    }
}
