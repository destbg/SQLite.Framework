using System.ComponentModel.DataAnnotations;
using SQLite.Framework;
using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

file sealed class TagListRow
{
    [Key]
    public int Id { get; set; }

    public List<string> Tags { get; set; } = [];
}

public class JsonCollectionProjectionWindowTests
{
    private static TestDatabase CreateDb()
    {
        TestDatabase db = new(b =>
            b.TypeConverters[typeof(List<string>)] =
                new SQLiteJsonConverter<List<string>>(TestJsonContext.Default.ListString));
        db.Table<TagListRow>().Schema.CreateTable();
        return db;
    }

    [Fact]
    public void SelectAfterTakeMatchesDotNet()
    {
        using TestDatabase db = CreateDb();
        List<string> seed = ["ab", "cd", "ef", "gh"];
        db.Table<TagListRow>().Add(new TagListRow { Id = 1, Tags = [.. seed] });

        List<string> oracle = seed.Take(3).Select(x => x.ToUpper()).ToList();
        List<string> actual = db.Table<TagListRow>()
            .Select(r => r.Tags.Take(3).Select(x => x.ToUpper()).ToList())
            .First();

        Assert.Equal(["AB", "CD", "EF"], oracle);
        Assert.Equal(oracle, actual);
    }

    [Fact]
    public void SelectAfterOrderByMatchesDotNet()
    {
        using TestDatabase db = CreateDb();
        List<string> seed = ["ef", "ab", "gh", "cd"];
        db.Table<TagListRow>().Add(new TagListRow { Id = 1, Tags = [.. seed] });

        List<string> oracle = seed.OrderBy(x => x).Select(x => x + "!").ToList();
        List<string> actual = db.Table<TagListRow>()
            .Select(r => r.Tags.OrderBy(x => x).Select(x => x + "!").ToList())
            .First();

        Assert.Equal(["ab!", "cd!", "ef!", "gh!"], oracle);
        Assert.Equal(oracle, actual);
    }
}
