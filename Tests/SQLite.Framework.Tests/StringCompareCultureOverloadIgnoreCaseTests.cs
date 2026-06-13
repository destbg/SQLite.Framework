using System.ComponentModel.DataAnnotations;
using System.Globalization;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

internal sealed class CaseFoldCompareRow
{
    [Key]
    public int Id { get; set; }

    public string Name { get; set; } = "";
}

public class StringCompareCultureOverloadIgnoreCaseTests
{
    private static TestDatabase SetupDatabase()
    {
        TestDatabase db = new();
        db.Table<CaseFoldCompareRow>().Schema.CreateTable();
        db.Table<CaseFoldCompareRow>().Add(new CaseFoldCompareRow { Id = 1, Name = "HELLO" });
        return db;
    }

    [Fact]
    public void BoolIgnoreCaseCultureOverloadInSelectComparesCaseInsensitive()
    {
        using TestDatabase db = SetupDatabase();
        CultureInfo invariant = CultureInfo.InvariantCulture;

        int expected = db.Table<CaseFoldCompareRow>().AsEnumerable()
            .Select(r => Math.Sign(string.Compare(r.Name, "hello", true, invariant)))
            .First();

        Assert.Equal(0, expected);

        int actual = db.Table<CaseFoldCompareRow>()
            .Select(r => Math.Sign(string.Compare(r.Name, "hello", true, invariant)))
            .First();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void CompareOptionsIgnoreCaseOverloadInSelectComparesCaseInsensitive()
    {
        using TestDatabase db = SetupDatabase();
        CultureInfo invariant = CultureInfo.InvariantCulture;

        int expected = db.Table<CaseFoldCompareRow>().AsEnumerable()
            .Select(r => Math.Sign(string.Compare(r.Name, "hello", invariant, CompareOptions.IgnoreCase)))
            .First();

        Assert.Equal(0, expected);

        int actual = db.Table<CaseFoldCompareRow>()
            .Select(r => Math.Sign(string.Compare(r.Name, "hello", invariant, CompareOptions.IgnoreCase)))
            .First();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void BoolIgnoreCaseCultureOverloadInWhereKeepsMatchingRows()
    {
        using TestDatabase db = SetupDatabase();
        CultureInfo invariant = CultureInfo.InvariantCulture;

        bool expected = db.Table<CaseFoldCompareRow>().AsEnumerable()
            .Any(r => string.Compare(r.Name, "hello", true, invariant) == 0);

        Assert.True(expected);

        bool actual = db.Table<CaseFoldCompareRow>()
            .Any(r => string.Compare(r.Name, "hello", true, invariant) == 0);

        Assert.Equal(expected, actual);
    }
}
