using System.Runtime.CompilerServices;
using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class ExternalResultTests
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

        Author author = (
            from a in db.Table<Author>()
            where a.Id == 1
            select new Author
            {
                Id = CommonHelpers.ConvertString(a.Name) - 1,
                Name = a.Name,
                Email = a.Email,
                BirthDate = a.BirthDate,
            }
        ).First();

        Assert.NotNull(author);
        Assert.Equal(-2, author.Id);
        Assert.Equal("Author 1", author.Name);
    }

    [Fact]
    public void CallExternalMethodWithPlus()
    {
        using TestDatabase db = SetupDatabase();

        Author author = (
            from a in db.Table<Author>()
            where a.Id == 1
            select new Author
            {
                Id = CommonHelpers.ConvertString(a.Name) + 1,
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

    private static TestDatabase SetupDatabase([CallerMemberName] string? methodName = null)
    {
        TestDatabase db = new(methodName);

        db.Table<Book>().CreateTable();
        db.Table<Author>().CreateTable();

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