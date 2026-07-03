using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using SQLite.Framework.Enums;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[JsonSerializable(typeof(List<char>))]
internal partial class CharListContext : JsonSerializerContext;

internal sealed class CharListRow
{
    [Key]
    public int Id { get; set; }

    public List<char> Letters { get; set; } = [];
}

public class JsonListCharContainsIntegerStorageTests
{
    private static TestDatabase Seed()
    {
        TestDatabase db = new(b =>
        {
            b.UseCharStorage(CharStorageMode.Integer);
            b.AddJsonContext(CharListContext.Default);
        });
        db.Table<CharListRow>().Schema.CreateTable();
        db.Table<CharListRow>().Add(new CharListRow { Id = 1, Letters = ['a', 'z'] });
        return db;
    }

    [Fact]
    public void ContainsBindsStorageFormAndDoesNotMatch()
    {
        using TestDatabase db = Seed();

        List<char> letters = ['a', 'z'];
        bool inMemory = letters.Contains('a');
        Assert.True(inMemory);

        List<int> actual = db.Table<CharListRow>().Where(r => r.Letters.Contains('a')).Select(r => r.Id).ToList();
        Assert.Equal([], actual);
    }

    [Fact]
    public void IndexOfBindsStorageFormAndDoesNotMatch()
    {
        using TestDatabase db = Seed();

        List<char> letters = ['a', 'z'];
        int inMemory = letters.IndexOf('z');
        Assert.Equal(1, inMemory);

        int actual = db.Table<CharListRow>().Select(r => r.Letters.IndexOf('z')).First();
        Assert.Equal(-1, actual);
    }
}
