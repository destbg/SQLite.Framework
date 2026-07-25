using System.Globalization;
using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class CharToLowerInvariantTests
{
    private static TestDatabase SetupDatabase(params char[] values)
    {
        TestDatabase db = new();
        db.Table<NumericType>().Schema.CreateTable();
        int id = 1;
        foreach (char c in values)
        {
            db.Table<NumericType>().Add(new NumericType
            {
                Id = id++,
                IntValue = 1, LongValue = 1, ShortValue = 1, ByteValue = 1,
                SByteValue = 1, UIntValue = 1, ULongValue = 1, UShortValue = 1,
                DoubleValue = 1, FloatValue = 1, DecimalValue = 1, CharValue = c
            });
        }
        return db;
    }

    [Fact]
    public void CharToLowerInvariant_InWhereClause_TranslatesToLower()
    {
        using TestDatabase db = SetupDatabase('A', 'B', 'c');

        List<NumericType> oracle = db.Table<NumericType>().AsEnumerable()
            .Where(n => char.ToLowerInvariant(n.CharValue) == 'a')
            .ToList();

        List<NumericType> actual = db.Table<NumericType>()
            .Where(n => char.ToLowerInvariant(n.CharValue) == 'a')
            .ToList();

        Assert.Equal(oracle.Count, actual.Count);
        Assert.Single(actual);
    }

    [Fact]
    public void CharToUpperInvariant_InWhereClause_TranslatesToUpper()
    {
        using TestDatabase db = SetupDatabase('a', 'b', 'C');

        List<NumericType> oracle = db.Table<NumericType>().AsEnumerable()
            .Where(n => char.ToUpperInvariant(n.CharValue) == 'A')
            .ToList();

        List<NumericType> actual = db.Table<NumericType>()
            .Where(n => char.ToUpperInvariant(n.CharValue) == 'A')
            .ToList();

        Assert.Equal(oracle.Count, actual.Count);
        Assert.Single(actual);
    }

    [Fact]
    public void CharToLower_WithCultureInfo_ThrowsNotSupported()
    {
        using TestDatabase db = SetupDatabase('A', 'B');

        Assert.Throws<NotSupportedException>(() =>
            db.Table<NumericType>()
                .Where(n => char.ToLower(n.CharValue, CultureInfo.InvariantCulture) == 'a')
                .ToList());
    }
}
