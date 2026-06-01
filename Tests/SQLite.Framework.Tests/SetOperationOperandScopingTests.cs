using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class SetOperationOperandScopingTests
{
    private static TestDatabase Seed()
    {
        TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        for (int i = 1; i <= 5; i++)
        {
            db.Table<Book>().Add(new Book { Id = i, Title = "T" + i, AuthorId = 1, Price = i });
        }

        return db;
    }

    [Fact]
    public void ConcatWithOrderedTakenOperandThrows()
    {
        using TestDatabase db = Seed();

        Assert.Throws<System.NotSupportedException>(() =>
            db.Table<Book>().Concat(db.Table<Book>().OrderBy(b => b.Price).Take(2)).ToList());
    }

    [Fact]
    public void UnionWithOrderedTakenOperandThrows()
    {
        using TestDatabase db = Seed();

        Assert.Throws<System.NotSupportedException>(() =>
            db.Table<Book>().Union(db.Table<Book>().OrderBy(b => b.Price).Take(2)).ToList());
    }

    [Fact]
    public void ConcatWithSkippedOperandThrows()
    {
        using TestDatabase db = Seed();

        Assert.Throws<System.NotSupportedException>(() =>
            db.Table<Book>().Concat(db.Table<Book>().OrderBy(b => b.Price).Skip(2)).ToList());
    }

    [Fact]
    public void TakeBeforeConcatThrows()
    {
        using TestDatabase db = Seed();

        Assert.Throws<System.NotSupportedException>(() =>
            db.Table<Book>().Take(2).Concat(db.Table<Book>()).ToList());
    }

    [Fact]
    public void SkipBeforeConcatThrows()
    {
        using TestDatabase db = Seed();

        Assert.Throws<System.NotSupportedException>(() =>
            db.Table<Book>().Skip(2).Concat(db.Table<Book>()).ToList());
    }

    [Fact]
    public void ConcatWithTakeOnlyOperandThrows()
    {
        using TestDatabase db = Seed();

        Assert.Throws<System.NotSupportedException>(() =>
            db.Table<Book>().Concat(db.Table<Book>().Take(2)).ToList());
    }

    [Fact]
    public void ConcatWithSkipOnlyOperandThrows()
    {
        using TestDatabase db = Seed();

        Assert.Throws<System.NotSupportedException>(() =>
            db.Table<Book>().Concat(db.Table<Book>().Skip(2)).ToList());
    }

    [Fact]
    public void PlainConcatStillWorks()
    {
        using TestDatabase db = Seed();

        List<int> expected = Enumerable.Range(1, 5).Concat(Enumerable.Range(1, 5)).OrderBy(x => x).ToList();
        List<int> actual = db.Table<Book>().Concat(db.Table<Book>()).ToList().Select(b => b.Id).OrderBy(x => x).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void FilteredOperandsStillWork()
    {
        using TestDatabase db = Seed();

        List<int> expected = new[] { 1, 2 }.Concat(new[] { 4, 5 }).OrderBy(x => x).ToList();
        List<int> actual = db.Table<Book>().Where(b => b.Id <= 2)
            .Concat(db.Table<Book>().Where(b => b.Id >= 4))
            .ToList()
            .Select(b => b.Id).OrderBy(x => x).ToList();

        Assert.Equal(expected, actual);
    }
}
