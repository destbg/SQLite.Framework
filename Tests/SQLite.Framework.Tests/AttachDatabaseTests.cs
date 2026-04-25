using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class AttachDatabaseTests
{
    [Fact]
    public void AttachDatabase_QueryAcrossFiles_ReturnsRowsFromAttached()
    {
        using TestDatabase main = new();
        main.Schema.CreateTable<Book>();

        string auxPath = Path.Combine(Path.GetTempPath(), $"aux_{Guid.NewGuid():N}.db3");
        try
        {
            using (SQLiteDatabase auxDb = OpenAux(auxPath))
            {
                auxDb.Schema.CreateTable<Book>();
                auxDb.Table<Book>().Add(new Book { Id = 1, Title = "from-aux", AuthorId = 1, Price = 1 });
            }

            main.AttachDatabase(auxPath, "aux", AuxEncryptionKey);

            List<Book> rows = main.Query<Book>("SELECT BookId AS Id, BookTitle AS Title, BookAuthorId AS AuthorId, BookPrice AS Price FROM aux.Books");
            Assert.Single(rows);
            Assert.Equal("from-aux", rows[0].Title);
        }
        finally
        {
            if (File.Exists(auxPath))
            {
                File.Delete(auxPath);
            }
        }
    }

    [Fact]
    public void DetachDatabase_RemovesAccess()
    {
        using TestDatabase main = new();

        string auxPath = Path.Combine(Path.GetTempPath(), $"aux_{Guid.NewGuid():N}.db3");
        try
        {
            using (SQLiteDatabase auxDb = OpenAux(auxPath))
            {
                auxDb.Schema.CreateTable<Book>();
            }

            main.AttachDatabase(auxPath, "aux", AuxEncryptionKey);
            main.DetachDatabase("aux");

            Assert.ThrowsAny<Exception>(() => main.Query<Book>("SELECT * FROM aux.Books"));
        }
        finally
        {
            if (File.Exists(auxPath))
            {
                File.Delete(auxPath);
            }
        }
    }

    [Fact]
    public void AttachDatabase_InvalidSchemaName_Throws()
    {
        using TestDatabase main = new();

        Assert.Throws<ArgumentException>(() => main.AttachDatabase("path.db", "aux; DROP TABLE Books"));
        Assert.Throws<ArgumentException>(() => main.AttachDatabase("path.db", "aux name"));
        Assert.Throws<ArgumentException>(() => main.AttachDatabase("path.db", ""));
        Assert.Throws<ArgumentNullException>(() => main.AttachDatabase("path.db", null!));
    }

    [Fact]
    public void AttachDatabase_NullPath_Throws()
    {
        using TestDatabase main = new();
        Assert.Throws<ArgumentNullException>(() => main.AttachDatabase(null!, "aux"));
    }

    [Fact]
    public void AttachDatabase_PathWithApostrophe_EscapesSafely()
    {
        using TestDatabase main = new();

        string auxPath = Path.Combine(Path.GetTempPath(), $"aux'name_{Guid.NewGuid():N}.db3");
        try
        {
            using (SQLiteDatabase auxDb = OpenAux(auxPath))
            {
                auxDb.Schema.CreateTable<Book>();
            }

            main.AttachDatabase(auxPath, "aux", AuxEncryptionKey);
            main.DetachDatabase("aux");
        }
        finally
        {
            if (File.Exists(auxPath))
            {
                File.Delete(auxPath);
            }
        }
    }

    private static SQLiteDatabase OpenAux(string path)
    {
        SQLiteOptionsBuilder builder = new(path);
#if SQLITECIPHER
        builder.UseEncryptionKey("test-key");
#endif
        return new SQLiteDatabase(builder.Build());
    }

    private static string? AuxEncryptionKey =>
#if SQLITECIPHER
        "test-key";
#else
        null;
#endif

    [Fact]
    public void DetachDatabase_InvalidSchemaName_Throws()
    {
        using TestDatabase main = new();
        Assert.Throws<ArgumentException>(() => main.DetachDatabase("name; DROP TABLE Books"));
    }

    [Fact]
    public async Task AttachDatabaseAsync_AndDetachDatabaseAsync_RoundTrip()
    {
        using TestDatabase main = new();
        string auxPath = Path.Combine(Path.GetTempPath(), $"aux_async_{Guid.NewGuid():N}.db3");
        try
        {
            using (SQLiteDatabase auxDb = OpenAux(auxPath))
            {
                auxDb.Schema.CreateTable<Book>();
            }

            await main.AttachDatabaseAsync(auxPath, "auxAsync", AuxEncryptionKey, TestContext.Current.CancellationToken);
            await main.DetachDatabaseAsync("auxAsync", TestContext.Current.CancellationToken);

            Assert.ThrowsAny<Exception>(() => main.Query<Book>("SELECT * FROM auxAsync.Books"));
        }
        finally
        {
            if (File.Exists(auxPath)) File.Delete(auxPath);
        }
    }
}
