using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class DateTimeAddRelativePre1970Tests
{
    private static TestDatabase SeedBirth(DateTime birth)
    {
        TestDatabase db = new();
        db.Table<Author>().Schema.CreateTable();
        db.Table<Author>().Add(new Author { Id = 1, Name = "x", Email = "e", BirthDate = birth });
        return db;
    }

    private static DateTime AddYears(TestDatabase db, int n)
        => db.Table<Author>().Where(a => a.Id == 1).Select(a => a.BirthDate.AddYears(n)).First();

    private static DateTime AddMonths(TestDatabase db, int n)
        => db.Table<Author>().Where(a => a.Id == 1).Select(a => a.BirthDate.AddMonths(n)).First();

    [Fact]
    public void Pre1970SubSecond_AddYears_MatchesDotNet()
    {
        DateTime birth = new DateTime(1969, 12, 31, 23, 59, 59).AddTicks(9_999_999);
        using TestDatabase db = SeedBirth(birth);

        foreach (int n in new[] { 1, 5, -1, 0, -5 })
        {
            Assert.Equal(birth.AddYears(n), AddYears(db, n));
        }
    }

    [Fact]
    public void Pre1970SubSecond_AddMonths_MatchesDotNet()
    {
        DateTime birth = new DateTime(1969, 12, 31, 23, 59, 59).AddTicks(9_999_999);
        using TestDatabase db = SeedBirth(birth);

        foreach (int n in new[] { 1, 13, -1, 0, -14 })
        {
            Assert.Equal(birth.AddMonths(n), AddMonths(db, n));
        }
    }

    [Fact]
    public void Pre1970SubSecond_DayOverflow_AddMonths_MatchesDotNet()
    {
        DateTime birth = new DateTime(1968, 1, 31, 23, 59, 59).AddTicks(1234567);
        using TestDatabase db = SeedBirth(birth);

        DateTime expected = birth.AddMonths(1);
        DateTime actual = AddMonths(db, 1);

        Assert.Equal(new DateTime(1968, 2, 29, 23, 59, 59).AddTicks(1234567), expected);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Pre1970OnSecondBoundary_AddYears_MatchesDotNet()
    {
        DateTime birth = new DateTime(1955, 3, 10, 8, 15, 30);
        using TestDatabase db = SeedBirth(birth);

        Assert.Equal(birth.AddYears(3), AddYears(db, 3));
        Assert.Equal(birth.AddMonths(20), AddMonths(db, 20));
    }

    [Fact]
    public void Post1970SubSecond_StillMatchesDotNet()
    {
        DateTime birth = new DateTime(1985, 6, 15, 12, 30, 45).AddTicks(1234567);
        using TestDatabase db = SeedBirth(birth);

        Assert.Equal(birth.AddYears(2), AddYears(db, 2));
        Assert.Equal(birth.AddMonths(7), AddMonths(db, 7));
        Assert.Equal(birth.AddMonths(-9), AddMonths(db, -9));
    }

    [Fact]
    public void Pre1970SubSecond_AddYears_ConstantLiteral_MatchesDotNet()
    {
        DateTime birth = new DateTime(1969, 12, 31, 23, 59, 59).AddTicks(9_999_999);
        using TestDatabase db = SeedBirth(birth);

        DateTime expected = birth.AddYears(1);
        DateTime actual = db.Table<Author>().Where(a => a.Id == 1).Select(a => a.BirthDate.AddYears(1)).First();

        Assert.Equal(expected, actual);
    }
}
