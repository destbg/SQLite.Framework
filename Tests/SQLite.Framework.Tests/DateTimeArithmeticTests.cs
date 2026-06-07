using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class DateTimeArithmeticTests
{
    private static TestDatabase WithBirth(DateTime birth)
    {
        TestDatabase db = new();
        db.Table<Author>().Schema.CreateTable();
        db.Table<Author>().Add(new Author { Id = 1, Name = "x", Email = "e", BirthDate = birth });
        return db;
    }

    private static DateTime Project(TestDatabase db, System.Linq.Expressions.Expression<Func<Author, DateTime>> selector)
    {
        return db.Table<Author>().Where(a => a.Id == 1).Select(selector).First();
    }

    [Fact]
    public void AddMonthsClampsEndOfMonthLikeDotNet()
    {
        DateTime birth = new(2021, 1, 31);
        using TestDatabase db = WithBirth(birth);

        DateTime actual = Project(db, a => a.BirthDate.AddMonths(1));

        Assert.Equal(birth.AddMonths(1), actual);
    }

    [Fact]
    public void AddYearsClampsLeapDayLikeDotNet()
    {
        DateTime birth = new(2020, 2, 29);
        using TestDatabase db = WithBirth(birth);

        DateTime actual = Project(db, a => a.BirthDate.AddYears(1));

        Assert.Equal(birth.AddYears(1), actual);
    }

    [Theory]
    [InlineData("2021-01-31 10:30:45", 1)]
    [InlineData("2021-01-31 10:30:45", 13)]
    [InlineData("2021-03-31 23:59:59", -1)]
    [InlineData("2021-01-15 08:00:00", 1)]
    [InlineData("2021-05-31 00:00:00", 0)]
    [InlineData("2020-02-29 12:00:00", 12)]
    [InlineData("2021-12-15 06:30:00", 3)]
    public void AddMonthsMatchesDotNetAcrossCases(string birthText, int months)
    {
        DateTime birth = DateTime.Parse(birthText, System.Globalization.CultureInfo.InvariantCulture);
        using TestDatabase db = WithBirth(birth);

        DateTime actual = db.Table<Author>().Where(a => a.Id == 1).Select(a => a.BirthDate.AddMonths(months)).First();

        Assert.Equal(birth.AddMonths(months), actual);
    }

    [Fact]
    public void AddMonthsWithColumnOperandMatchesDotNet()
    {
        DateTime birth = new(2021, 1, 31, 9, 15, 0);
        using TestDatabase db = new();
        db.Table<Author>().Schema.CreateTable();
        db.Table<Author>().Add(new Author { Id = 5, Name = "x", Email = "e", BirthDate = birth });

        DateTime actual = db.Table<Author>().Where(a => a.Id == 5).Select(a => a.BirthDate.AddMonths(a.Id)).First();

        Assert.Equal(birth.AddMonths(5), actual);
    }

    [Fact]
    public void DateOnlyAddMonthsClampsEndOfMonthLikeDotNet()
    {
        DateOnly date = new(2021, 1, 31);
        using TestDatabase db = new();
        db.Table<DateOnlyMethodEntity>().Schema.CreateTable();
        db.Table<DateOnlyMethodEntity>().Add(new DateOnlyMethodEntity { Id = 1, Date = date });

        DateOnly actual = db.Table<DateOnlyMethodEntity>().Where(x => x.Id == 1).Select(x => x.Date.AddMonths(1)).First();

        Assert.Equal(date.AddMonths(1), actual);
    }

    [Fact]
    public void YearOfPre1970DateWithSubSecondMatchesDotNet()
    {
        DateTime birth = new DateTime(1969, 12, 31, 23, 59, 59).AddTicks(5_000_000);
        using TestDatabase db = WithBirth(birth);

        int actual = db.Table<Author>().Where(a => a.Id == 1).Select(a => a.BirthDate.Year).First();

        Assert.Equal(birth.Year, actual);
    }

    [Fact]
    public void SecondOfPre1970DateWithSubSecondMatchesDotNet()
    {
        DateTime birth = new DateTime(1950, 6, 15, 12, 30, 45).AddTicks(5_000_000);
        using TestDatabase db = WithBirth(birth);

        int actual = db.Table<Author>().Where(a => a.Id == 1).Select(a => a.BirthDate.Second).First();

        Assert.Equal(birth.Second, actual);
    }
}
