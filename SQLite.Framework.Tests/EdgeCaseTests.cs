using System.ComponentModel.DataAnnotations;
using SQLite.Framework.Exceptions;
using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class EdgeCaseTests
{
    [Fact]
    public void EmptyTableQuery()
    {
        using TestDatabase db = new();

        db.Table<Book>().CreateTable();

        List<Book> results = db.Table<Book>().ToList();

        Assert.NotNull(results);
        Assert.Empty(results);
    }

    [Fact]
    public void EmptyWhereClause()
    {
        using TestDatabase db = new();

        db.Table<Book>().CreateTable();
        db.Table<Book>().Add(new Book
        {
            Id = 1,
            Title = "Test",
            AuthorId = 1,
            Price = 10
        });

        List<Book> results = db.Table<Book>().Where(b => true).ToList();

        Assert.Single(results);
    }

    [Fact]
    public void QueryWithNoResults()
    {
        using TestDatabase db = new();

        db.Table<Book>().CreateTable();
        db.Table<Book>().Add(new Book
        {
            Id = 1,
            Title = "Test",
            AuthorId = 1,
            Price = 10
        });

        List<Book> results = db.Table<Book>().Where(b => b.Id == 999).ToList();

        Assert.Empty(results);
    }

    [Fact]
    public void FirstOrDefaultOnEmptyTable()
    {
        using TestDatabase db = new();

        db.Table<Book>().CreateTable();

        Book? result = db.Table<Book>().FirstOrDefault();

        Assert.Null(result);
    }

    [Fact]
    public void FirstOnEmptyTableThrows()
    {
        using TestDatabase db = new();

        db.Table<Book>().CreateTable();

        Assert.Throws<InvalidOperationException>(() => db.Table<Book>().First());
    }

    [Fact]
    public void CountOnEmptyTable()
    {
        using TestDatabase db = new();

        db.Table<Book>().CreateTable();

        int count = db.Table<Book>().Count();

        Assert.Equal(0, count);
    }

    [Fact]
    public void SumOnEmptyTable()
    {
        using TestDatabase db = new();

        db.Table<Book>().CreateTable();

        Assert.Throws<NullReferenceException>(() => db.Table<Book>().Sum(b => b.Price));
    }

    [Fact]
    public void AnyOnEmptyTable()
    {
        using TestDatabase db = new();

        db.Table<Book>().CreateTable();

        bool any = db.Table<Book>().Any();

        Assert.False(any);
    }

    [Fact]
    public void AllOnEmptyTable()
    {
        using TestDatabase db = new();

        db.Table<Book>().CreateTable();

        bool all = db.Table<Book>().All(b => b.Price > 0);

        Assert.True(all);
    }

    [Fact]
    public void InsertNullIntoRequiredField()
    {
        using TestDatabase db = new();

        db.Table<Book>().CreateTable();

        Assert.Throws<SQLiteException>(() =>
        {
            db.Table<Book>().Add(new Book
            {
                Id = 1,
                Title = null!,
                AuthorId = 1,
                Price = 10
            });
        });
    }

    [Fact]
    public void InsertDuplicateKey()
    {
        using TestDatabase db = new();

        db.Table<Book>().CreateTable();

        db.Table<Book>().Add(new Book
        {
            Id = 1,
            Title = "Test",
            AuthorId = 1,
            Price = 10
        });

        Assert.Throws<SQLiteException>(() =>
        {
            db.Table<Book>().Add(new Book
            {
                Id = 1,
                Title = "Test2",
                AuthorId = 1,
                Price = 20
            });
        });
    }

    [Fact]
    public void UpdateNonExistentRecord()
    {
        using TestDatabase db = new();

        db.Table<Book>().CreateTable();

        db.Table<Book>().Update(new Book
        {
            Id = 999,
            Title = "Non-existent",
            AuthorId = 1,
            Price = 10
        });

        int count = db.Table<Book>().Count();
        Assert.Equal(0, count);
    }

    [Fact]
    public void DeleteNonExistentRecord()
    {
        using TestDatabase db = new();

        db.Table<Book>().CreateTable();

        db.Table<Book>().Remove(new Book
        {
            Id = 999,
            Title = "Non-existent",
            AuthorId = 1,
            Price = 10
        });

        int count = db.Table<Book>().Count();
        Assert.Equal(0, count);
    }

    [Fact]
    public void ExecuteDeleteWithNoMatches()
    {
        using TestDatabase db = new();

        db.Table<Book>().CreateTable();
        db.Table<Book>().Add(new Book
        {
            Id = 1,
            Title = "Test",
            AuthorId = 1,
            Price = 10
        });

        db.Table<Book>().Where(b => b.Id == 999).ExecuteDelete();

        int count = db.Table<Book>().Count();
        Assert.Equal(1, count);
    }

    [Fact]
    public void ExecuteUpdateWithNoMatches()
    {
        using TestDatabase db = new();

        db.Table<Book>().CreateTable();
        db.Table<Book>().Add(new Book
        {
            Id = 1,
            Title = "Test",
            AuthorId = 1,
            Price = 10
        });

        db.Table<Book>()
            .Where(b => b.Id == 999)
            .ExecuteUpdate(s => s.Set(b => b.Price, 20));

        Book book = db.Table<Book>().First();
        Assert.Equal(10, book.Price);
    }

    [Fact]
    public void VeryLongStringInQuery()
    {
        using TestDatabase db = new();

        db.Table<Book>().CreateTable();

        string longString = new('X', 10000);
        db.Table<Book>().Add(new Book
        {
            Id = 1,
            Title = longString,
            AuthorId = 1,
            Price = 10
        });

        Book result = db.Table<Book>().First();
        Assert.Equal(longString, result.Title);
    }

    [Fact]
    public void QueryWithManyParameters()
    {
        using TestDatabase db = new();

        List<int> ids = Enumerable.Range(1, 100).ToList();

        SQLiteCommand command = (
            from book in db.Table<Book>()
            where ids.Contains(book.Id)
            select book
        ).ToSqlCommand();

        Assert.Equal(100, command.Parameters.Count);
    }

    [Fact]
    public void MultipleTablesCreation()
    {
        using TestDatabase db = new();

        db.Table<Book>().CreateTable();
        db.Table<Author>().CreateTable();
        db.Table<Publisher>().CreateTable();

        db.Table<Book>().CreateTable();
        db.Table<Author>().CreateTable();
        db.Table<Publisher>().CreateTable();

        Assert.True(true);
    }

    [Fact]
    public void DropNonExistentTable()
    {
        using TestDatabase db = new();

        db.Table<Book>().DropTable();

        Assert.True(true);
    }

    [Fact]
    public void QueryAfterDropTable()
    {
        using TestDatabase db = new();

        db.Table<Book>().CreateTable();
        db.Table<Book>().DropTable();

        Assert.Throws<SQLiteException>(() => db.Table<Book>().ToList());
    }

    [Fact]
    public void NegativeSkip()
    {
        using TestDatabase db = new();

        db.Table<Book>().CreateTable();
        db.Table<Book>().AddRange(new[]
        {
            new Book { Id = 1, Title = "Book 1", AuthorId = 1, Price = 10 },
            new Book { Id = 2, Title = "Book 2", AuthorId = 1, Price = 20 }
        });

        List<Book> results = db.Table<Book>().Skip(-1).ToList();

        Assert.Equal(2, results.Count);
    }

    [Fact]
    public void ZeroTake()
    {
        using TestDatabase db = new();

        db.Table<Book>().CreateTable();
        db.Table<Book>().Add(new Book
        {
            Id = 1,
            Title = "Test",
            AuthorId = 1,
            Price = 10
        });

        List<Book> results = db.Table<Book>().Take(0).ToList();

        Assert.Empty(results);
    }

    [Fact]
    public void NegativeTake()
    {
        using TestDatabase db = new();

        db.Table<Book>().CreateTable();
        db.Table<Book>().Add(new Book
        {
            Id = 1,
            Title = "Test",
            AuthorId = 1,
            Price = 10
        });

        List<Book> results = db.Table<Book>().Take(-1).ToList();

        Assert.Single(results);
    }

    [Fact]
    public void ExcessiveSkip()
    {
        using TestDatabase db = new();

        db.Table<Book>().CreateTable();
        db.Table<Book>().Add(new Book
        {
            Id = 1,
            Title = "Test",
            AuthorId = 1,
            Price = 10
        });

        List<Book> results = db.Table<Book>().Skip(1000).ToList();

        Assert.Empty(results);
    }

    [Fact]
    public void MultipleOrderBySameColumn()
    {
        using TestDatabase db = new();

        SQLiteCommand command = db.Table<Book>()
            .OrderBy(b => b.Price)
            .OrderBy(b => b.Price)
            .ThenBy(b => b.Id)
            .ToSqlCommand();

        Assert.Contains("ORDER BY", command.CommandText);
    }

    [Fact]
    public void EmptyUnion()
    {
        using TestDatabase db = new();

        db.Table<Book>().CreateTable();

        IQueryable<Book> query1 = db.Table<Book>().Where(b => b.Id == 1);
        IQueryable<Book> query2 = db.Table<Book>().Where(b => b.Id == 2);
        List<Book> results = query1.Union(query2).ToList();

        Assert.Empty(results);
    }

    [Fact]
    public void EmptyConcat()
    {
        using TestDatabase db = new();

        db.Table<Book>().CreateTable();

        IQueryable<Book> query1 = db.Table<Book>().Where(b => b.Id == 1);
        IQueryable<Book> query2 = db.Table<Book>().Where(b => b.Id == 2);
        List<Book> results = query1.Concat(query2).ToList();

        Assert.Empty(results);
    }

    [Fact]
    public void NestedGroupBy()
    {
        using TestDatabase db = new();

        db.Table<Book>().CreateTable();
        db.Table<Book>().AddRange(new[]
        {
            new Book { Id = 1, Title = "Book 1", AuthorId = 1, Price = 10 },
            new Book { Id = 2, Title = "Book 2", AuthorId = 1, Price = 20 },
            new Book { Id = 3, Title = "Book 3", AuthorId = 2, Price = 30 }
        });

        var results = (
            from book in db.Table<Book>()
            group book by book.AuthorId
            into g
            where g.Count() > 1
            select new
            {
                AuthorId = g.Key,
                Count = g.Count()
            }
        ).ToList();

        Assert.Single(results);
        Assert.Equal(1, results[0].AuthorId);
        Assert.Equal(2, results[0].Count);
    }

    [Fact]
    public void NullableComparison()
    {
        using TestDatabase db = new();

        db.Table<Author>().CreateTable();
        db.Table<Author>().Add(new Author
        {
            Id = 1,
            Name = "Test",
            Email = "test@test.com",
            BirthDate = DateTime.Now
        });

        int? nullableId = 1;
        Author? result = db.Table<Author>().Where(a => a.Id == nullableId).FirstOrDefault();

        Assert.NotNull(result);
    }

    [Fact]
    public void LargeResultSet()
    {
        using TestDatabase db = new();

        db.Table<Book>().CreateTable();

        Book[] books = Enumerable.Range(1, 1000).Select(i => new Book
        {
            Id = i,
            Title = $"Book {i}",
            AuthorId = i % 10,
            Price = i * 10
        }).ToArray();

        db.Table<Book>().AddRange(books);

        List<Book> results = db.Table<Book>().ToList();

        Assert.Equal(1000, results.Count);
    }

    [Fact]
    public void TransactionRollbackOnDispose()
    {
        using TestDatabase db = new();

        db.Table<Book>().CreateTable();

        {
            using SQLiteTransaction transaction = db.BeginTransaction();
            db.Table<Book>().Add(new Book
            {
                Id = 1,
                Title = "Test",
                AuthorId = 1,
                Price = 10
            });
        }

        int count = db.Table<Book>().Count();
        Assert.Equal(0, count);
    }

    [Fact]
    public void NestedTransactionsSupported()
    {
        using TestDatabase db = new();

        db.Table<Book>().CreateTable();

        using (SQLiteTransaction transaction1 = db.BeginTransaction())
        {
            db.Table<Book>().Add(new Book
            {
                Id = 1,
                Title = "Book 1",
                AuthorId = 1,
                Price = 10
            });

            using (SQLiteTransaction transaction2 = db.BeginTransaction())
            {
                db.Table<Book>().Add(new Book
                {
                    Id = 2,
                    Title = "Book 2",
                    AuthorId = 1,
                    Price = 20
                });

                transaction2.Commit();
            }

            transaction1.Commit();
        }

        int count = db.Table<Book>().Count();
        Assert.Equal(2, count);
    }

    private class EmptyKeyEntity
    {
        public int Id { get; set; }
        public string? Name { get; set; }
    }

    private class MultipleKeyEntity
    {
        [Key] public int Id1 { get; set; }
        [Key] public int Id2 { get; set; }
        public string? Name { get; set; }
    }

    private class NoPropertiesEntity
    {
    }
}