using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class AttachedDatabaseTypedQueryTests
{
    [Fact]
    public void ExplicitSchemaReadsAttachedRowsNotMain()
    {
        using TestDatabase main = new();
        main.Table<Book>().Schema.CreateTable();
        main.Table<Book>().Add(new Book { Id = 1, Title = "main-book", AuthorId = 1, Price = 1 });

        string auxPath = TempPath();
        try
        {
            SeedAux(auxPath, new Book { Id = 2, Title = "aux-book", AuthorId = 2, Price = 2 });
            main.AttachDatabase(auxPath, "aux", AuxKey);

            List<string> fromAux = main.Table<Book>("aux").Select(b => b.Title).ToList();
            List<string> fromMain = main.Table<Book>().Select(b => b.Title).ToList();

            Assert.Equal(["aux-book"], fromAux);
            Assert.Equal(["main-book"], fromMain);
        }
        finally
        {
            Delete(auxPath);
        }
    }

    [Fact]
    public void ExplicitSchemaJoinsAcrossDatabases()
    {
        Author[] authors =
        [
            new() { Id = 1, Name = "Alice", Email = "a@x", BirthDate = new DateTime(1990, 1, 1) },
            new() { Id = 2, Name = "Bob", Email = "b@x", BirthDate = new DateTime(1991, 2, 2) },
        ];
        Book[] books =
        [
            new() { Id = 1, Title = "A1", AuthorId = 1, Price = 1 },
            new() { Id = 2, Title = "B1", AuthorId = 2, Price = 2 },
        ];

        using TestDatabase main = new();
        main.Table<Author>().Schema.CreateTable();
        main.Table<Author>().AddRange(authors);

        string auxPath = TempPath();
        try
        {
            SeedAux(auxPath, books);
            main.AttachDatabase(auxPath, "aux", AuxKey);

            List<(string Name, string Title)> actual = (
                from a in main.Table<Author>()
                join b in main.Table<Book>("aux") on a.Id equals b.AuthorId
                orderby a.Name
                select new ValueTuple<string, string>(a.Name, b.Title)
            ).ToList();

            List<(string Name, string Title)> expected = (
                from a in authors
                join b in books on a.Id equals b.AuthorId
                orderby a.Name
                select (a.Name, b.Title)
            ).ToList();

            Assert.Equal([("Alice", "A1"), ("Bob", "B1")], expected);
            Assert.Equal(expected, actual);
        }
        finally
        {
            Delete(auxPath);
        }
    }

    [Fact]
    public void AutoDetectResolvesSchemaFromDatabaseObject()
    {
        Author[] authors =
        [
            new() { Id = 1, Name = "Alice", Email = "a@x", BirthDate = new DateTime(1990, 1, 1) },
        ];
        Book[] books =
        [
            new() { Id = 1, Title = "aux-only", AuthorId = 1, Price = 5 },
        ];

        using TestDatabase main = new();
        main.Table<Author>().Schema.CreateTable();
        main.Table<Author>().AddRange(authors);

        string auxPath = TempPath();
        try
        {
            using SQLiteDatabase aux = OpenAux(auxPath);
            aux.Table<Book>().Schema.CreateTable();
            aux.Table<Book>().AddRange(books);

            main.AttachDatabase(aux, "aux");

            List<string> actual = (
                from a in main.Table<Author>()
                join b in aux.Table<Book>() on a.Id equals b.AuthorId
                select b.Title
            ).ToList();

            List<string> expected = (
                from a in authors
                join b in books on a.Id equals b.AuthorId
                select b.Title
            ).ToList();

            Assert.Equal(["aux-only"], expected);
            Assert.Equal(expected, actual);
        }
        finally
        {
            Delete(auxPath);
        }
    }

    [Fact]
    public void AutoDetectDistinguishesMultipleAttachedDatabases()
    {
        using TestDatabase main = new();
        main.Table<Author>().Schema.CreateTable();
        main.Table<Author>().Add(new Author { Id = 1, Name = "Alice", Email = "a@x", BirthDate = new DateTime(1990, 1, 1) });

        string path1 = TempPath();
        string path2 = TempPath();
        try
        {
            using SQLiteDatabase aux1 = OpenAux(path1);
            aux1.Table<Book>().Schema.CreateTable();
            aux1.Table<Book>().Add(new Book { Id = 1, Title = "in-aux1", AuthorId = 1, Price = 1 });

            using SQLiteDatabase aux2 = OpenAux(path2);
            aux2.Table<Book>().Schema.CreateTable();
            aux2.Table<Book>().Add(new Book { Id = 1, Title = "in-aux2", AuthorId = 1, Price = 1 });

            main.AttachDatabase(aux1, "aux1");
            main.AttachDatabase(aux2, "aux2");

            List<string> from1 = (
                from a in main.Table<Author>()
                join b in aux1.Table<Book>() on a.Id equals b.AuthorId
                select b.Title
            ).ToList();
            List<string> from2 = (
                from a in main.Table<Author>()
                join b in aux2.Table<Book>() on a.Id equals b.AuthorId
                select b.Title
            ).ToList();

            Assert.Equal(["in-aux1"], from1);
            Assert.Equal(["in-aux2"], from2);
        }
        finally
        {
            Delete(path1);
            Delete(path2);
        }
    }

    [Fact]
    public void DetachClearsAutoDetectRegistryAndLeavesOthers()
    {
        using TestDatabase main = new();
        main.Table<Book>().Schema.CreateTable();
        main.Table<Book>().Add(new Book { Id = 10, Title = "main-title", AuthorId = 1, Price = 1 });
        main.Table<Author>().Schema.CreateTable();
        main.Table<Author>().Add(new Author { Id = 1, Name = "Alice", Email = "a@x", BirthDate = new DateTime(1990, 1, 1) });

        string path1 = TempPath();
        string path2 = TempPath();
        try
        {
            using SQLiteDatabase aux1 = OpenAux(path1);
            aux1.Table<Book>().Schema.CreateTable();
            aux1.Table<Book>().Add(new Book { Id = 10, Title = "aux1-title", AuthorId = 1, Price = 1 });

            using SQLiteDatabase aux2 = OpenAux(path2);
            aux2.Table<Book>().Schema.CreateTable();
            aux2.Table<Book>().Add(new Book { Id = 20, Title = "aux2-title", AuthorId = 1, Price = 1 });

            main.AttachDatabase(aux1, "aux1");
            main.AttachDatabase(aux2, "aux2");

            List<string> beforeDetach = (
                from a in main.Table<Author>()
                join b in aux1.Table<Book>() on a.Id equals b.AuthorId
                select b.Title
            ).ToList();
            Assert.Equal(["aux1-title"], beforeDetach);

            main.DetachDatabase("aux1");

            List<string> afterDetach = (
                from a in main.Table<Author>()
                join b in aux1.Table<Book>() on a.Id equals b.AuthorId
                select b.Title
            ).ToList();
            Assert.Equal(["main-title"], afterDetach);

            List<string> aux2Still = (
                from a in main.Table<Author>()
                join b in aux2.Table<Book>() on a.Id equals b.AuthorId
                select b.Title
            ).ToList();
            Assert.Equal(["aux2-title"], aux2Still);
        }
        finally
        {
            Delete(path1);
            Delete(path2);
        }
    }

    [Fact]
    public void AnySubqueryUsesAttachedSchema()
    {
        using TestDatabase main = new();
        main.Table<Author>().Schema.CreateTable();
        main.Table<Author>().AddRange([
            new Author { Id = 1, Name = "Alice", Email = "a@x", BirthDate = new DateTime(1990, 1, 1) },
            new Author { Id = 2, Name = "Bob", Email = "b@x", BirthDate = new DateTime(1991, 2, 2) }
        ]);
        main.Table<Book>().Schema.CreateTable();
        main.Table<Book>().Add(new Book { Id = 1, Title = "main", AuthorId = 2, Price = 1 });

        string auxPath = TempPath();
        try
        {
            SeedAux(auxPath, new Book { Id = 1, Title = "aux", AuthorId = 1, Price = 1 });
            main.AttachDatabase(auxPath, "aux", AuxKey);

            List<string> actual = main.Table<Author>()
                .Where(a => main.Table<Book>("aux").Any(b => b.AuthorId == a.Id))
                .Select(a => a.Name)
                .ToList();

            Assert.Equal(["Alice"], actual);
        }
        finally
        {
            Delete(auxPath);
        }
    }

    [Fact]
    public void ContainsSubqueryUsesAttachedSchema()
    {
        using TestDatabase main = new();
        main.Table<Author>().Schema.CreateTable();
        main.Table<Author>().AddRange([
            new Author { Id = 1, Name = "Alice", Email = "a@x", BirthDate = new DateTime(1990, 1, 1) },
            new Author { Id = 2, Name = "Bob", Email = "b@x", BirthDate = new DateTime(1991, 2, 2) }
        ]);
        main.Table<Book>().Schema.CreateTable();
        main.Table<Book>().Add(new Book { Id = 1, Title = "main", AuthorId = 2, Price = 1 });

        string auxPath = TempPath();
        try
        {
            SeedAux(auxPath, new Book { Id = 1, Title = "aux", AuthorId = 1, Price = 1 });
            main.AttachDatabase(auxPath, "aux", AuxKey);

            List<string> actual = main.Table<Author>()
                .Where(a => main.Table<Book>("aux").Select(b => b.AuthorId).Contains(a.Id))
                .Select(a => a.Name)
                .ToList();

            Assert.Equal(["Alice"], actual);
        }
        finally
        {
            Delete(auxPath);
        }
    }

    [Fact]
    public void SelectManyCrossJoinUsesAttachedSchema()
    {
        using TestDatabase main = new();
        main.Table<Author>().Schema.CreateTable();
        main.Table<Author>().Add(new Author { Id = 1, Name = "Alice", Email = "a@x", BirthDate = new DateTime(1990, 1, 1) });
        main.Table<Book>().Schema.CreateTable();
        main.Table<Book>().Add(new Book { Id = 1, Title = "main", AuthorId = 1, Price = 1 });

        string auxPath = TempPath();
        try
        {
            SeedAux(auxPath, new Book { Id = 1, Title = "aux", AuthorId = 1, Price = 1 });
            main.AttachDatabase(auxPath, "aux", AuxKey);

            List<string> actual = (
                from a in main.Table<Author>()
                from b in main.Table<Book>("aux")
                where b.AuthorId == a.Id
                select b.Title
            ).ToList();

            Assert.Equal(["aux"], actual);
        }
        finally
        {
            Delete(auxPath);
        }
    }

    [Fact]
    public void ConcatAcrossAttachedSchemas()
    {
        using TestDatabase main = new();

        string path1 = TempPath();
        string path2 = TempPath();
        try
        {
            SeedAux(path1, new Book { Id = 1, Title = "from-1", AuthorId = 1, Price = 1 });
            SeedAux(path2, new Book { Id = 1, Title = "from-2", AuthorId = 1, Price = 1 });
            main.AttachDatabase(path1, "aux1", AuxKey);
            main.AttachDatabase(path2, "aux2", AuxKey);

            List<string> actual = main.Table<Book>("aux1").Select(b => b.Title)
                .Concat(main.Table<Book>("aux2").Select(b => b.Title))
                .OrderBy(t => t)
                .ToList();

            Assert.Equal(["from-1", "from-2"], actual);
        }
        finally
        {
            Delete(path1);
            Delete(path2);
        }
    }

    [Fact]
    public void TableWithInvalidSchemaNameThrows()
    {
        using TestDatabase main = new();

        Assert.Throws<ArgumentException>(() => main.Table<Book>("aux name"));
        Assert.Throws<ArgumentException>(() => main.Table<Book>(""));
    }

    [Fact]
    public void AttachDatabaseWithNullDatabaseThrows()
    {
        using TestDatabase main = new();

        Assert.Throws<ArgumentNullException>(() => main.AttachDatabase((SQLiteDatabase)null!, "aux"));
    }

    [Fact]
    public async Task AttachDatabaseAsyncWithDatabaseObjectRoundTrips()
    {
        using TestDatabase main = new();
        main.Table<Author>().Schema.CreateTable();
        main.Table<Author>().Add(new Author { Id = 1, Name = "Alice", Email = "a@x", BirthDate = new DateTime(1990, 1, 1) });

        string auxPath = TempPath();
        try
        {
            using SQLiteDatabase aux = OpenAux(auxPath);
            aux.Table<Book>().Schema.CreateTable();
            aux.Table<Book>().Add(new Book { Id = 1, Title = "async-aux", AuthorId = 1, Price = 1 });

            await main.AttachDatabaseAsync(aux, "aux", TestContext.Current.CancellationToken);

            List<string> actual = (
                from a in main.Table<Author>()
                join b in aux.Table<Book>() on a.Id equals b.AuthorId
                select b.Title
            ).ToList();

            Assert.Equal(["async-aux"], actual);

            await main.DetachDatabaseAsync("aux", TestContext.Current.CancellationToken);
        }
        finally
        {
            Delete(auxPath);
        }
    }

    private static string TempPath()
    {
        return Path.Combine(Path.GetTempPath(), $"attached_{Guid.NewGuid():N}.db3");
    }

    private static void Delete(string path)
    {
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }

    private static void SeedAux(string path, params Book[] books)
    {
        using SQLiteDatabase aux = OpenAux(path);
        aux.Table<Book>().Schema.CreateTable();
        aux.Table<Book>().AddRange(books);
    }

    private static SQLiteDatabase OpenAux(string path)
    {
        SQLiteOptionsBuilder builder = new(path);
#if SQLITECIPHER
        builder.UseEncryptionKey("test-key");
#endif
        return new SQLiteDatabase(builder.Build());
    }

    private static string? AuxKey =>
#if SQLITECIPHER
        "test-key";
#else
        null;
#endif
}
