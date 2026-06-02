using System.ComponentModel.DataAnnotations;
using SQLite.Framework;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests.BugHunt;

file sealed class HuntNumRow
{
    [Key]
    public int Id { get; set; }

    public List<int> Numbers { get; set; } = [];
}

file sealed class HuntStrRow
{
    [Key]
    public int Id { get; set; }

    public List<string> Tags { get; set; } = [];
}

public class JsonCollectionBugTests
{
    private static TestDatabase CreateNumDb()
    {
        TestDatabase db = new(b =>
            b.TypeConverters[typeof(List<int>)] =
                new SQLiteJsonConverter<List<int>>(TestJsonContext.Default.ListInt32));
        db.Table<HuntNumRow>().Schema.CreateTable();
        return db;
    }

    private static TestDatabase CreateStrDb()
    {
        TestDatabase db = new(b =>
            b.TypeConverters[typeof(List<string>)] =
                new SQLiteJsonConverter<List<string>>(TestJsonContext.Default.ListString));
        db.Table<HuntStrRow>().Schema.CreateTable();
        return db;
    }

    [Fact]
    public void SumOverEmptyArrayWithFirstReturnsZero()
    {
        using TestDatabase db = CreateNumDb();
        db.Table<HuntNumRow>().Add(new HuntNumRow { Id = 1, Numbers = [] });

        int expected = new List<int>().Sum();
        int actual = db.Table<HuntNumRow>().Select(r => r.Numbers.Sum()).First();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void FirstOrDefaultOverEmptyArrayWithFirstReturnsZero()
    {
        using TestDatabase db = CreateNumDb();
        db.Table<HuntNumRow>().Add(new HuntNumRow { Id = 1, Numbers = [] });

        int expected = new List<int>().FirstOrDefault();
        int actual = db.Table<HuntNumRow>().Select(r => r.Numbers.FirstOrDefault()).First();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void TakeNegativeOnJsonCollectionReturnsEmpty()
    {
        using TestDatabase db = CreateNumDb();
        db.Table<HuntNumRow>().Add(new HuntNumRow { Id = 1, Numbers = [1, 2, 3] });

        List<int> expected = new List<int> { 1, 2, 3 }.Where(x => x > 0).Take(-1).ToList();
        List<int> actual = db.Table<HuntNumRow>()
            .Select(r => r.Numbers.Where(x => x > 0).Take(-1).ToList())
            .First();

        Assert.Equal(expected, actual);
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
