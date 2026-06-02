using System.ComponentModel.DataAnnotations;
using SQLite.Framework;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests.BugHunt;

file sealed class HuntStrRow
{
    [Key]
    public int Id { get; set; }

    public List<string> Tags { get; set; } = [];
}

public class JsonCollectionBugTests
{
    private static TestDatabase CreateStrDb()
    {
        TestDatabase db = new(b =>
            b.TypeConverters[typeof(List<string>)] =
                new SQLiteJsonConverter<List<string>>(TestJsonContext.Default.ListString));
        db.Table<HuntStrRow>().Schema.CreateTable();
        return db;
    }

    [Fact]
    public void ElementAtPastEndOnJsonCollectionThrows()
    {
        using TestDatabase db = CreateStrDb();
        db.Table<HuntStrRow>().Add(new HuntStrRow { Id = 1, Tags = ["a", "b"] });

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new List<string> { "a", "b" }.ElementAt(5));

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            db.Table<HuntStrRow>().Select(r => r.Tags.ElementAt(5)).First());
    }
}
