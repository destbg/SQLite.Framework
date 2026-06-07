using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Enums;
using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("CharIntRows")]
file sealed class CharIntRow
{
    [Key]
    public int Id { get; set; }

    public char Value { get; set; }

    public char? Nullable { get; set; }
}

public class CharIntegerStorageTests
{
    private static TestDatabase NewDb() => new(b => b.UseCharStorage(CharStorageMode.Integer));

    private static TestDatabase Seed(char[] values)
    {
        TestDatabase db = NewDb();
        db.Table<CharIntRow>().Schema.CreateTable();
        for (int i = 0; i < values.Length; i++)
        {
            db.Table<CharIntRow>().Add(new CharIntRow { Id = i + 1, Value = values[i] });
        }

        return db;
    }

    [Fact]
    public void CharColumnIsIntegerType()
    {
        using TestDatabase db = NewDb();
        db.Table<CharIntRow>().Schema.CreateTable();

        string sql = db.ExecuteScalar<string>(
            "SELECT sql FROM sqlite_master WHERE type = 'table' AND name = 'CharIntRows'")!;

        Assert.Equal("CREATE TABLE \"CharIntRows\" (\"Id\" INTEGER PRIMARY KEY, \"Value\" INTEGER NOT NULL, \"Nullable\" INTEGER NULL)", sql);
    }

    [Theory]
    [InlineData('A')]
    [InlineData('z')]
    [InlineData('5')]
    [InlineData(' ')]
    [InlineData('\0')]
    [InlineData('\uD83D')]
    [InlineData('￿')]
    [InlineData('ñ')]
    public void RoundTripsExactly(char value)
    {
        using TestDatabase db = NewDb();
        db.Table<CharIntRow>().Schema.CreateTable();
        db.Table<CharIntRow>().Add(new CharIntRow { Id = 1, Value = value, Nullable = value });

        CharIntRow read = db.Table<CharIntRow>().Single();

        Assert.Equal(value, read.Value);
        Assert.Equal(value, read.Nullable);
    }

    [Fact]
    public void NullableCharNullRoundTrips()
    {
        using TestDatabase db = NewDb();
        db.Table<CharIntRow>().Schema.CreateTable();
        db.Table<CharIntRow>().Add(new CharIntRow { Id = 1, Value = 'a', Nullable = null });

        Assert.Null(db.Table<CharIntRow>().Single().Nullable);
    }

