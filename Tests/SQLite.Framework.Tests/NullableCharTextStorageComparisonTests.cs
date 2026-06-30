using System.ComponentModel.DataAnnotations;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

internal sealed class NullableCharRow
{
    [Key]
    public int Id { get; set; }

    public char? NValue { get; set; }
}

public class NullableCharTextStorageComparisonTests
{
    private static readonly NullableCharRow[] Data =
    [
        new NullableCharRow { Id = 1, NValue = 'A' },
        new NullableCharRow { Id = 2, NValue = null },
        new NullableCharRow { Id = 3, NValue = 'C' },
        new NullableCharRow { Id = 4, NValue = 'm' },
    ];

    private static TestDatabase Create()
    {
        TestDatabase db = new();
        db.Table<NullableCharRow>().Schema.CreateTable();
        foreach (NullableCharRow r in Data)
        {
            db.Table<NullableCharRow>().Add(r);
        }

        return db;
    }

    [Fact]
    public void EqualityWithCharLiteral()
    {
        using TestDatabase db = Create();

        List<int> expected = Data.Where(r => r.NValue == 'A').Select(r => r.Id).OrderBy(i => i).ToList();
        List<int> actual = db.Table<NullableCharRow>().Where(r => r.NValue == 'A').Select(r => r.Id).OrderBy(i => i).ToList();

        Assert.Equal([1], expected);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void GreaterOrEqualWithCharLiteral()
    {
        using TestDatabase db = Create();

        List<int> expected = Data.Where(r => r.NValue >= 'C').Select(r => r.Id).OrderBy(i => i).ToList();
        List<int> actual = db.Table<NullableCharRow>().Where(r => r.NValue >= 'C').Select(r => r.Id).OrderBy(i => i).ToList();

        Assert.Equal([3, 4], expected);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void LessThanWithCharLiteral()
    {
        using TestDatabase db = Create();

        List<int> expected = Data.Where(r => r.NValue < 'm').Select(r => r.Id).OrderBy(i => i).ToList();
        List<int> actual = db.Table<NullableCharRow>().Where(r => r.NValue < 'm').Select(r => r.Id).OrderBy(i => i).ToList();

        Assert.Equal([1, 3], expected);
        Assert.Equal(expected, actual);
    }
}
