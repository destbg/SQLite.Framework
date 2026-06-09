using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using SQLite.Framework.Enums;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("StringCharEntities")]
file sealed class StringCharEntity
{
    [Key]
    public int Id { get; set; }

    public string Text { get; set; } = "";

    public char SearchChar { get; set; }

    public char ReplacementChar { get; set; }
}

public class CharStorageColumnArgStringMethodParityTests
{
    private static TestDatabase Seed(string text, char searchChar, char replacementChar)
    {
        TestDatabase db = new(b => b.UseCharStorage(CharStorageMode.Integer));
        db.Table<StringCharEntity>().Schema.CreateTable();
        db.Table<StringCharEntity>().Add(new StringCharEntity
        {
            Id = 1,
            Text = text,
            SearchChar = searchChar,
            ReplacementChar = replacementChar
        });
        return db;
    }

    [Fact]
    public void IndexOf_NonConstantCharColumn_IntegerStorage_MatchesDotNet()
    {
        string text = "hello world";
        char searchChar = 'l';
        using TestDatabase db = Seed(text, searchChar, 'x');

        int expected = text.IndexOf(searchChar);
        int actual = db.Table<StringCharEntity>()
            .Where(e => e.Id == 1)
            .Select(e => e.Text.IndexOf(e.SearchChar))
            .First();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Contains_NonConstantCharColumn_IntegerStorage_MatchesDotNet()
    {
        string text = "hello";
        char searchChar = 'l';
        using TestDatabase db = Seed(text, searchChar, 'x');

        bool expected = text.Contains(searchChar);
        bool actual = db.Table<StringCharEntity>()
            .Where(e => e.Id == 1)
            .Select(e => e.Text.Contains(e.SearchChar))
            .First();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Contains_NonConstantCharColumn_AsciiDigitsInText_IntegerStorage_MatchesDotNet()
    {
        string text = "test122end";
        char searchChar = 'z';
        using TestDatabase db = Seed(text, searchChar, 'x');

        bool expected = text.Contains(searchChar);
        bool actual = db.Table<StringCharEntity>()
            .Where(e => e.Id == 1)
            .Select(e => e.Text.Contains(e.SearchChar))
            .First();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Replace_NonConstantCharColumns_IntegerStorage_MatchesDotNet()
    {
        string text = "hello";
        char searchChar = 'l';
        char replacementChar = 'L';
        using TestDatabase db = Seed(text, searchChar, replacementChar);

        string expected = text.Replace(searchChar, replacementChar);
        string actual = db.Table<StringCharEntity>()
            .Where(e => e.Id == 1)
            .Select(e => e.Text.Replace(e.SearchChar, e.ReplacementChar))
            .First();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Trim_NonConstantCharColumn_IntegerStorage_MatchesDotNet()
    {
        string text = "aaahelloaaa";
        char searchChar = 'a';
        using TestDatabase db = Seed(text, searchChar, 'x');

        string expected = text.Trim(searchChar);
        string actual = db.Table<StringCharEntity>()
            .Where(e => e.Id == 1)
            .Select(e => e.Text.Trim(e.SearchChar))
            .First();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void TrimStart_NonConstantCharColumn_IntegerStorage_MatchesDotNet()
    {
        string text = "xxxhello";
        char searchChar = 'x';
        using TestDatabase db = Seed(text, searchChar, 'x');

        string expected = text.TrimStart(searchChar);
        string actual = db.Table<StringCharEntity>()
            .Where(e => e.Id == 1)
            .Select(e => e.Text.TrimStart(e.SearchChar))
            .First();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void TrimEnd_NonConstantCharColumn_IntegerStorage_MatchesDotNet()
    {
        string text = "helloyyy";
        char searchChar = 'y';
        using TestDatabase db = Seed(text, searchChar, 'x');

        string expected = text.TrimEnd(searchChar);
        string actual = db.Table<StringCharEntity>()
            .Where(e => e.Id == 1)
            .Select(e => e.Text.TrimEnd(e.SearchChar))
            .First();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void PadLeft_NonConstantCharColumn_IntegerStorage_MatchesDotNet()
    {
        string text = "hi";
        char searchChar = '0';
        using TestDatabase db = Seed(text, searchChar, 'x');

        string expected = text.PadLeft(5, searchChar);
        string actual = db.Table<StringCharEntity>()
            .Where(e => e.Id == 1)
            .Select(e => e.Text.PadLeft(5, e.SearchChar))
            .First();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void PadRight_NonConstantCharColumn_IntegerStorage_MatchesDotNet()
    {
        string text = "hi";
        char searchChar = '-';
        using TestDatabase db = Seed(text, searchChar, 'x');

        string expected = text.PadRight(6, searchChar);
        string actual = db.Table<StringCharEntity>()
            .Where(e => e.Id == 1)
            .Select(e => e.Text.PadRight(6, e.SearchChar))
            .First();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void LastIndexOf_NonConstantCharColumn_IntegerStorage_MatchesDotNet()
    {
        string text = "hello world";
        char searchChar = 'l';
        using TestDatabase db = Seed(text, searchChar, 'x');

        int expected = text.LastIndexOf(searchChar);
        int actual = db.Table<StringCharEntity>()
            .Where(e => e.Id == 1)
            .Select(e => e.Text.LastIndexOf(e.SearchChar))
            .First();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void StartsWith_NonConstantCharColumn_IntegerStorage_MatchesDotNet()
    {
        string text = "hello";
        char searchChar = 'h';
        using TestDatabase db = Seed(text, searchChar, 'x');

        bool expected = text.StartsWith(searchChar);
        bool actual = db.Table<StringCharEntity>()
            .Where(e => e.Id == 1)
            .Select(e => e.Text.StartsWith(e.SearchChar))
            .First();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void EndsWith_NonConstantCharColumn_IntegerStorage_MatchesDotNet()
    {
        string text = "hello";
        char searchChar = 'o';
        using TestDatabase db = Seed(text, searchChar, 'x');

        bool expected = text.EndsWith(searchChar);
        bool actual = db.Table<StringCharEntity>()
            .Where(e => e.Id == 1)
            .Select(e => e.Text.EndsWith(e.SearchChar))
            .First();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void IndexOfWithStartIndex_NonConstantCharColumn_IntegerStorage_MatchesDotNet()
    {
        string text = "hello world";
        char searchChar = 'l';
        using TestDatabase db = Seed(text, searchChar, 'x');

        int expected = text.IndexOf(searchChar, 4);
        int actual = db.Table<StringCharEntity>()
            .Where(e => e.Id == 1)
            .Select(e => e.Text.IndexOf(e.SearchChar, 4))
            .First();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void LastIndexOfWithStartIndex_NonConstantCharColumn_IntegerStorage_MatchesDotNet()
    {
        string text = "hello world";
        char searchChar = 'l';
        using TestDatabase db = Seed(text, searchChar, 'x');

        int expected = text.LastIndexOf(searchChar, 4);
        int actual = db.Table<StringCharEntity>()
            .Where(e => e.Id == 1)
            .Select(e => e.Text.LastIndexOf(e.SearchChar, 4))
            .First();

        Assert.Equal(expected, actual);
    }
}
