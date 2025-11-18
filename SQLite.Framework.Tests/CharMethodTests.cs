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
        Assert.Equal("""
                     SELECT n0.Id AS "Id",
                            n0.IntValue AS "IntValue",
                            n0.LongValue AS "LongValue",
                            n0.ShortValue AS "ShortValue",
                            n0.ByteValue AS "ByteValue",
                            n0.SByteValue AS "SByteValue",
                            n0.UIntValue AS "UIntValue",
                            n0.ULongValue AS "ULongValue",
                            n0.UShortValue AS "UShortValue",
                            n0.DoubleValue AS "DoubleValue",
                            n0.FloatValue AS "FloatValue",
                            n0.DecimalValue AS "DecimalValue",
                            n0.CharValue AS "CharValue",
                            n0.BlobValue AS "BlobValue"
                     FROM "NumericTypes" AS n0
                     WHERE LOWER(n0.CharValue) = @p0
                     """.Replace("\r\n", "\n"),
            command.CommandText.Replace("\r\n", "\n"));

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
        Assert.Equal("""
                     SELECT n0.Id AS "Id",
                            n0.IntValue AS "IntValue",
                            n0.LongValue AS "LongValue",
                            n0.ShortValue AS "ShortValue",
                            n0.ByteValue AS "ByteValue",
                            n0.SByteValue AS "SByteValue",
                            n0.UIntValue AS "UIntValue",
                            n0.ULongValue AS "ULongValue",
                            n0.UShortValue AS "UShortValue",
                            n0.DoubleValue AS "DoubleValue",
                            n0.FloatValue AS "FloatValue",
                            n0.DecimalValue AS "DecimalValue",
                            n0.CharValue AS "CharValue",
                            n0.BlobValue AS "BlobValue"
                     FROM "NumericTypes" AS n0
                     WHERE UPPER(n0.CharValue) = @p0
                     """.Replace("\r\n", "\n"),
            command.CommandText.Replace("\r\n", "\n"));

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
        Assert.Equal("""
                     SELECT n0.Id AS "Id",
                            n0.IntValue AS "IntValue",
                            n0.LongValue AS "LongValue",
                            n0.ShortValue AS "ShortValue",
                            n0.ByteValue AS "ByteValue",
                            n0.SByteValue AS "SByteValue",
                            n0.UIntValue AS "UIntValue",
                            n0.ULongValue AS "ULongValue",
                            n0.UShortValue AS "UShortValue",
                            n0.DoubleValue AS "DoubleValue",
                            n0.FloatValue AS "FloatValue",
                            n0.DecimalValue AS "DecimalValue",
                            n0.CharValue AS "CharValue",
                            n0.BlobValue AS "BlobValue"
                     FROM "NumericTypes" AS n0
                     WHERE TRIM(n0.CharValue) = ''
                     """.Replace("\r\n", "\n"),
            command.CommandText.Replace("\r\n", "\n"));

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
        Assert.Equal("""
                     SELECT n0.Id AS "Id",
                            n0.IntValue AS "IntValue",
                            n0.LongValue AS "LongValue",
                            n0.ShortValue AS "ShortValue",
                            n0.ByteValue AS "ByteValue",
                            n0.SByteValue AS "SByteValue",
                            n0.UIntValue AS "UIntValue",
                            n0.ULongValue AS "ULongValue",
                            n0.UShortValue AS "UShortValue",
                            n0.DoubleValue AS "DoubleValue",
                            n0.FloatValue AS "FloatValue",
                            n0.DecimalValue AS "DecimalValue",
                            n0.CharValue AS "CharValue",
                            n0.BlobValue AS "BlobValue"
                     FROM "NumericTypes" AS n0
                     WHERE (n0.CharValue >= '0' AND n0.CharValue <= '9')
                     """.Replace("\r\n", "\n"),
            command.CommandText.Replace("\r\n", "\n"));

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
        Assert.Equal("""
                     SELECT n0.Id AS "Id",
                            n0.IntValue AS "IntValue",
                            n0.LongValue AS "LongValue",
                            n0.ShortValue AS "ShortValue",
                            n0.ByteValue AS "ByteValue",
                            n0.SByteValue AS "SByteValue",
                            n0.UIntValue AS "UIntValue",
                            n0.ULongValue AS "ULongValue",
                            n0.UShortValue AS "UShortValue",
                            n0.DoubleValue AS "DoubleValue",
                            n0.FloatValue AS "FloatValue",
                            n0.DecimalValue AS "DecimalValue",
                            n0.CharValue AS "CharValue",
                            n0.BlobValue AS "BlobValue"
                     FROM "NumericTypes" AS n0
                     WHERE ((n0.CharValue >= 'a' AND n0.CharValue <= 'z') OR (n0.CharValue >= 'A' AND n0.CharValue <= 'Z'))
                     """.Replace("\r\n", "\n"),
            command.CommandText.Replace("\r\n", "\n"));

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
        Assert.Equal("""
                     SELECT n0.Id AS "Id",
                            n0.IntValue AS "IntValue",
                            n0.LongValue AS "LongValue",
                            n0.ShortValue AS "ShortValue",
                            n0.ByteValue AS "ByteValue",
                            n0.SByteValue AS "SByteValue",
                            n0.UIntValue AS "UIntValue",
                            n0.ULongValue AS "ULongValue",
                            n0.UShortValue AS "UShortValue",
                            n0.DoubleValue AS "DoubleValue",
                            n0.FloatValue AS "FloatValue",
                            n0.DecimalValue AS "DecimalValue",
                            n0.CharValue AS "CharValue",
                            n0.BlobValue AS "BlobValue"
                     FROM "NumericTypes" AS n0
                     WHERE ((n0.CharValue >= '0' AND n0.CharValue <= '9') OR (n0.CharValue >= 'a' AND n0.CharValue <= 'z') OR (n0.CharValue >= 'A' AND n0.CharValue <= 'Z'))
                     """.Replace("\r\n", "\n"),
            command.CommandText.Replace("\r\n", "\n"));

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
        Assert.Equal("""
                     SELECT n0.Id AS "Id",
                            n0.IntValue AS "IntValue",
                            n0.LongValue AS "LongValue",
                            n0.ShortValue AS "ShortValue",
                            n0.ByteValue AS "ByteValue",
                            n0.SByteValue AS "SByteValue",
                            n0.UIntValue AS "UIntValue",
                            n0.ULongValue AS "ULongValue",
                            n0.UShortValue AS "UShortValue",
                            n0.DoubleValue AS "DoubleValue",
                            n0.FloatValue AS "FloatValue",
                            n0.DecimalValue AS "DecimalValue",
                            n0.CharValue AS "CharValue",
                            n0.BlobValue AS "BlobValue"
                     FROM "NumericTypes" AS n0
                     WHERE (n0.CharValue >= 'a' AND n0.CharValue <= 'z')
                     """.Replace("\r\n", "\n"),
            command.CommandText.Replace("\r\n", "\n"));

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
        Assert.Equal("""
                     SELECT n0.Id AS "Id",
                            n0.IntValue AS "IntValue",
                            n0.LongValue AS "LongValue",
                            n0.ShortValue AS "ShortValue",
                            n0.ByteValue AS "ByteValue",
                            n0.SByteValue AS "SByteValue",
                            n0.UIntValue AS "UIntValue",
                            n0.ULongValue AS "ULongValue",
                            n0.UShortValue AS "UShortValue",
                            n0.DoubleValue AS "DoubleValue",
                            n0.FloatValue AS "FloatValue",
                            n0.DecimalValue AS "DecimalValue",
                            n0.CharValue AS "CharValue",
                            n0.BlobValue AS "BlobValue"
                     FROM "NumericTypes" AS n0
                     WHERE (n0.CharValue >= 'A' AND n0.CharValue <= 'Z')
                     """.Replace("\r\n", "\n"),
            command.CommandText.Replace("\r\n", "\n"));

        List<NumericType> results = query.ToList();
        Assert.Equal(2, results.Count);
        Assert.Contains(results, r => r.CharValue == 'A');
        Assert.Contains(results, r => r.CharValue == 'Z');
    }

    private static TestDatabase SetupDatabase(char one, char two, char three, [CallerMemberName] string? methodName = null)
    {
        TestDatabase db = new(methodName);

        db.Table<NumericType>().CreateTable();
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
}
