using System.Runtime.CompilerServices;
using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class CharMethodTests
{
    [Fact]
    public void CharToLower()
    {
        using TestDatabase db = SetupDatabase('A', 'Z', 'b');

        IQueryable<NumericType> query =
            from n in db.Table<NumericType>()
            where char.ToLower(n.CharValue) == 'a'
            select n;

        SQLiteCommand command = query.ToSqlCommand();

        Assert.Single(command.Parameters);
        Assert.Equal(command.Parameters[0].Value, "a");
        Assert.Equal("SELECT n0.\"Id\" AS \"Id\",\n       n0.\"IntValue\" AS \"IntValue\",\n       n0.\"LongValue\" AS \"LongValue\",\n       n0.\"ShortValue\" AS \"ShortValue\",\n       n0.\"ByteValue\" AS \"ByteValue\",\n       n0.\"SByteValue\" AS \"SByteValue\",\n       n0.\"UIntValue\" AS \"UIntValue\",\n       n0.\"ULongValue\" AS \"ULongValue\",\n       n0.\"UShortValue\" AS \"UShortValue\",\n       n0.\"DoubleValue\" AS \"DoubleValue\",\n       n0.\"FloatValue\" AS \"FloatValue\",\n       n0.\"DecimalValue\" AS \"DecimalValue\",\n       n0.\"CharValue\" AS \"CharValue\",\n       n0.\"BlobValue\" AS \"BlobValue\"\nFROM \"NumericTypes\" AS n0\nWHERE LOWER(n0.\"CharValue\") = @p0", command.CommandText.Replace("\r\n", "\n"));

        List<NumericType> results = query.ToList();
        Assert.Single(results);
        Assert.Equal('A', results[0].CharValue);
    }

    [Fact]
    public void CharToUpper()
    {
        using TestDatabase db = SetupDatabase('a', 'z', 'B');

        IQueryable<NumericType> query =
            from n in db.Table<NumericType>()
            where char.ToUpper(n.CharValue) == 'Z'
            select n;

        SQLiteCommand command = query.ToSqlCommand();

        Assert.Single(command.Parameters);
        Assert.Equal(command.Parameters[0].Value, "Z");
        Assert.Equal("SELECT n0.\"Id\" AS \"Id\",\n       n0.\"IntValue\" AS \"IntValue\",\n       n0.\"LongValue\" AS \"LongValue\",\n       n0.\"ShortValue\" AS \"ShortValue\",\n       n0.\"ByteValue\" AS \"ByteValue\",\n       n0.\"SByteValue\" AS \"SByteValue\",\n       n0.\"UIntValue\" AS \"UIntValue\",\n       n0.\"ULongValue\" AS \"ULongValue\",\n       n0.\"UShortValue\" AS \"UShortValue\",\n       n0.\"DoubleValue\" AS \"DoubleValue\",\n       n0.\"FloatValue\" AS \"FloatValue\",\n       n0.\"DecimalValue\" AS \"DecimalValue\",\n       n0.\"CharValue\" AS \"CharValue\",\n       n0.\"BlobValue\" AS \"BlobValue\"\nFROM \"NumericTypes\" AS n0\nWHERE UPPER(n0.\"CharValue\") = @p0", command.CommandText.Replace("\r\n", "\n"));

        List<NumericType> results = query.ToList();
        Assert.Single(results);
        Assert.Equal('z', results[0].CharValue);
    }

    [Fact]
    public void CharIsWhiteSpace()
    {
        using TestDatabase db = SetupDatabase(' ', 'A', '9');

        IQueryable<NumericType> query =
            from n in db.Table<NumericType>()
            where char.IsWhiteSpace(n.CharValue)
            select n;

        SQLiteCommand command = query.ToSqlCommand();

        Assert.Empty(command.Parameters);
        Assert.Equal("SELECT n0.\"Id\" AS \"Id\",\n       n0.\"IntValue\" AS \"IntValue\",\n       n0.\"LongValue\" AS \"LongValue\",\n       n0.\"ShortValue\" AS \"ShortValue\",\n       n0.\"ByteValue\" AS \"ByteValue\",\n       n0.\"SByteValue\" AS \"SByteValue\",\n       n0.\"UIntValue\" AS \"UIntValue\",\n       n0.\"ULongValue\" AS \"ULongValue\",\n       n0.\"UShortValue\" AS \"UShortValue\",\n       n0.\"DoubleValue\" AS \"DoubleValue\",\n       n0.\"FloatValue\" AS \"FloatValue\",\n       n0.\"DecimalValue\" AS \"DecimalValue\",\n       n0.\"CharValue\" AS \"CharValue\",\n       n0.\"BlobValue\" AS \"BlobValue\"\nFROM \"NumericTypes\" AS n0\nWHERE TRIM(n0.\"CharValue\", CHAR(9, 10, 11, 12, 13, 32, 133, 160, 5760, 8192, 8193, 8194, 8195, 8196, 8197, 8198, 8199, 8200, 8201, 8202, 8232, 8233, 8239, 8287, 12288)) = ''", command.CommandText.Replace("\r\n", "\n"));

        List<NumericType> results = query.ToList();
        Assert.Single(results);
        Assert.Equal(' ', results[0].CharValue);
    }

    [Fact]
    public void CharIsAsciiDigit()
    {
        using TestDatabase db = SetupDatabase('5', 'A', '9');

        IQueryable<NumericType> query =
            from n in db.Table<NumericType>()
            where char.IsAsciiDigit(n.CharValue)
            select n;

        SQLiteCommand command = query.ToSqlCommand();

        Assert.Empty(command.Parameters);
        Assert.Equal("SELECT n0.\"Id\" AS \"Id\",\n       n0.\"IntValue\" AS \"IntValue\",\n       n0.\"LongValue\" AS \"LongValue\",\n       n0.\"ShortValue\" AS \"ShortValue\",\n       n0.\"ByteValue\" AS \"ByteValue\",\n       n0.\"SByteValue\" AS \"SByteValue\",\n       n0.\"UIntValue\" AS \"UIntValue\",\n       n0.\"ULongValue\" AS \"ULongValue\",\n       n0.\"UShortValue\" AS \"UShortValue\",\n       n0.\"DoubleValue\" AS \"DoubleValue\",\n       n0.\"FloatValue\" AS \"FloatValue\",\n       n0.\"DecimalValue\" AS \"DecimalValue\",\n       n0.\"CharValue\" AS \"CharValue\",\n       n0.\"BlobValue\" AS \"BlobValue\"\nFROM \"NumericTypes\" AS n0\nWHERE (n0.\"CharValue\" BETWEEN '0' AND '9')", command.CommandText.Replace("\r\n", "\n"));

        List<NumericType> results = query.ToList();
        Assert.Equal(2, results.Count);
        Assert.Contains(results, r => r.CharValue == '5');
        Assert.Contains(results, r => r.CharValue == '9');
    }

    [Fact]
    public void CharIsAsciiLetter()
    {
        using TestDatabase db = SetupDatabase('A', '5', 'z');

        IQueryable<NumericType> query =
            from n in db.Table<NumericType>()
            where char.IsAsciiLetter(n.CharValue)
            select n;

        SQLiteCommand command = query.ToSqlCommand();

        Assert.Empty(command.Parameters);
        Assert.Equal("SELECT n0.\"Id\" AS \"Id\",\n       n0.\"IntValue\" AS \"IntValue\",\n       n0.\"LongValue\" AS \"LongValue\",\n       n0.\"ShortValue\" AS \"ShortValue\",\n       n0.\"ByteValue\" AS \"ByteValue\",\n       n0.\"SByteValue\" AS \"SByteValue\",\n       n0.\"UIntValue\" AS \"UIntValue\",\n       n0.\"ULongValue\" AS \"ULongValue\",\n       n0.\"UShortValue\" AS \"UShortValue\",\n       n0.\"DoubleValue\" AS \"DoubleValue\",\n       n0.\"FloatValue\" AS \"FloatValue\",\n       n0.\"DecimalValue\" AS \"DecimalValue\",\n       n0.\"CharValue\" AS \"CharValue\",\n       n0.\"BlobValue\" AS \"BlobValue\"\nFROM \"NumericTypes\" AS n0\nWHERE (LOWER(n0.\"CharValue\") BETWEEN 'a' AND 'z')", command.CommandText.Replace("\r\n", "\n"));

        List<NumericType> results = query.ToList();
        Assert.Equal(2, results.Count);
        Assert.Contains(results, r => r.CharValue == 'A');
        Assert.Contains(results, r => r.CharValue == 'z');
    }

    [Fact]
    public void CharIsAsciiLetterOrDigit()
    {
        using TestDatabase db = SetupDatabase('A', '5', '!');

        IQueryable<NumericType> query =
            from n in db.Table<NumericType>()
            where char.IsAsciiLetterOrDigit(n.CharValue)
            select n;

        SQLiteCommand command = query.ToSqlCommand();

        Assert.Empty(command.Parameters);
        Assert.Equal("SELECT n0.\"Id\" AS \"Id\",\n       n0.\"IntValue\" AS \"IntValue\",\n       n0.\"LongValue\" AS \"LongValue\",\n       n0.\"ShortValue\" AS \"ShortValue\",\n       n0.\"ByteValue\" AS \"ByteValue\",\n       n0.\"SByteValue\" AS \"SByteValue\",\n       n0.\"UIntValue\" AS \"UIntValue\",\n       n0.\"ULongValue\" AS \"ULongValue\",\n       n0.\"UShortValue\" AS \"UShortValue\",\n       n0.\"DoubleValue\" AS \"DoubleValue\",\n       n0.\"FloatValue\" AS \"FloatValue\",\n       n0.\"DecimalValue\" AS \"DecimalValue\",\n       n0.\"CharValue\" AS \"CharValue\",\n       n0.\"BlobValue\" AS \"BlobValue\"\nFROM \"NumericTypes\" AS n0\nWHERE ((n0.\"CharValue\" BETWEEN '0' AND '9') OR (LOWER(n0.\"CharValue\") BETWEEN 'a' AND 'z'))", command.CommandText.Replace("\r\n", "\n"));

        List<NumericType> results = query.ToList();
        Assert.Equal(2, results.Count);
        Assert.Contains(results, r => r.CharValue == 'A');
        Assert.Contains(results, r => r.CharValue == '5');
    }

    [Fact]
    public void CharIsAsciiLetterLower()
    {
        using TestDatabase db = SetupDatabase('a', 'A', 'z');

        IQueryable<NumericType> query =
            from n in db.Table<NumericType>()
            where char.IsAsciiLetterLower(n.CharValue)
            select n;

        SQLiteCommand command = query.ToSqlCommand();

        Assert.Empty(command.Parameters);
        Assert.Equal("SELECT n0.\"Id\" AS \"Id\",\n       n0.\"IntValue\" AS \"IntValue\",\n       n0.\"LongValue\" AS \"LongValue\",\n       n0.\"ShortValue\" AS \"ShortValue\",\n       n0.\"ByteValue\" AS \"ByteValue\",\n       n0.\"SByteValue\" AS \"SByteValue\",\n       n0.\"UIntValue\" AS \"UIntValue\",\n       n0.\"ULongValue\" AS \"ULongValue\",\n       n0.\"UShortValue\" AS \"UShortValue\",\n       n0.\"DoubleValue\" AS \"DoubleValue\",\n       n0.\"FloatValue\" AS \"FloatValue\",\n       n0.\"DecimalValue\" AS \"DecimalValue\",\n       n0.\"CharValue\" AS \"CharValue\",\n       n0.\"BlobValue\" AS \"BlobValue\"\nFROM \"NumericTypes\" AS n0\nWHERE (n0.\"CharValue\" BETWEEN 'a' AND 'z')", command.CommandText.Replace("\r\n", "\n"));

        List<NumericType> results = query.ToList();
        Assert.Equal(2, results.Count);
        Assert.Contains(results, r => r.CharValue == 'a');
        Assert.Contains(results, r => r.CharValue == 'z');
    }

    [Fact]
    public void CharIsAsciiLetterUpper()
    {
        using TestDatabase db = SetupDatabase('A', 'Z', 'b');

        IQueryable<NumericType> query =
            from n in db.Table<NumericType>()
            where char.IsAsciiLetterUpper(n.CharValue)
            select n;

        SQLiteCommand command = query.ToSqlCommand();

        Assert.Empty(command.Parameters);
        Assert.Equal("SELECT n0.\"Id\" AS \"Id\",\n       n0.\"IntValue\" AS \"IntValue\",\n       n0.\"LongValue\" AS \"LongValue\",\n       n0.\"ShortValue\" AS \"ShortValue\",\n       n0.\"ByteValue\" AS \"ByteValue\",\n       n0.\"SByteValue\" AS \"SByteValue\",\n       n0.\"UIntValue\" AS \"UIntValue\",\n       n0.\"ULongValue\" AS \"ULongValue\",\n       n0.\"UShortValue\" AS \"UShortValue\",\n       n0.\"DoubleValue\" AS \"DoubleValue\",\n       n0.\"FloatValue\" AS \"FloatValue\",\n       n0.\"DecimalValue\" AS \"DecimalValue\",\n       n0.\"CharValue\" AS \"CharValue\",\n       n0.\"BlobValue\" AS \"BlobValue\"\nFROM \"NumericTypes\" AS n0\nWHERE (n0.\"CharValue\" BETWEEN 'A' AND 'Z')", command.CommandText.Replace("\r\n", "\n"));

        List<NumericType> results = query.ToList();
        Assert.Equal(2, results.Count);
        Assert.Contains(results, r => r.CharValue == 'A');
        Assert.Contains(results, r => r.CharValue == 'Z');
    }

    private static TestDatabase SetupDatabase(char one, char two, char three, [CallerMemberName] string? methodName = null)
    {
        TestDatabase db = new(methodName);

        db.Table<NumericType>().Schema.CreateTable();
        db.Table<NumericType>().AddRange(new[]
        {
            new NumericType
            {
                Id = 1,
                IntValue = 1,
                LongValue = 1,
                ShortValue = 1,
                ByteValue = 1,
                SByteValue = 1,
                UIntValue = 1,
                ULongValue = 1,
                UShortValue = 1,
                DoubleValue = 1,
                FloatValue = 1,
                DecimalValue = 1,
                CharValue = one
            },
            new NumericType
            {
                Id = 2,
                IntValue = 2,
                LongValue = 2,
                ShortValue = 2,
                ByteValue = 2,
                SByteValue = 2,
                UIntValue = 2,
                ULongValue = 2,
                UShortValue = 2,
                DoubleValue = 2,
                FloatValue = 2,
                DecimalValue = 2,
                CharValue = two
            },
            new NumericType
            {
                Id = 3,
                IntValue = 3,
                LongValue = 3,
                ShortValue = 3,
                ByteValue = 3,
                SByteValue = 3,
                UIntValue = 3,
                ULongValue = 3,
                UShortValue = 3,
                DoubleValue = 3,
                FloatValue = 3,
                DecimalValue = 3,
                CharValue = three
            }
        });

        return db;
    }

    [Fact]
    public void CastCharToInt_Select_ProducesUnicode()
    {
        using TestDatabase db = SetupDatabase('A', 'B', 'C');

        SQLiteCommand command = db.Table<NumericType>()
            .Select(n => (int)n.CharValue)
            .ToSqlCommand();

        Assert.Equal("SELECT UNICODE(n0.\"CharValue\") AS \"15\"\nFROM \"NumericTypes\" AS n0", command.CommandText.Replace("\r\n", "\n"));
    }

    [Fact]
    public void CastCharToInt_Select_ReturnsCharCode()
    {
        using TestDatabase db = SetupDatabase('A', 'B', 'C');

        int result = db.Table<NumericType>()
            .Where(n => n.Id == 1)
            .Select(n => (int)n.CharValue)
            .First();

        Assert.Equal(65, result);
    }

    [Fact]
    public void CastIntToChar_Select_ProducesChar()
    {
        using TestDatabase db = SetupDatabase('A', 'B', 'C');

        SQLiteCommand command = db.Table<NumericType>()
            .Select(n => (char)n.IntValue)
            .ToSqlCommand();

        Assert.Equal("SELECT CHAR((n0.\"IntValue\") & 65535) AS \"15\"\nFROM \"NumericTypes\" AS n0", command.CommandText.Replace("\r\n", "\n"));
    }

    [Fact]
    public void CastIntToChar_Select_ReturnsChar()
    {
        using TestDatabase db = SetupDatabase('A', 'B', 'C');

        char result = db.Table<NumericType>()
            .Where(n => n.Id == 1)
            .Select(n => (char)n.IntValue)
            .First();

        Assert.Equal((char)1, result);
    }
}
