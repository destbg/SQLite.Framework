using System.ComponentModel.DataAnnotations;
using SQLite.Framework.Enums;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

internal sealed class LoneSurrogateCharRow
{
    [Key]
    public int Id { get; set; }

    public char Mark { get; set; }
}

public class CharIntegerStorageLoneSurrogateTests
{
    private static TestDatabase SetupDatabase()
    {
        TestDatabase db = new(b => b.CharStorage = CharStorageMode.Integer);
        db.Table<LoneSurrogateCharRow>().Schema.CreateTable();
        db.Table<LoneSurrogateCharRow>().Add(new LoneSurrogateCharRow { Id = 1, Mark = '\uD800' });
        return db;
    }

    [Fact]
    public void RawColumnReadRoundTripsLoneSurrogate()
    {
        using TestDatabase db = SetupDatabase();

        char actual = db.Table<LoneSurrogateCharRow>().Select(r => r.Mark).First();

        Assert.Equal('\uD800', actual);
    }

    [Fact]
    public void ToStringDoesNotKeepLoneSurrogate()
    {
        using TestDatabase db = SetupDatabase();

        string expected = '\uD800'.ToString();

        Assert.Equal(1, expected.Length);

        string actual = db.Table<LoneSurrogateCharRow>().Select(r => r.Mark.ToString()).First();

        Assert.NotEqual(expected, actual);
    }

    [Fact]
    public void ToUpperDoesNotKeepLoneSurrogate()
    {
        using TestDatabase db = SetupDatabase();

        char expected = char.ToUpper('\uD800');

        Assert.Equal('\uD800', expected);

        char actual = db.Table<LoneSurrogateCharRow>().Select(r => char.ToUpper(r.Mark)).First();

        Assert.NotEqual(expected, actual);
    }
}
