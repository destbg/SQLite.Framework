using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class CompareSubstringOutOfRangeIndexTests
{
    [Fact]
    public void Compare_SubstringOverload_OutOfRangeStartIndex_DivergesFromDotNet()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().Add(new Book { Id = 1, Title = "ab", AuthorId = 1, Price = 1 });

        Assert.Throws<ArgumentOutOfRangeException>(() => string.Compare("ab", 5, "cd", 0, 1));

        int actual = db.Table<Book>()
            .Select(b => string.Compare(b.Title, 5, "cd", 0, 1))
            .First();

        Assert.Equal(-1, Math.Sign(actual));
    }
}
