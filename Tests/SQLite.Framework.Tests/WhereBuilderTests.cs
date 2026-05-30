using SQLite.Framework.Extensions;
using SQLite.Framework.Models;
using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class WhereBuilderTests
{
    private static TestDatabase MakeDb(string name)
    {
        TestDatabase db = new(name);
        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().AddRange([
            new Book { Id = 1, Title = "A", AuthorId = 1, Price = 5 },
            new Book { Id = 2, Title = "B", AuthorId = 1, Price = 15 },
            new Book { Id = 3, Title = "C", AuthorId = 2, Price = 25 },
            new Book { Id = 4, Title = "D", AuthorId = 3, Price = 35 }
        ]);
        return db;
    }

    [Fact]
    public void SinglePredicate_Equivalent_To_Where()
    {
        using TestDatabase db = MakeDb(nameof(SinglePredicate_Equivalent_To_Where));

        List<Book> result = db.Table<Book>()
            .WhereBuilder(f => f.And(b => b.Price > 10))
            .OrderBy(b => b.Id)
            .ToList();

        Assert.Equal([2, 3, 4], result.Select(b => b.Id));
    }

    [Fact]
    public void And_Chain_Combines_With_AND()
    {
        using TestDatabase db = MakeDb(nameof(And_Chain_Combines_With_AND));

        List<Book> result = db.Table<Book>()
            .WhereBuilder(f => f
                .And(b => b.Price > 10)
                .And(b => b.AuthorId == 1))
            .ToList();

        Assert.Single(result);
        Assert.Equal(2, result[0].Id);
    }

    [Fact]
    public void Or_Chain_Combines_With_OR()
    {
        using TestDatabase db = MakeDb(nameof(Or_Chain_Combines_With_OR));

        List<Book> result = db.Table<Book>()
            .WhereBuilder(f => f
                .Or(b => b.AuthorId == 1)
                .Or(b => b.AuthorId == 3))
            .OrderBy(b => b.Id)
            .ToList();

        Assert.Equal([1, 2, 4], result.Select(b => b.Id));
    }

    [Fact]
    public void Or_Then_And_Is_Left_To_Right()
    {
        using TestDatabase db = MakeDb(nameof(Or_Then_And_Is_Left_To_Right));

        List<Book> result = db.Table<Book>()
            .WhereBuilder(f => f
                .Or(b => b.AuthorId == 1)
                .Or(b => b.AuthorId == 2)
                .And(b => b.Price < 20))
            .OrderBy(b => b.Id)
            .ToList();

        Assert.Equal([1, 2], result.Select(b => b.Id));
    }

    [Fact]
    public void Nested_Group_With_AndGroup()
    {
        using TestDatabase db = MakeDb(nameof(Nested_Group_With_AndGroup));

        List<Book> result = db.Table<Book>()
            .WhereBuilder(f => f
                .And(b => b.Price > 0)
                .And(g => g
                    .Or(b => b.AuthorId == 1)
                    .Or(b => b.AuthorId == 3)))
            .OrderBy(b => b.Id)
            .ToList();

        Assert.Equal([1, 2, 4], result.Select(b => b.Id));
    }

    [Fact]
    public void Nested_Group_With_OrGroup()
    {
        using TestDatabase db = MakeDb(nameof(Nested_Group_With_OrGroup));

        List<Book> result = db.Table<Book>()
            .WhereBuilder(f => f
                .And(b => b.AuthorId == 1)
                .Or(g => g
                    .And(b => b.AuthorId == 3)
                    .And(b => b.Price > 30)))
            .OrderBy(b => b.Id)
            .ToList();

        Assert.Equal([1, 2, 4], result.Select(b => b.Id));
    }

    [Fact]
    public void First_Call_Is_AndGroup()
    {
        using TestDatabase db = MakeDb(nameof(First_Call_Is_AndGroup));

        List<Book> result = db.Table<Book>()
            .WhereBuilder(f => f
                .And(g => g
                    .Or(b => b.AuthorId == 1)
                    .Or(b => b.AuthorId == 3)))
            .OrderBy(b => b.Id)
            .ToList();

        Assert.Equal([1, 2, 4], result.Select(b => b.Id));
    }

    [Fact]
    public void First_Call_Is_OrGroup()
    {
        using TestDatabase db = MakeDb(nameof(First_Call_Is_OrGroup));

        List<Book> result = db.Table<Book>()
            .WhereBuilder(f => f
                .Or(g => g
                    .And(b => b.AuthorId == 1)
                    .And(b => b.Price > 10)))
            .OrderBy(b => b.Id)
            .ToList();

        Assert.Single(result);
        Assert.Equal(2, result[0].Id);
    }

    [Fact]
    public void Empty_Builder_Returns_All_Rows()
    {
        using TestDatabase db = MakeDb(nameof(Empty_Builder_Returns_All_Rows));

        List<Book> result = db.Table<Book>()
            .WhereBuilder(_ => { })
            .OrderBy(b => b.Id)
            .ToList();

        Assert.Equal(4, result.Count);
    }

    [Fact]
    public void Empty_Group_Is_NoOp()
    {
        using TestDatabase db = MakeDb(nameof(Empty_Group_Is_NoOp));

        List<Book> result = db.Table<Book>()
            .WhereBuilder(f => f
                .And(b => b.AuthorId == 1)
                .And(_ => { })
                .Or(_ => { }))
            .OrderBy(b => b.Id)
            .ToList();

        Assert.Equal([1, 2], result.Select(b => b.Id));
    }

    [Fact]
    public void Dynamic_Loop_Builds_Or_From_List()
    {
        using TestDatabase db = MakeDb(nameof(Dynamic_Loop_Builds_Or_From_List));

        int[] authorIds = [1, 3];
        List<Book> result = db.Table<Book>()
            .WhereBuilder(f =>
            {
                foreach (int id in authorIds)
                {
                    f.Or(b => b.AuthorId == id);
                }
            })
            .OrderBy(b => b.Id)
            .ToList();

        Assert.Equal([1, 2, 4], result.Select(b => b.Id));
    }

    [Fact]
    public void Composes_With_Other_Where_Calls()
    {
        using TestDatabase db = MakeDb(nameof(Composes_With_Other_Where_Calls));

        List<Book> result = db.Table<Book>()
            .Where(b => b.Price > 10)
            .WhereBuilder(f => f
                .Or(b => b.AuthorId == 1)
                .Or(b => b.AuthorId == 3))
            .OrderBy(b => b.Id)
            .ToList();

        Assert.Equal([2, 4], result.Select(b => b.Id));
    }

    [Fact]
    public void Null_Source_Throws()
    {
        IQueryable<Book> source = null!;
        Assert.Throws<ArgumentNullException>(() =>
            source.WhereBuilder(f => f.And(b => b.Id == 1)));
    }

    [Fact]
    public void Null_Build_Throws()
    {
        using TestDatabase db = MakeDb(nameof(Null_Build_Throws));
        Assert.Throws<ArgumentNullException>(() =>
            db.Table<Book>().WhereBuilder(null!));
    }

    [Fact]
    public void Null_Predicate_Throws()
    {
        using TestDatabase db = MakeDb(nameof(Null_Predicate_Throws));
        Assert.Throws<ArgumentNullException>(() =>
            db.Table<Book>().WhereBuilder(f => f.And((System.Linq.Expressions.Expression<Func<Book, bool>>)null!)));
        Assert.Throws<ArgumentNullException>(() =>
            db.Table<Book>().WhereBuilder(f => f.Or((System.Linq.Expressions.Expression<Func<Book, bool>>)null!)));
    }

    [Fact]
    public void Null_Group_Throws()
    {
        using TestDatabase db = MakeDb(nameof(Null_Group_Throws));
        Assert.Throws<ArgumentNullException>(() =>
            db.Table<Book>().WhereBuilder(f => f.And((Action<SQLiteWhereBuilder<Book>>)null!)));
        Assert.Throws<ArgumentNullException>(() =>
            db.Table<Book>().WhereBuilder(f => f.Or((Action<SQLiteWhereBuilder<Book>>)null!)));
    }

    private static TestDatabase MakeJoinedDb(string name)
    {
        TestDatabase db = new(name);
        db.Table<Author>().Schema.CreateTable();
        db.Table<Book>().Schema.CreateTable();
        db.Table<Author>().AddRange([
            new Author { Id = 1, Name = "Alice", Email = "a@x", BirthDate = new DateTime(1980, 1, 1) },
            new Author { Id = 2, Name = "Bob", Email = "b@x", BirthDate = new DateTime(1985, 1, 1) },
            new Author { Id = 3, Name = "Carol", Email = "c@x", BirthDate = new DateTime(1990, 1, 1) }
        ]);
        db.Table<Book>().AddRange([
            new Book { Id = 1, Title = "A", AuthorId = 1, Price = 5 },
            new Book { Id = 2, Title = "B", AuthorId = 1, Price = 15 },
            new Book { Id = 3, Title = "C", AuthorId = 2, Price = 25 },
            new Book { Id = 4, Title = "D", AuthorId = 3, Price = 35 }
        ]);
        return db;
    }

    private class Filter
    {
        public int? AuthorId { get; set; }
        public double? Price { get; set; }
    }

    [Fact]
    public void WhereBuilder_AfterJoinSelect_BothFiltersSet_GeneratesOrSql()
    {
        using TestDatabase db = MakeJoinedDb(nameof(WhereBuilder_AfterJoinSelect_BothFiltersSet_GeneratesOrSql));
        Filter filter = new() { AuthorId = 1, Price = 25 };

        IQueryable<BookAuthor> query = (
                from book in db.Table<Book>()
                join author in db.Table<Author>() on book.AuthorId equals author.Id
                select new BookAuthor { Book = book, Author = author })
            .WhereBuilder(f =>
            {
                if (filter.AuthorId != null) f.Or(x => x.Book.AuthorId == filter.AuthorId);
                if (filter.Price != null) f.Or(x => x.Book.Price == filter.Price);
            });

        SQLiteCommand cmd = query.ToSqlCommand();
        Assert.Equal("""
                     SELECT b0."BookId" AS "Book.Id",
                            b0."BookTitle" AS "Book.Title",
                            b0."BookAuthorId" AS "Book.AuthorId",
                            b0."BookPrice" AS "Book.Price",
                            a1."AuthorId" AS "Author.Id",
                            a1."AuthorName" AS "Author.Name",
                            a1."AuthorEmail" AS "Author.Email",
                            a1."AuthorBirthDate" AS "Author.BirthDate"
                     FROM "Books" AS b0
                     JOIN "Authors" AS a1 ON b0."BookAuthorId" = a1."AuthorId"
                     WHERE CAST(b0."BookAuthorId" AS INTEGER) = @p0 OR CAST(b0."BookPrice" AS REAL) = @p1
                     """.Replace("\r\n", "\n"),
            cmd.CommandText.Replace("\r\n", "\n"));
        Assert.DoesNotContain(" AND ", cmd.CommandText);

        List<int> ids = query.OrderBy(x => x.Book.Id).Select(x => x.Book.Id).ToList();
        Assert.Equal([1, 2, 3], ids);
    }

    [Fact]
    public void WhereBuilder_AfterJoinSelect_OneFilterSet_GeneratesSinglePredicate()
    {
        using TestDatabase db = MakeJoinedDb(nameof(WhereBuilder_AfterJoinSelect_OneFilterSet_GeneratesSinglePredicate));
        Filter filter = new() { AuthorId = 1, Price = null };

        IQueryable<BookAuthor> query = (
                from book in db.Table<Book>()
                join author in db.Table<Author>() on book.AuthorId equals author.Id
                select new BookAuthor { Book = book, Author = author })
            .WhereBuilder(f =>
            {
                if (filter.AuthorId != null) f.Or(x => x.Book.AuthorId == filter.AuthorId);
                if (filter.Price != null) f.Or(x => x.Book.Price == filter.Price);
            });

        SQLiteCommand cmd = query.ToSqlCommand();
        Assert.Equal("""
                     SELECT b0."BookId" AS "Book.Id",
                            b0."BookTitle" AS "Book.Title",
                            b0."BookAuthorId" AS "Book.AuthorId",
                            b0."BookPrice" AS "Book.Price",
                            a1."AuthorId" AS "Author.Id",
                            a1."AuthorName" AS "Author.Name",
                            a1."AuthorEmail" AS "Author.Email",
                            a1."AuthorBirthDate" AS "Author.BirthDate"
                     FROM "Books" AS b0
                     JOIN "Authors" AS a1 ON b0."BookAuthorId" = a1."AuthorId"
                     WHERE CAST(b0."BookAuthorId" AS INTEGER) = @p0
                     """.Replace("\r\n", "\n"),
            cmd.CommandText.Replace("\r\n", "\n"));
        Assert.DoesNotContain(" OR ", cmd.CommandText);
        Assert.DoesNotContain(" AND ", cmd.CommandText);

        List<int> ids = query.OrderBy(x => x.Book.Id).Select(x => x.Book.Id).ToList();
        Assert.Equal([1, 2], ids);
    }

    [Fact]
    public void WhereBuilder_AfterJoinSelect_NoFilters_GeneratesNoWhere()
    {
        using TestDatabase db = MakeJoinedDb(nameof(WhereBuilder_AfterJoinSelect_NoFilters_GeneratesNoWhere));
        Filter filter = new() { AuthorId = null, Price = null };

        IQueryable<BookAuthor> query = (
                from book in db.Table<Book>()
                join author in db.Table<Author>() on book.AuthorId equals author.Id
                select new BookAuthor { Book = book, Author = author })
            .WhereBuilder(f =>
            {
                if (filter.AuthorId != null) f.Or(x => x.Book.AuthorId == filter.AuthorId);
                if (filter.Price != null) f.Or(x => x.Book.Price == filter.Price);
            });

        SQLiteCommand cmd = query.ToSqlCommand();
        Assert.DoesNotContain("WHERE", cmd.CommandText);

        List<int> ids = query.OrderBy(x => x.Book.Id).Select(x => x.Book.Id).ToList();
        Assert.Equal([1, 2, 3, 4], ids);
    }

    [Fact]
    public void ChainedWhere_AfterJoinSelect_BothFiltersSet_GeneratesAndSql()
    {
        using TestDatabase db = MakeJoinedDb(nameof(ChainedWhere_AfterJoinSelect_BothFiltersSet_GeneratesAndSql));
        Filter filter = new() { AuthorId = 1, Price = 15 };

        IQueryable<BookAuthor> query =
            from book in db.Table<Book>()
            join author in db.Table<Author>() on book.AuthorId equals author.Id
            select new BookAuthor { Book = book, Author = author };

        if (filter.AuthorId != null) query = query.Where(f => f.Book.AuthorId == filter.AuthorId);
        if (filter.Price != null) query = query.Where(f => f.Book.Price == filter.Price);

        SQLiteCommand cmd = query.ToSqlCommand();
        Assert.Equal("""
                     SELECT b0."BookId" AS "Book.Id",
                            b0."BookTitle" AS "Book.Title",
                            b0."BookAuthorId" AS "Book.AuthorId",
                            b0."BookPrice" AS "Book.Price",
                            a1."AuthorId" AS "Author.Id",
                            a1."AuthorName" AS "Author.Name",
                            a1."AuthorEmail" AS "Author.Email",
                            a1."AuthorBirthDate" AS "Author.BirthDate"
                     FROM "Books" AS b0
                     JOIN "Authors" AS a1 ON b0."BookAuthorId" = a1."AuthorId"
                     WHERE CAST(b0."BookAuthorId" AS INTEGER) = @p0 AND CAST(b0."BookPrice" AS REAL) = @p1
                     """.Replace("\r\n", "\n"),
            cmd.CommandText.Replace("\r\n", "\n"));
        Assert.DoesNotContain(" OR ", cmd.CommandText);

        List<int> ids = query.OrderBy(x => x.Book.Id).Select(x => x.Book.Id).ToList();
        Assert.Equal([2], ids);
    }

    [Fact]
    public void ChainedWhere_AfterJoinSelect_OneFilterSet_GeneratesSinglePredicate()
    {
        using TestDatabase db = MakeJoinedDb(nameof(ChainedWhere_AfterJoinSelect_OneFilterSet_GeneratesSinglePredicate));
        Filter filter = new() { AuthorId = null, Price = 25 };

        IQueryable<BookAuthor> query =
            from book in db.Table<Book>()
            join author in db.Table<Author>() on book.AuthorId equals author.Id
            select new BookAuthor { Book = book, Author = author };

        if (filter.AuthorId != null) query = query.Where(f => f.Book.AuthorId == filter.AuthorId);
        if (filter.Price != null) query = query.Where(f => f.Book.Price == filter.Price);

        SQLiteCommand cmd = query.ToSqlCommand();
        Assert.Equal("""
                     SELECT b0."BookId" AS "Book.Id",
                            b0."BookTitle" AS "Book.Title",
                            b0."BookAuthorId" AS "Book.AuthorId",
                            b0."BookPrice" AS "Book.Price",
                            a1."AuthorId" AS "Author.Id",
                            a1."AuthorName" AS "Author.Name",
                            a1."AuthorEmail" AS "Author.Email",
                            a1."AuthorBirthDate" AS "Author.BirthDate"
                     FROM "Books" AS b0
                     JOIN "Authors" AS a1 ON b0."BookAuthorId" = a1."AuthorId"
                     WHERE CAST(b0."BookPrice" AS REAL) = @p0
                     """.Replace("\r\n", "\n"),
            cmd.CommandText.Replace("\r\n", "\n"));
        Assert.DoesNotContain(" AND ", cmd.CommandText);

        List<int> ids = query.OrderBy(x => x.Book.Id).Select(x => x.Book.Id).ToList();
        Assert.Equal([3], ids);
    }
}

internal class BookAuthor
{
    public required Book Book { get; set; }
    public required Author Author { get; set; }
}
