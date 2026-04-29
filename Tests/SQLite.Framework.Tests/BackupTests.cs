using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class BackupTests
{
    [Fact]
    public void BackupTo_Database_CopiesAllRows()
    {
        using TestDatabase source = new();
        source.Table<Book>().Schema.CreateTable();
        source.Table<Book>().AddRange(new[]
        {
            new Book { Id = 1, Title = "a", AuthorId = 1, Price = 1 },
            new Book { Id = 2, Title = "b", AuthorId = 1, Price = 2 },
            new Book { Id = 3, Title = "c", AuthorId = 2, Price = 3 },
        });

        using TestDatabase destination = new();
        source.BackupTo(destination);

        List<Book> rows = destination.Table<Book>().OrderBy(b => b.Id).ToList();
        Assert.Equal(3, rows.Count);
        Assert.Equal(["a", "b", "c"], rows.Select(b => b.Title));
    }

    [Fact]
    public void BackupTo_Path_CreatesFileWithSameContent()
    {
        using TestDatabase source = new();
        source.Table<Book>().Schema.CreateTable();
        source.Table<Book>().Add(new Book { Id = 42, Title = "saved", AuthorId = 1, Price = 1 });

        string path = Path.Combine(Path.GetTempPath(), $"backup_{Guid.NewGuid():N}.db3");
        try
        {
            source.BackupTo(path);

            Assert.True(File.Exists(path));

            SQLiteOptionsBuilder reopenBuilder = new(path);
#if SQLITECIPHER
            reopenBuilder.UseEncryptionKey("test-key");
#endif
            using SQLiteDatabase reopened = new(reopenBuilder.Build());
            Book row = reopened.Table<Book>().Single();
            Assert.Equal(42, row.Id);
            Assert.Equal("saved", row.Title);
        }
        finally
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }

    [Fact]
    public void BackupTo_OverwritesExistingDestinationData()
    {
        using TestDatabase source = new();
        source.Table<Book>().Schema.CreateTable();
        source.Table<Book>().Add(new Book { Id = 1, Title = "from-source", AuthorId = 1, Price = 1 });

        using TestDatabase destination = new();
        destination.Table<Book>().Schema.CreateTable();
        destination.Table<Book>().Add(new Book { Id = 99, Title = "stale", AuthorId = 1, Price = 1 });

        source.BackupTo(destination);

        List<Book> rows = destination.Table<Book>().ToList();
        Assert.Single(rows);
        Assert.Equal("from-source", rows[0].Title);
    }

    [Fact]
    public void BackupTo_NullDestination_Throws()
    {
        using TestDatabase source = new();
        Assert.Throws<ArgumentNullException>(() => source.BackupTo((SQLiteDatabase)null!));
    }

    [Fact]
    public void BackupTo_NullPath_Throws()
    {
        using TestDatabase source = new();
        Assert.Throws<ArgumentNullException>(() => source.BackupTo((string)null!));
    }

    [Fact]
    public void BackupTo_EmptyPath_Throws()
    {
        using TestDatabase source = new();
        Assert.Throws<ArgumentException>(() => source.BackupTo(""));
    }

    [Fact]
    public async Task BackupToAsync_Database_CopiesAllRows()
    {
        using TestDatabase source = new();
        source.Table<Book>().Schema.CreateTable();
        source.Table<Book>().Add(new Book { Id = 1, Title = "async-copy", AuthorId = 1, Price = 1 });

        using TestDatabase destination = new();
        await source.BackupToAsync(destination, ct: TestContext.Current.CancellationToken);

        Book row = destination.Table<Book>().Single();
        Assert.Equal("async-copy", row.Title);
    }

    [Fact]
    public async Task BackupToAsync_Path_CreatesFile()
    {
        using TestDatabase source = new();
        source.Table<Book>().Schema.CreateTable();
        source.Table<Book>().Add(new Book { Id = 7, Title = "to-file", AuthorId = 1, Price = 1 });

        string path = Path.Combine(Path.GetTempPath(), $"backup_async_{Guid.NewGuid():N}.db3");
        try
        {
            await source.BackupToAsync(path, TestContext.Current.CancellationToken);
            Assert.True(File.Exists(path));
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }
}
