using System.Runtime.CompilerServices;
using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class ExternalMethodTests
{
    public class SpecialModel
    {
        public Author Author { get; } = new Author()
        {
            Id = 1,
            Name = "Author 1",
            Email = "asd",
            BirthDate = new DateTime(2000, 1, 1)
        };
    }

    [Fact]
    public void CallExternalMethodWithDoubleSelect()
    {
        using TestDatabase db = SetupDatabase();

        SQLiteCommand command = (
            from a in db.Table<Author>()
            where a.Id == 1
            select new Author
            {
                Id = CommonHelpers.ConvertString(a.Name) - 1,
                Name = a.Name,
                Email = a.Email,
                BirthDate = a.BirthDate,
            }
        ).Select(f => f.Id).ToSqlCommand();

        Assert.Single(command.Parameters);
        Assert.Equal(1, command.Parameters[0].Value);

        int id = (
            from a in db.Table<Author>()
            where a.Id == 1
            select new Author
            {
                Id = CommonHelpers.ConvertString(a.Name) - 1,
                Name = a.Name,
                Email = a.Email,
                BirthDate = a.BirthDate,
            }
        ).Select(f => f.Id).First();

        Assert.Equal(-2, id);
    }

    [Fact]
    public void CallExternalMethodWithMinus()
    {
        using TestDatabase db = SetupDatabase();

        IQueryable<Author> query =
            from a in db.Table<Author>()
            where a.Id == 1
            select new Author
            {
                Id = CommonHelpers.ConvertString(a.Name) - 1,
                Name = a.Name,
                Email = a.Email,
                BirthDate = a.BirthDate,
            }
        ;

        SQLiteCommand command = query.ToSqlCommand();

        Assert.Single(command.Parameters);
        Assert.Equal(1, command.Parameters[0].Value);

        Author author = query.First();

        Assert.NotNull(author);
        Assert.Equal(-2, author.Id);
        Assert.Equal("Author 1", author.Name);
    }

    [Fact]
    public void CallExternalMethodWithPlus()
    {
        using TestDatabase db = SetupDatabase();

        IQueryable<Author> query =
            from a in db.Table<Author>()
            where a.Id == 1
            select new Author
            {
                Id = CommonHelpers.ConvertString(a.Name) + 1,
                Name = a.Name,
                Email = a.Email,
                BirthDate = a.BirthDate,
            }
        ;

        SQLiteCommand command = query.ToSqlCommand();

        Assert.Single(command.Parameters);
        Assert.Equal(1, command.Parameters[0].Value);

        Author author = query.First();

        Assert.NotNull(author);
        Assert.Equal(0, author.Id);
        Assert.Equal("Author 1", author.Name);
    }

    [Fact]
    public void CallExternalMethodWithMultiply()
    {
        using TestDatabase db = SetupDatabase();

        Author author = (
            from a in db.Table<Author>()
            where a.Id == 1
            select new Author
            {
                Id = CommonHelpers.ConvertString(a.Name) * 10,
                Name = a.Name,
                Email = a.Email,
                BirthDate = a.BirthDate,
            }
        ).First();

        Assert.NotNull(author);
        Assert.Equal(-10, author.Id);
        Assert.Equal("Author 1", author.Name);
    }

    [Fact]
    public void CallExternalMethodWithDivide()
    {
        using TestDatabase db = SetupDatabase();

        Author author = (
            from a in db.Table<Author>()
            where a.Id == 1
            select new Author
            {
                Id = CommonHelpers.ConvertString(a.Name) / 10,
                Name = a.Name,
                Email = a.Email,
                BirthDate = a.BirthDate,
            }
        ).First();

        Assert.NotNull(author);
        Assert.Equal(0, author.Id);
        Assert.Equal("Author 1", author.Name);
    }

    [Fact]
    public void CallExternalMethodWithModulo()
    {
        using TestDatabase db = SetupDatabase();

        Author author = (
            from a in db.Table<Author>()
            where a.Id == 1
            select new Author
            {
                Id = CommonHelpers.ConvertString(a.Name) % 10,
                Name = a.Name,
                Email = a.Email,
                BirthDate = a.BirthDate,
            }
        ).First();

        Assert.NotNull(author);
        Assert.Equal(-1, author.Id);
        Assert.Equal("Author 1", author.Name);
    }

    [Fact]
    public void CallExternalMethodWithEqual()
    {
        using TestDatabase db = SetupDatabase();

        Author author = (
            from a in db.Table<Author>()
            where a.Id == 1
            select new Author
            {
                Id = CommonHelpers.ConvertString(a.Name) == 1 ? 1 : 0,
                Name = a.Name,
                Email = a.Email,
                BirthDate = a.BirthDate,
            }
        ).First();

        Assert.NotNull(author);
        Assert.Equal(0, author.Id);
        Assert.Equal("Author 1", author.Name);
    }

    [Fact]
    public void CallExternalMethodWithNotEqual()
    {
        using TestDatabase db = SetupDatabase();

        Author author = (
            from a in db.Table<Author>()
            where a.Id == 1
            select new Author
            {
                Id = CommonHelpers.ConvertString(a.Name) != 1 ? 1 : 0,
                Name = a.Name,
                Email = a.Email,
                BirthDate = a.BirthDate,
            }
        ).First();

        Assert.NotNull(author);
        Assert.Equal(1, author.Id);
        Assert.Equal("Author 1", author.Name);
    }

    [Fact]
    public void CallExternalMethodWithNotGreaterThan()
    {
        using TestDatabase db = SetupDatabase();

        Author author = (
            from a in db.Table<Author>()
            where a.Id == 1
            select new Author
            {
                Id = CommonHelpers.ConvertString(a.Name) > 1 ? 1 : 0,
                Name = a.Name,
                Email = a.Email,
                BirthDate = a.BirthDate,
            }
        ).First();

        Assert.NotNull(author);
        Assert.Equal(0, author.Id);
        Assert.Equal("Author 1", author.Name);
    }

    [Fact]
    public void CallExternalMethodWithNotLessThan()
    {
        using TestDatabase db = SetupDatabase();

        Author author = (
            from a in db.Table<Author>()
            where a.Id == 1
            select new Author
            {
                Id = CommonHelpers.ConvertString(a.Name) < 1 ? 1 : 0,
                Name = a.Name,
                Email = a.Email,
                BirthDate = a.BirthDate,
            }
        ).First();

        Assert.NotNull(author);
        Assert.Equal(1, author.Id);
        Assert.Equal("Author 1", author.Name);
    }

    [Fact]
    public void CallExternalMethodWithIndex()
    {
        using TestDatabase db = SetupDatabase();

        Author author = (
            from a in db.Table<Author>()
            where a.Id == 1
            select new Author
            {
                Id = CommonHelpers.GetArray()[0] + CommonHelpers.ConvertString(a.Name),
                Name = a.Name,
                Email = a.Email,
                BirthDate = a.BirthDate,
            }
        ).First();

        Assert.NotNull(author);
        Assert.Equal(0, author.Id);
        Assert.Equal("Author 1", author.Name);
    }

    [Fact]
    public void CallExternalMethodWithMember()
    {
        using TestDatabase db = SetupDatabase();

        int value = CommonHelpers.GetValue();

        Author author = (
            from a in db.Table<Author>()
            where a.Id == 1
            select new Author
            {
                Id = value + CommonHelpers.ConvertString(a.Name),
                Name = a.Name,
                Email = a.Email,
                BirthDate = a.BirthDate,
            }
        ).First();

        Assert.NotNull(author);
        Assert.Equal(0, author.Id);
        Assert.Equal("Author 1", author.Name);
    }

    [Fact]
    public void CallExternalMethodWithCast()
    {
        using TestDatabase db = SetupDatabase();

        Author author = (
            from a in db.Table<Author>()
            where a.Id == 1
            select new Author
            {
                Id = (int)CommonHelpers.ConvertStringLong(a.Name),
                Name = a.Name,
                Email = a.Email,
                BirthDate = a.BirthDate,
            }
        ).First();

        Assert.NotNull(author);
        Assert.Equal(-1, author.Id);
        Assert.Equal("Author 1", author.Name);
    }

    [Fact]
    public void ListInit()
    {
        using TestDatabase db = SetupDatabase();

        Author author = (
            from a in db.Table<Author>()
            where a.Id == 1
            select new Author
            {
                Id = new List<int> { 1, 2, 3 }[0],
                Name = a.Name,
                Email = a.Email,
                BirthDate = a.BirthDate,
            }
        ).First();

        Assert.NotNull(author);
        Assert.Equal(1, author.Id);
        Assert.Equal("Author 1", author.Name);
    }

    [Fact]
    public void ListInit_WithCapturedIndex()
    {
        using TestDatabase db = SetupDatabase();

        int index = 2;

        Author author = (
            from a in db.Table<Author>()
            where a.Id == 1
            select new Author
            {
                Id = new List<int> { 10, 20, 30 }[index],
                Name = a.Name,
                Email = a.Email,
                BirthDate = a.BirthDate,
            }
        ).First();

        Assert.NotNull(author);
        Assert.Equal(30, author.Id);
        Assert.Equal("Author 1", author.Name);
    }

    [Fact]
    public void ArrayInit()
    {
        using TestDatabase db = SetupDatabase();

        Author author = (
            from a in db.Table<Author>()
            where a.Id == 1
            select new Author
            {
                Id = new[] { 1, 2, 3 }[0],
                Name = a.Name,
                Email = a.Email,
                BirthDate = a.BirthDate,
            }
        ).First();

        Assert.NotNull(author);
        Assert.Equal(1, author.Id);
        Assert.Equal("Author 1", author.Name);
    }

    [Fact]
    public void NegationOfExternalMethod()
    {
        using TestDatabase db = SetupDatabase();

        int result = db.Table<Author>()
            .Where(a => a.Id == 1)
            .Select(a => -CommonHelpers.ConvertString(a.Name))
            .First();

        Assert.Equal(1, result);
    }

    [Fact]
    public void CallExternalDictionaryGetValueOrDefault()
    {
        using TestDatabase db = SetupDatabase();

        Dictionary<int, string> authorNames = new()
        {
            { 1, "Author 1" },
            { 2, "Author 2" },
        };

        List<(int BookId, string AuthorName)> results = (
            from b in db.Table<Book>()
            orderby b.Id
            select new
            {
                BookId = b.Id,
                AuthorName = authorNames.GetValueOrDefault(b.AuthorId, "Unknown"),
            }
        ).AsEnumerable().Select(x => (x.BookId, x.AuthorName)).ToList();

        Assert.Equal(2, results.Count);
        Assert.Equal((1, "Author 1"), results[0]);
        Assert.Equal((2, "Author 1"), results[1]);
    }

    [Fact]
    public void ExternalModelMembers()
    {
        using TestDatabase db = SetupDatabase();

        Author author = (
            from a in db.Table<Author>()
            where a.Id == 1
            select new Author
            {
                Id = new SpecialModel
                {
                    Author =
                    {
                        Id = a.Id,
                    },
                }.Author.Id,
                Name = a.Name,
                Email = a.Email,
                BirthDate = a.BirthDate,
            }
        ).First();

        Assert.NotNull(author);
        Assert.Equal(1, author.Id);
        Assert.Equal("Author 1", author.Name);
    }

    private static string FormatBookTitle(Book book)
    {
        return $"[{book.Id}] {book.Title}";
    }

    private static string FormatAuthorName(Author author)
    {
        return $"{author.Name} <{author.Email}>";
    }

    private static string FormatBookByAuthor(Book book, Author author)
    {
        return $"{book.Title} by {author.Name}";
    }

    [Fact]
    public void Select_MethodCallOnRow_ToList()
    {
        using TestDatabase db = SetupDatabase();

        List<string> titles = db.Table<Book>()
            .OrderBy(b => b.Id)
            .Select(b => FormatBookTitle(b))
            .ToList();

        Assert.Equal(new[] { "[1] Book 1", "[2] Book 2" }, titles);
    }

    [Fact]
    public void Select_MethodCallOnJoinVariable_ToList()
    {
        using TestDatabase db = SetupDatabase();

        List<string> names = (
            from book in db.Table<Book>()
            join author in db.Table<Author>() on book.AuthorId equals author.Id
            orderby book.Id
            select FormatAuthorName(author)
        ).ToList();

        Assert.Equal(new[] { "Author 1 <author@mail.com>", "Author 1 <author@mail.com>" }, names);
    }

    [Fact]
    public void Select_MethodCallOnJoinVariables_ToList()
    {
        using TestDatabase db = SetupDatabase();

        List<string> lines = (
            from book in db.Table<Book>()
            join author in db.Table<Author>() on book.AuthorId equals author.Id
            orderby book.Id
            select FormatBookByAuthor(book, author)
        ).ToList();

        Assert.Equal(new[] { "Book 1 by Author 1", "Book 2 by Author 1" }, lines);
    }

    private static TestDatabase SetupDatabase([CallerMemberName] string? methodName = null)
    {
        TestDatabase db = new(methodName);

        db.Table<Book>().Schema.CreateTable();
        db.Table<Author>().Schema.CreateTable();

        db.Table<Book>().AddRange(new[]
        {
            new Book
            {
                Id = 1,
                Title = "Book 1",
                AuthorId = 1,
                Price = 5
            },
            new Book
            {
                Id = 2,
                Title = "Book 2",
                AuthorId = 1,
                Price = 10
            }
        });

        db.Table<Author>().AddRange(new[]
        {
            new Author
            {
                Id = 1,
                Name = "Author 1",
                Email = "author@mail.com",
                BirthDate = new DateTime(2000, 1, 1)
            }
        });

        return db;
    }
}