using SQLite.Framework;
using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;
using System.Linq;

namespace SQLite.Framework.Tests;

public class CapturedTableSubqueryFilterTests
{
    [Fact]
    public void CapturedTableInsideAnySubquery_StillAppliesGlobalFilter()
    {
        using TestDatabase db = new(b => b.AddQueryFilter<SoftDeletableBook>(s => !s.IsDeleted));
        db.Table<Author>().Schema.CreateTable();
        db.Table<SoftDeletableBook>().Schema.CreateTable();

        Author[] authors =
        [
            new Author { Id = 1, Name = "a1", Email = "e1", BirthDate = default },
            new Author { Id = 2, Name = "a2", Email = "e2", BirthDate = default },
        ];
        SoftDeletableBook[] books =
        [
            new SoftDeletableBook { Id = 1, Title = "live", IsDeleted = false },
            new SoftDeletableBook { Id = 2, Title = "gone", IsDeleted = true },
        ];
        db.Table<Author>().AddRange(authors);
        db.Table<SoftDeletableBook>().AddRange(books);

        SoftDeletableBook[] visibleBooks = books.Where(s => !s.IsDeleted).ToArray();
        List<int> oracle = authors
            .Where(a => visibleBooks.Any(s => s.Id == a.Id))
            .Select(a => a.Id)
            .OrderBy(i => i)
            .ToList();

        SQLiteTable<SoftDeletableBook> capturedBooks = db.Table<SoftDeletableBook>();
        List<int> actual = db.Table<Author>()
            .Where(a => capturedBooks.Any(s => s.Id == a.Id))
            .Select(a => a.Id)
            .OrderBy(i => i)
            .ToList();

        Assert.Equal([1], oracle);
        Assert.Equal(oracle, actual);
    }
}
