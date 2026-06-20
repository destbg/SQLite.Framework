using System.Collections.Generic;
using System.Linq;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("NestedFilterAuthors")]
file sealed class NestedFilterAuthor
{
    [Key]
    public int Id { get; set; }

    public required string Name { get; set; }
}

[Table("NestedFilterBooks")]
file sealed class NestedFilterBook
{
    [Key]
    public int Id { get; set; }

    public int AuthorId { get; set; }

    public bool IsDeleted { get; set; }
}

public class QueryFilterInsideFilterBodyParityTests
{
    [Fact]
    public void FilterReferencingAnotherFilteredTable_AppliesThatFilter()
    {
        TestDatabase? captured = null;
        using TestDatabase db = new(b =>
        {
            b.AddQueryFilter<NestedFilterBook>(x => !x.IsDeleted);
            b.AddQueryFilter<NestedFilterAuthor>(a => captured!.Table<NestedFilterBook>().Any(x => x.AuthorId == a.Id));
        });
        captured = db;

        db.Table<NestedFilterAuthor>().Schema.CreateTable();
        db.Table<NestedFilterBook>().Schema.CreateTable();

        NestedFilterAuthor[] authors =
        [
            new NestedFilterAuthor { Id = 1, Name = "a1" },
            new NestedFilterAuthor { Id = 2, Name = "a2" },
        ];
        NestedFilterBook[] books =
        [
            new NestedFilterBook { Id = 1, AuthorId = 1, IsDeleted = true },
            new NestedFilterBook { Id = 2, AuthorId = 2, IsDeleted = false },
        ];
        db.Table<NestedFilterAuthor>().AddRange(authors);
        db.Table<NestedFilterBook>().AddRange(books);

        List<NestedFilterBook> visibleBooks = books.Where(x => !x.IsDeleted).ToList();
        List<string> expected = authors
            .Where(a => visibleBooks.Any(x => x.AuthorId == a.Id))
            .Select(a => a.Name)
            .OrderBy(x => x)
            .ToList();

        List<string> actual = db.Table<NestedFilterAuthor>()
            .Select(a => a.Name)
            .OrderBy(x => x)
            .ToList();

        Assert.Equal(["a2"], expected);
        Assert.Equal(expected, actual);
    }
}
