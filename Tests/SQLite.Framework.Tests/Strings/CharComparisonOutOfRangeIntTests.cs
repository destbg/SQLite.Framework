using System.ComponentModel.DataAnnotations;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

internal sealed class CharCodeRow
{
    [Key]
    public int Id { get; set; }

    public char Letter { get; set; }

    public int Code { get; set; }
}

public class CharComparisonOutOfRangeIntTests
{
    private static TestDatabase SetupDatabase(int code = 10)
    {
        TestDatabase db = new();
        db.Table<CharCodeRow>().Schema.CreateTable();
        db.Table<CharCodeRow>().Add(new CharCodeRow { Id = 1, Letter = 'b', Code = code });
        return db;
    }

    [Fact]
    public void EqualsIntAboveCharRangeIsFalse()
    {
        using TestDatabase db = SetupDatabase();

        bool expected = db.Table<CharCodeRow>().AsEnumerable().Any(r => r.Letter == 65634);

        Assert.False(expected);

        bool actual = db.Table<CharCodeRow>().Any(r => r.Letter == 65634);

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void GreaterThanOrEqualNegativeIntIsTrue()
    {
        using TestDatabase db = SetupDatabase();

        bool expected = db.Table<CharCodeRow>().AsEnumerable().Any(r => r.Letter >= -1);

        Assert.True(expected);

        bool actual = db.Table<CharCodeRow>().Any(r => r.Letter >= -1);

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void GreaterThanIntAboveCharRangeIsFalse()
    {
        using TestDatabase db = SetupDatabase();

        bool expected = db.Table<CharCodeRow>().AsEnumerable().Any(r => r.Letter > 65601);

        Assert.False(expected);

        bool actual = db.Table<CharCodeRow>().Any(r => r.Letter > 65601);

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void GreaterThanNegativeIntColumnIsTrue()
    {
        using TestDatabase db = SetupDatabase(-1);

        bool expected = db.Table<CharCodeRow>().AsEnumerable().Any(r => r.Letter > r.Code);

        Assert.True(expected);

        bool actual = db.Table<CharCodeRow>().Any(r => r.Letter > r.Code);

        Assert.Equal(expected, actual);
    }
}
