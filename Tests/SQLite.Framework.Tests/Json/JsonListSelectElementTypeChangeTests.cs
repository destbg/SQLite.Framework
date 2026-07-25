using System.ComponentModel.DataAnnotations;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

internal sealed class JsonSelectCastRow
{
    [Key]
    public int Id { get; set; }

    public List<int> Numbers { get; set; } = [];
}

public class JsonListSelectElementTypeChangeTests
{
    [Fact]
    public void SelectChangesElementTypeBeforeToListThrows()
    {
        using TestDatabase db = new(b =>
            b.TypeConverters[typeof(List<int>)] = new SQLiteJsonConverter<List<int>>(TestJsonContext.Default.ListInt32));
        db.Table<JsonSelectCastRow>().Schema.CreateTable();

        List<int> seed = [1, 2, 3];
        db.Table<JsonSelectCastRow>().Add(new JsonSelectCastRow { Id = 1, Numbers = seed });

        Assert.Throws<NotSupportedException>(() =>
            db.Table<JsonSelectCastRow>().Select(r => r.Numbers.Select(x => (long)x).ToList()).First());
    }

    [Fact]
    public void SelectSameElementTypeToListWorks()
    {
        using TestDatabase db = new(b =>
            b.TypeConverters[typeof(List<int>)] = new SQLiteJsonConverter<List<int>>(TestJsonContext.Default.ListInt32));
        db.Table<JsonSelectCastRow>().Schema.CreateTable();

        List<int> seed = [1, 2, 3];
        db.Table<JsonSelectCastRow>().Add(new JsonSelectCastRow { Id = 1, Numbers = seed });

        List<int> expected = seed.Select(x => x * 2).ToList();
        List<int> actual = db.Table<JsonSelectCastRow>().Select(r => r.Numbers.Select(x => x * 2).ToList()).First();

        Assert.Equal([2, 4, 6], expected);
        Assert.Equal(expected, actual);
    }
}
