using SQLite.Framework.Enums;
using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class CharNonAsciiCaseConversionTests
{
    private static TestDatabase SeedIntegerStorage(params char[] values)
    {
        TestDatabase db = new(b => b.UseCharStorage(CharStorageMode.Integer));
        db.Table<NumericType>().Schema.CreateTable();
        for (int i = 0; i < values.Length; i++)
        {
            db.Table<NumericType>().Add(new NumericType
            {
                Id = i + 1,
                IntValue = 0,
                LongValue = 0,
                ShortValue = 0,
                ByteValue = 0,
                SByteValue = 0,
                UIntValue = 0,
                ULongValue = 0,
                UShortValue = 0,
                DoubleValue = 0,
                FloatValue = 0,
                DecimalValue = 0,
                CharValue = values[i],
            });
        }
        return db;
    }

    private static TestDatabase SeedTextStorage(params char[] values)
    {
        TestDatabase db = new();
        db.Table<NumericType>().Schema.CreateTable();
        for (int i = 0; i < values.Length; i++)
        {
            db.Table<NumericType>().Add(new NumericType
            {
                Id = i + 1,
                IntValue = 0,
                LongValue = 0,
                ShortValue = 0,
                ByteValue = 0,
                SByteValue = 0,
                UIntValue = 0,
                ULongValue = 0,
                UShortValue = 0,
                DoubleValue = 0,
                FloatValue = 0,
                DecimalValue = 0,
                CharValue = values[i],
            });
        }
        return db;
    }

    [Fact]
    public void ToLowerInvariant_NonAsciiUpperCase_IntegerStorage_FoldsOnlyAscii()
    {
        char input = 'Ä';
        using TestDatabase db = SeedIntegerStorage(input);

        char actual = db.Table<NumericType>().Select(n => char.ToLowerInvariant(n.CharValue)).First();

        Assert.Equal(input, actual);
    }

    [Fact]
    public void ToUpperInvariant_NonAsciiLowerCase_IntegerStorage_FoldsOnlyAscii()
    {
        char input = 'ä';
        using TestDatabase db = SeedIntegerStorage(input);

        char actual = db.Table<NumericType>().Select(n => char.ToUpperInvariant(n.CharValue)).First();

        Assert.Equal(input, actual);
    }

    [Fact]
    public void ToLowerInvariant_NonAsciiUpperCase_TextStorage_FoldsOnlyAscii()
    {
        char input = 'Ä';
        using TestDatabase db = SeedTextStorage(input);

        char actual = db.Table<NumericType>().Select(n => char.ToLowerInvariant(n.CharValue)).First();

        Assert.Equal(input, actual);
    }

    [Fact]
    public void ToUpperInvariant_NonAsciiLowerCase_TextStorage_FoldsOnlyAscii()
    {
        char input = 'ä';
        using TestDatabase db = SeedTextStorage(input);

        char actual = db.Table<NumericType>().Select(n => char.ToUpperInvariant(n.CharValue)).First();

        Assert.Equal(input, actual);
    }

    [Fact]
    public void ToLower_NonAscii_IntegerStorage_FoldsOnlyAscii()
    {
        char input = 'Ñ';
        using TestDatabase db = SeedIntegerStorage(input);

        char actual = db.Table<NumericType>().Select(n => char.ToLower(n.CharValue)).First();

        Assert.Equal(input, actual);
    }

    [Fact]
    public void ToUpper_NonAscii_IntegerStorage_FoldsOnlyAscii()
    {
        char input = 'ñ';
        using TestDatabase db = SeedIntegerStorage(input);

        char actual = db.Table<NumericType>().Select(n => char.ToUpper(n.CharValue)).First();

        Assert.Equal(input, actual);
    }

    [Fact]
    public void ToLowerInvariant_AsciiUpperCase_IntegerStorage_MatchesDotNet()
    {
        char input = 'Q';
        using TestDatabase db = SeedIntegerStorage(input);

        char oracle = char.ToLowerInvariant(input);
        char actual = db.Table<NumericType>().Select(n => char.ToLowerInvariant(n.CharValue)).First();

        Assert.Equal(oracle, actual);
    }

    [Fact]
    public void ToUpperInvariant_AsciiLowerCase_TextStorage_MatchesDotNet()
    {
        char input = 'q';
        using TestDatabase db = SeedTextStorage(input);

        char oracle = char.ToUpperInvariant(input);
        char actual = db.Table<NumericType>().Select(n => char.ToUpperInvariant(n.CharValue)).First();

        Assert.Equal(oracle, actual);
    }
}
