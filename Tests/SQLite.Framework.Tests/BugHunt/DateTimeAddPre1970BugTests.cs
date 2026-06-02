using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests.BugHunt;

public class DateTimeAddPre1970BugTests
{
    [Fact]
    public void AddYears_OnPre1970WithSubSecondTicks_MatchesDotNet()
    {
        DateTime birth = new DateTime(1969, 12, 31, 23, 59, 59).AddTicks(9_999_999);
        using TestDatabase db = new();
        db.Table<Author>().Schema.CreateTable();
        db.Table<Author>().Add(new Author { Id = 1, Name = "x", Email = "e", BirthDate = birth });

        DateTime expected = birth.AddYears(1);
        DateTime actual = db.Table<Author>()
            .Where(a => a.Id == 1)
            .Select(a => a.BirthDate.AddYears(1))
            .First();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void AddMonths_OnPre1970WithSubSecondTicks_MatchesDotNet()
    {
        DateTime birth = new DateTime(1969, 12, 31, 23, 59, 59).AddTicks(9_999_999);
        using TestDatabase db = new();
        db.Table<Author>().Schema.CreateTable();
        db.Table<Author>().Add(new Author { Id = 1, Name = "x", Email = "e", BirthDate = birth });

        DateTime expected = birth.AddMonths(1);
        DateTime actual = db.Table<Author>()
            .Where(a => a.Id == 1)
            .Select(a => a.BirthDate.AddMonths(1))
            .First();

        Assert.Equal(expected, actual);
    }
}
