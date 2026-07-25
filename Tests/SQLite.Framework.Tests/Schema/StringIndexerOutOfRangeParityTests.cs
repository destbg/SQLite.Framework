using System;
using System.Linq;
using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class StringIndexerOutOfRangeParityTests
{
    private static TestDatabase Seed()
    {
        TestDatabase db = new();
        db.Table<Author>().Schema.CreateTable();
        db.Table<Author>().Add(new Author { Id = 1, Name = "hello", Email = "e", BirthDate = new DateTime(2000, 1, 1) });
        return db;
    }

    [Fact]
    public void Indexer_InBounds_MatchesLinqToObjects()
    {
        using TestDatabase db = Seed();

        char oracle = "hello"[1];
        char actual = db.Table<Author>().Where(x => x.Id == 1).Select(x => x.Name[1]).First();

        Assert.Equal(oracle, actual);
    }

    [Fact]
    public void Indexer_PastEnd_DoesNotThrowIndexOutOfRange()
    {
        using TestDatabase db = Seed();

        Assert.Throws<IndexOutOfRangeException>(() => { char _ = "hello"[10]; });

        Exception ex = Assert.ThrowsAny<Exception>(() =>
            db.Table<Author>().Where(x => x.Id == 1).Select(x => x.Name[10]).First());
        Assert.IsNotType<IndexOutOfRangeException>(ex);
    }

    [Fact]
    public void Indexer_Negative_ReadsFromEnd()
    {
        using TestDatabase db = Seed();

        int index = -2;
        Assert.Throws<IndexOutOfRangeException>(() => { char _ = "hello"[index]; });

        char actual = db.Table<Author>().Where(x => x.Id == 1).Select(x => x.Name[index]).First();
        Assert.Equal('o', actual);
    }
}
