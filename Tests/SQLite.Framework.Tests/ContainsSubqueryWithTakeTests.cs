using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class ContainsSubqueryWithTakeTests
{
    [Fact]
    public void ContainsOverTakeSubqueryMatchesDotNet()
    {
        using TestDatabase db = new();
        db.Table<Author>().Schema.CreateTable();
        db.Table<NumericType>().Schema.CreateTable();

        Author[] authors =
        [
            new Author { Id = 5, Name = "a", Email = "a", BirthDate = default },
            new Author { Id = 6, Name = "b", Email = "b", BirthDate = default },
            new Author { Id = 7, Name = "c", Email = "c", BirthDate = default },
        ];
        NumericType[] values =
        [
            new NumericType { Id = 1, IntValue = 5 },
            new NumericType { Id = 2, IntValue = 6 },
            new NumericType { Id = 3, IntValue = 7 },
        ];
        foreach (Author a in authors)
        {
            db.Table<Author>().Add(a);
        }

        foreach (NumericType n in values)
        {
            db.Table<NumericType>().Add(n);
        }

        List<int> oracle = authors
            .Where(a => values.OrderBy(n => n.Id).Take(2).Select(n => n.IntValue).Contains(a.Id))
            .Select(a => a.Id)
            .OrderBy(x => x)
            .ToList();

        List<int> actual = db.Table<Author>()
            .Where(a => db.Table<NumericType>().OrderBy(n => n.Id).Take(2).Select(n => n.IntValue).Contains(a.Id))
            .Select(a => a.Id)
            .OrderBy(x => x)
            .ToList();

        Assert.Equal([5, 6], oracle);
        Assert.Equal(oracle, actual);
    }
}
