using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class SetOperationOperandScopingTests
{
    [Fact]
    public void ConcatWithOrderedTakenOperandMatchesObjects()
    {
        List<Book> rows = Rows();
        using TestDatabase db = Seed(rows);

        List<int> expected = rows.Concat(rows.OrderByDescending(b => b.Price).Take(2))
            .Select(b => b.Id)
            .OrderBy(id => id)
            .ToList();
        List<int> actual = db.Table<Book>().Concat(db.Table<Book>().OrderByDescending(b => b.Price).Take(2))
            .Select(b => b.Id)
            .OrderBy(id => id)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void UnionWithOrderedTakenOperandMatchesObjects()
    {
        List<Book> rows = Rows();
        using TestDatabase db = Seed(rows);

        List<int> expected = rows.Where(b => b.Id <= 2)
            .Union(rows.OrderByDescending(b => b.Price).Take(2))
            .Select(b => b.Id)
            .OrderBy(id => id)
            .ToList();
        List<int> actual = db.Table<Book>().Where(b => b.Id <= 2)
            .Union(db.Table<Book>().OrderByDescending(b => b.Price).Take(2))
            .Select(b => b.Id)
            .OrderBy(id => id)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ConcatWithSkippedOperandMatchesObjects()
    {
        List<Book> rows = Rows();
        using TestDatabase db = Seed(rows);

        List<int> expected = rows.Concat(rows.OrderByDescending(b => b.Price).Skip(2))
            .Select(b => b.Id)
            .OrderBy(id => id)
            .ToList();
        List<int> actual = db.Table<Book>().Concat(db.Table<Book>().OrderByDescending(b => b.Price).Skip(2))
            .Select(b => b.Id)
            .OrderBy(id => id)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void TakeZeroBeforeConcatMatchesObjects()
    {
        List<Book> rows = Rows();
        using TestDatabase db = Seed(rows);

        List<int> expected = rows.Take(0).Concat(rows).Select(b => b.Id).OrderBy(id => id).ToList();
        List<int> actual = db.Table<Book>().Take(0).Concat(db.Table<Book>())
            .Select(b => b.Id)
            .OrderBy(id => id)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void OrderedSkipBeforeConcatMatchesObjects()
    {
        List<Book> rows = Rows();
        using TestDatabase db = Seed(rows);

        List<int> expected = rows.OrderByDescending(b => b.Price).Skip(2)
            .Concat(rows)
            .Select(b => b.Id)
            .OrderBy(id => id)
            .ToList();
        List<int> actual = db.Table<Book>().OrderByDescending(b => b.Price).Skip(2)
            .Concat(db.Table<Book>())
            .Select(b => b.Id)
            .OrderBy(id => id)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ConcatWithTakeZeroOperandMatchesObjects()
    {
        List<Book> rows = Rows();
        using TestDatabase db = Seed(rows);

        List<int> expected = rows.Concat(rows.Take(0)).Select(b => b.Id).OrderBy(id => id).ToList();
        List<int> actual = db.Table<Book>().Concat(db.Table<Book>().Take(0))
            .Select(b => b.Id)
            .OrderBy(id => id)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ConcatWithSkipZeroOperandMatchesObjects()
    {
        List<Book> rows = Rows();
        using TestDatabase db = Seed(rows);

        List<int> expected = rows.Concat(rows.Skip(0)).Select(b => b.Id).OrderBy(id => id).ToList();
        List<int> actual = db.Table<Book>().Concat(db.Table<Book>().Skip(0))
            .Select(b => b.Id)
            .OrderBy(id => id)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void PlainConcatStillWorks()
    {
        List<Book> rows = Rows();
        using TestDatabase db = Seed(rows);

        List<int> expected = rows.Concat(rows).Select(b => b.Id).OrderBy(id => id).ToList();
        List<int> actual = db.Table<Book>().Concat(db.Table<Book>())
            .Select(b => b.Id)
            .OrderBy(id => id)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void FilteredOperandsStillWork()
    {
        List<Book> rows = Rows();
        using TestDatabase db = Seed(rows);

        List<int> expected = rows.Where(b => b.Id <= 2)
            .Concat(rows.Where(b => b.Id >= 4))
            .Select(b => b.Id)
            .OrderBy(id => id)
            .ToList();
        List<int> actual = db.Table<Book>().Where(b => b.Id <= 2)
            .Concat(db.Table<Book>().Where(b => b.Id >= 4))
            .Select(b => b.Id)
            .OrderBy(id => id)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void OrderedLeftOperandWithoutPagingIsRejected()
    {
        List<Book> rows = Rows();
        using TestDatabase db = Seed(rows);

        NotSupportedException exception = Assert.Throws<NotSupportedException>(() => db.Table<Book>()
            .OrderBy(book => book.Price)
            .Concat(db.Table<Book>())
            .ToList());

        Assert.Contains("without paging", exception.Message);
    }

    [Fact]
    public void OrderedRightOperandWithoutPagingIsRejected()
    {
        List<Book> rows = Rows();
        using TestDatabase db = Seed(rows);

        NotSupportedException exception = Assert.Throws<NotSupportedException>(() => db.Table<Book>()
            .Concat(db.Table<Book>().OrderBy(book => book.Price))
            .ToList());

        Assert.Contains("combined operand", exception.Message);
    }

    private static List<Book> Rows()
    {
        return Enumerable.Range(1, 5)
            .Select(i => new Book { Id = i, Title = "T" + i, AuthorId = i % 2, Price = i })
            .ToList();
    }

    private static TestDatabase Seed(List<Book> rows)
    {
        TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().AddRange(rows);
        return db;
    }
}
