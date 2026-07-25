using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class DateTimeAddYearBoundaryTests
{
    [Fact]
    public void AddMonthsIntoDecemberOfYear9999ReturnsDefaultDate()
    {
        using TestDatabase db = new();
        db.Table<Author>().Schema.CreateTable();

        DateTime seed = new(9999, 11, 15, 10, 20, 30);
        db.Table<Author>().Add(new Author { Id = 1, Name = "x", Email = "y", BirthDate = seed });

        DateTime actual = db.Table<Author>()
            .Where(a => a.Id == 1)
            .Select(a => a.BirthDate.AddMonths(1))
            .First();

        Assert.Equal(new DateTime(9999, 12, 15, 10, 20, 30), seed.AddMonths(1));
        Assert.Equal(default, actual);
    }

    [Fact]
    public void AddYearsIntoYear9999ReturnsDefaultDate()
    {
        using TestDatabase db = new();
        db.Table<Author>().Schema.CreateTable();

        DateTime seed = new(9990, 12, 15, 1, 2, 3);
        db.Table<Author>().Add(new Author { Id = 1, Name = "x", Email = "y", BirthDate = seed });

        DateTime actual = db.Table<Author>()
            .Where(a => a.Id == 1)
            .Select(a => a.BirthDate.AddYears(9))
            .First();

        Assert.Equal(new DateTime(9999, 12, 15, 1, 2, 3), seed.AddYears(9));
        Assert.Equal(default, actual);
    }
}
