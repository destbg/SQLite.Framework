using System.ComponentModel.DataAnnotations;
using SQLite.Framework;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

internal sealed class ExceptTagRow
{
    [Key]
    public int Id { get; set; }

    public List<string> Tags { get; set; } = [];
}

public class JsonExceptNullElementSourceTests
{
    [Fact]
    public void ExceptWithNullFreeOtherDropsSourceNull()
    {
        List<string> tags = ["a", null!, "c"];
        using TestDatabase db = Seed(tags);
        List<string> other = ["b"];

        Assert.Equal(["a", null!, "c"], new List<string> { "a", null!, "c" }.Except(other).ToList());

        List<string> actual = db.Table<ExceptTagRow>().Select(r => r.Tags.Except(other).ToList()).First();

        Assert.Equal(["a", "c"], actual);
    }

    [Fact]
    public void ExceptRemovingOnlyMatchDropsSourceNull()
    {
        List<string> tags = ["a", null!, "c"];
        using TestDatabase db = Seed(tags);
        List<string> other = ["a"];

        Assert.Equal([null!, "c"], new List<string> { "a", null!, "c" }.Except(other).ToList());

        List<string> actual = db.Table<ExceptTagRow>().Select(r => r.Tags.Except(other).ToList()).First();

        Assert.Equal(["c"], actual);
    }

    private static TestDatabase Seed(List<string> tags)
    {
        TestDatabase db = new(b =>
            b.TypeConverters[typeof(List<string>)] =
                new SQLiteJsonConverter<List<string>>(TestJsonContext.Default.ListString));
        db.Table<ExceptTagRow>().Schema.CreateTable();
        db.Table<ExceptTagRow>().Add(new ExceptTagRow { Id = 1, Tags = tags });
        return db;
    }
}