    [Fact]
    public void EqualityWithCharConstantMatchesInMemory()
    {
        char[] data = ['a', 'B', 'a', 'z'];
        using TestDatabase db = Seed(data);

        List<int> expected = data.Select((c, i) => (c, i)).Where(x => x.c == 'a').Select(x => x.i + 1).ToList();
        List<int> actual = db.Table<CharIntRow>().Where(r => r.Value == 'a').Select(r => r.Id).OrderBy(i => i).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ComparisonWithIntCodePointMatchesInMemory()
    {
        char[] data = ['a', 'b', 'c', 'd'];
        using TestDatabase db = Seed(data);

        List<int> expected = data.Select((c, i) => (c, i)).Where(x => x.c > 'b').Select(x => x.i + 1).ToList();
        List<int> actual = db.Table<CharIntRow>().Where(r => r.Value > 'b').Select(r => r.Id).OrderBy(i => i).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void CastToIntReturnsCodePoint()
    {
        using TestDatabase db = Seed(['A']);

        int code = db.Table<CharIntRow>().Select(r => (int)r.Value).Single();

        Assert.Equal(65, code);
    }

    [Fact]
    public void CastIntToCharReturnsChar()
    {
        using TestDatabase db = Seed(['x']);

        char c = db.Table<CharIntRow>().Select(r => (char)(r.Id + 64)).Single();

        Assert.Equal('A', c);
    }

    [Fact]
    public void DefaultCharLiteralIsCodePoint()
    {
        using ModelTestDatabase db = new(
            model => model.Entity<CharIntRow>().Default(r => r.Value, 'A'),
            options => options.UseCharStorage(CharStorageMode.Integer));
        db.Table<CharIntRow>().Schema.CreateTable();

        string sql = db.ExecuteScalar<string>(
            "SELECT sql FROM sqlite_master WHERE type = 'table' AND name = 'CharIntRows'")!;

        Assert.Equal("CREATE TABLE \"CharIntRows\" (\"Id\" INTEGER PRIMARY KEY, \"Value\" INTEGER NOT NULL DEFAULT 65, \"Nullable\" INTEGER NULL)", sql);
    }

    [Fact]
    public void OrderByCharMatchesInMemory()
    {
        char[] data = ['c', 'a', 'b'];
        using TestDatabase db = Seed(data);

        List<char> expected = data.OrderBy(c => c).ToList();
        List<char> actual = db.Table<CharIntRow>().OrderBy(r => r.Value).Select(r => r.Value).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void IsAsciiDigitMatchesInMemory()
    {
        char[] data = ['7', 'a', '0', 'Z', ' '];
        using TestDatabase db = Seed(data);

        List<int> expected = data.Select((c, i) => (c, i)).Where(x => char.IsAsciiDigit(x.c)).Select(x => x.i + 1).ToList();
        List<int> actual = db.Table<CharIntRow>().Where(r => char.IsAsciiDigit(r.Value)).Select(r => r.Id).OrderBy(i => i).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void IsWhiteSpaceMatchesInMemory()
    {
        char[] data = [' ', 'a', '\t', 'x'];
        using TestDatabase db = Seed(data);

        List<int> expected = data.Select((c, i) => (c, i)).Where(x => char.IsWhiteSpace(x.c)).Select(x => x.i + 1).ToList();
        List<int> actual = db.Table<CharIntRow>().Where(r => char.IsWhiteSpace(r.Value)).Select(r => r.Id).OrderBy(i => i).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void IsAsciiLetterMatchesInMemory()
    {
        char[] data = ['a', 'Z', '5', '!'];
        using TestDatabase db = Seed(data);

        List<int> expected = data.Select((c, i) => (c, i)).Where(x => char.IsAsciiLetter(x.c)).Select(x => x.i + 1).ToList();
        List<int> actual = db.Table<CharIntRow>().Where(r => char.IsAsciiLetter(r.Value)).Select(r => r.Id).OrderBy(i => i).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void IsAsciiLetterOrDigitMatchesInMemory()
    {
        char[] data = ['a', '5', '!', 'Z'];
        using TestDatabase db = Seed(data);

        List<int> expected = data.Select((c, i) => (c, i)).Where(x => char.IsAsciiLetterOrDigit(x.c)).Select(x => x.i + 1).ToList();
        List<int> actual = db.Table<CharIntRow>().Where(r => char.IsAsciiLetterOrDigit(r.Value)).Select(r => r.Id).OrderBy(i => i).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void IsAsciiLetterLowerMatchesInMemory()
    {
        char[] data = ['a', 'A', 'z', 'Z'];
        using TestDatabase db = Seed(data);

        List<int> expected = data.Select((c, i) => (c, i)).Where(x => char.IsAsciiLetterLower(x.c)).Select(x => x.i + 1).ToList();
        List<int> actual = db.Table<CharIntRow>().Where(r => char.IsAsciiLetterLower(r.Value)).Select(r => r.Id).OrderBy(i => i).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void IsAsciiLetterUpperMatchesInMemory()
    {
        char[] data = ['a', 'A', 'z', 'Z'];
        using TestDatabase db = Seed(data);

        List<int> expected = data.Select((c, i) => (c, i)).Where(x => char.IsAsciiLetterUpper(x.c)).Select(x => x.i + 1).ToList();
        List<int> actual = db.Table<CharIntRow>().Where(r => char.IsAsciiLetterUpper(r.Value)).Select(r => r.Id).OrderBy(i => i).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ToLowerMatchesInMemory()
    {
        char[] data = ['A', 'b', 'Z'];
        using TestDatabase db = Seed(data);

        List<char> expected = data.Select(char.ToLower).OrderBy(c => c).ToList();
        List<char> actual = db.Table<CharIntRow>().Select(r => char.ToLower(r.Value)).ToList().OrderBy(c => c).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ToUpperMatchesInMemory()
    {
        char[] data = ['A', 'b', 'z'];
        using TestDatabase db = Seed(data);

        List<char> expected = data.Select(char.ToUpper).OrderBy(c => c).ToList();
        List<char> actual = db.Table<CharIntRow>().Select(r => char.ToUpper(r.Value)).ToList().OrderBy(c => c).ToList();

        Assert.Equal(expected, actual);
    }
}
