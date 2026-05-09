using System.Reflection;
using SQLite.Framework.Enums;
using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[CollectionDefinition("AsyncContention", DisableParallelization = true)]
public class AsyncContentionCollection;

[Collection("AsyncContention")]
public class AsyncContentionTests
{
    private static async Task RunContended(Func<TestDatabase, Task> action, Action<TestDatabase>? setup = null)
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        setup?.Invoke(db);

        using ManualResetEventSlim acquired = new();
        using ManualResetEventSlim release = new();
        Thread holder = new(() =>
        {
            using IDisposable l = db.Lock();
            acquired.Set();
            release.Wait();
        });
        holder.IsBackground = true;
        holder.Start();

        acquired.Wait();

        Task t = action(db);
        Assert.False(t.IsCompleted);

        release.Set();
        holder.Join();
        await t;
    }

    [Fact]
    public Task ExecuteAsync_Params_Contended() =>
        RunContended(db => db.ExecuteAsync("DELETE FROM Books", Array.Empty<SQLiteParameter>()));

    [Fact]
    public Task ExecuteAsync_AnonObject_Contended() =>
        RunContended(db => db.ExecuteAsync("DELETE FROM Books WHERE BookId = @id", new { id = -1 }));

    [Fact]
    public Task AttachDatabaseAsync_Contended() =>
        RunContended(async db =>
        {
            string path = Path.Combine(Path.GetTempPath(), $"attach_{Guid.NewGuid():N}.db");
            try
            {
                await db.AttachDatabaseAsync(path, "auxAsync");
                await db.DetachDatabaseAsync("auxAsync");
            }
            finally
            {
                if (File.Exists(path)) File.Delete(path);
            }
        });

    [Fact]
    public Task BackupToAsync_Path_Contended() =>
        RunContended(async db =>
        {
            string path = Path.Combine(Path.GetTempPath(), $"backup_{Guid.NewGuid():N}.db");
            try
            {
                await db.BackupToAsync(path);
            }
            finally
            {
                if (File.Exists(path)) File.Delete(path);
            }
        });

    [Fact]
    public Task BackupToAsync_Database_Contended() =>
        RunContended(async db =>
        {
            using TestDatabase dest = new();
            await db.BackupToAsync(dest);
        });

    [Fact] public Task GetForeignKeysAsync_Contended() => RunContended(db => db.Pragmas.GetForeignKeysAsync());
    [Fact] public Task SetForeignKeysAsync_Contended() => RunContended(db => db.Pragmas.SetForeignKeysAsync(true));
    [Fact] public Task GetJournalModeAsync_Contended() => RunContended(db => db.Pragmas.GetJournalModeAsync());
    [Fact] public Task SetJournalModeAsync_Contended() => RunContended(db => db.Pragmas.SetJournalModeAsync("WAL"));
    [Fact] public Task GetCacheSizeAsync_Contended() => RunContended(db => db.Pragmas.GetCacheSizeAsync());
    [Fact] public Task SetCacheSizeAsync_Contended() => RunContended(db => db.Pragmas.SetCacheSizeAsync(-2000));
    [Fact] public Task GetSynchronousModeAsync_Contended() => RunContended(db => db.Pragmas.GetSynchronousModeAsync());
    [Fact] public Task SetSynchronousModeAsync_Contended() => RunContended(db => db.Pragmas.SetSynchronousModeAsync(SQLiteSynchronousMode.Normal));
    [Fact] public Task GetUserVersionAsync_Contended() => RunContended(db => db.Pragmas.GetUserVersionAsync());
    [Fact] public Task SetUserVersionAsync_Contended() => RunContended(db => db.Pragmas.SetUserVersionAsync(42));
    [Fact] public Task GetPageSizeAsync_Contended() => RunContended(db => db.Pragmas.GetPageSizeAsync());
    [Fact] public Task GetFreelistCountAsync_Contended() => RunContended(db => db.Pragmas.GetFreelistCountAsync());
    [Fact] public Task GetRecursiveTriggersAsync_Contended() => RunContended(db => db.Pragmas.GetRecursiveTriggersAsync());
    [Fact] public Task SetRecursiveTriggersAsync_Contended() => RunContended(db => db.Pragmas.SetRecursiveTriggersAsync(true));
    [Fact] public Task GetTempStoreAsync_Contended() => RunContended(db => db.Pragmas.GetTempStoreAsync());
    [Fact] public Task SetTempStoreAsync_Contended() => RunContended(db => db.Pragmas.SetTempStoreAsync(2));
    [Fact] public Task GetSecureDeleteAsync_Contended() => RunContended(db => db.Pragmas.GetSecureDeleteAsync());
    [Fact] public Task SetSecureDeleteAsync_Contended() => RunContended(db => db.Pragmas.SetSecureDeleteAsync(true));

    [Fact] public Task SchemaCreateTableAsync_Type_Contended() => RunContended(db => db.Schema.CreateTableAsync(typeof(Author)));
    [Fact] public Task SchemaCreateTableAsync_Generic_Contended() => RunContended(db => db.Schema.CreateTableAsync<Author>());
    [Fact] public Task SchemaDropTableAsync_Generic_Contended() => RunContended(db => db.Schema.DropTableAsync<Book>());
    [Fact] public Task SchemaDropTableAsync_ByName_Contended() => RunContended(db => db.Schema.DropTableAsync("Books"));
    [Fact] public Task SchemaCreateIndexAsync_Contended() => RunContended(db => db.Schema.CreateIndexAsync<Book>(b => b.Title, "IX_Async_T"));
    [Fact] public Task SchemaDropIndexAsync_Contended() => RunContended(
        db => db.Schema.DropIndexAsync("IX_drop_me"),
        db => db.Schema.CreateIndex<Book>(b => b.Title, "IX_drop_me"));
    [Fact] public Task SchemaTableExistsAsync_Generic_Contended() => RunContended(db => db.Schema.TableExistsAsync<Book>());
    [Fact] public Task SchemaTableExistsAsync_ByName_Contended() => RunContended(db => db.Schema.TableExistsAsync("Books"));
    [Fact] public Task SchemaIndexExistsAsync_Contended() => RunContended(db => db.Schema.IndexExistsAsync("IX_missing"));
    [Fact] public Task SchemaColumnExistsAsync_Contended() => RunContended(db => db.Schema.ColumnExistsAsync<Book>("BookTitle"));
    [Fact] public Task SchemaListTablesAsync_Contended() => RunContended(db => db.Schema.ListTablesAsync());
    [Fact] public Task SchemaListIndexesAsync_Contended() => RunContended(db => db.Schema.ListIndexesAsync());
    [Fact] public Task SchemaListColumnsAsync_Contended() => RunContended(db => db.Schema.ListColumnsAsync<Book>());
    [Fact] public Task SchemaAddColumnAsync_Contended() => RunContended(
        db => db.Schema.AddColumnAsync<BookArchive>(nameof(BookArchive.Price)),
        db =>
        {
            db.Schema.CreateTable<BookArchive>();
            db.Schema.DropColumn<BookArchive>("BookPrice");
        });
    [Fact] public Task SchemaRenameColumnAsync_Contended() => RunContended(
        db => db.Schema.RenameColumnAsync<BookArchive>("BookTitle", "Title2"),
        db => db.Schema.CreateTable<BookArchive>());
    [Fact] public Task SchemaDropColumnAsync_Contended() => RunContended(
        db => db.Schema.DropColumnAsync<BookArchive>("BookPrice"),
        db => db.Schema.CreateTable<BookArchive>());
    [Fact] public Task SchemaRenameTableAsync_Contended() => RunContended(db => db.Schema.RenameTableAsync<Book>("Books_Renamed"));
    [Fact] public Task SchemaTableBuilderCreateAsync_Contended() => RunContended(db => db.Schema.Table<Author>().CreateTableAsync());

    [Fact]
    public Task AddAsync_Contended() => RunContended(db => db.Table<Book>().AddAsync(new Book { Id = 100, Title = "C", AuthorId = 1, Price = 100 }));

    [Fact]
    public Task UpdateAsync_Contended()
    {
        Book book = new() { Id = 200, Title = "U", AuthorId = 1, Price = 200 };
        return RunContended(
            db =>
            {
                book.Title = "U2";
                return db.Table<Book>().UpdateAsync(book);
            },
            db => db.Table<Book>().Add(book));
    }

    [Fact]
    public Task RemoveAsync_Contended()
    {
        Book book = new() { Id = 300, Title = "R", AuthorId = 1, Price = 300 };
        return RunContended(
            db => db.Table<Book>().RemoveAsync(book),
            db => db.Table<Book>().Add(book));
    }

    [Fact]
    public Task AddOrUpdateAsync_Contended() => RunContended(db =>
        db.Table<Book>().AddOrUpdateAsync(new Book { Id = 400, Title = "AU", AuthorId = 1, Price = 400 }));

    [Fact]
    public Task UpsertAsync_Contended() => RunContended(db =>
        db.Table<Book>().UpsertAsync(
            new Book { Id = 500, Title = "Up", AuthorId = 1, Price = 500 },
            c => c.OnConflict(b => b.Id).DoNothing()));

    [Fact]
    public Task ClearAsync_Contended() => RunContended(db => db.Table<Book>().ClearAsync());

    [Fact]
    public Task InsertFromQueryAsync_Contended() => RunContended(
        db => db.Table<BookArchive>().InsertFromQueryAsync(
            db.Table<Book>().Select(b => new BookArchive { Id = b.Id, Title = b.Title, AuthorId = b.AuthorId, Price = b.Price })),
        db =>
        {
            db.Schema.CreateTable<BookArchive>();
            db.Table<Book>().Add(new Book { Id = 1, Title = "A", AuthorId = 1, Price = 1 });
        });

    [Fact]
    public Task ExecuteDeleteAsync_Contended() => RunContended(db => db.Table<Book>().ExecuteDeleteAsync());

    [Fact]
    public Task ExecuteDeleteAsync_Predicate_Contended() => RunContended(db => db.Table<Book>().ExecuteDeleteAsync(b => b.Id > 0));

    [Fact]
    public Task ExecuteUpdateAsync_Contended() => RunContended(db => db.Table<Book>().ExecuteUpdateAsync(s => s.Set(b => b.Price, 99)));

    [Fact]
    public Task ExecuteNonQueryAsync_Contended() => RunContended(db =>
        db.CreateCommand("DELETE FROM Books", []).ExecuteNonQueryAsync());

    [Fact]
    public Task ExecuteWithLastRowIdAsync_Contended() => RunContended(db =>
        db.CreateCommand(
            "INSERT INTO Books (BookTitle, BookAuthorId, BookPrice) VALUES ('R', 1, 999)",
            []).ExecuteWithLastRowIdAsync());

    [Fact]
    public Task DetachDatabaseAsync_Contended() => RunContended(
        db => db.DetachDatabaseAsync("auxAttached"),
        db =>
        {
            string path = Path.Combine(Path.GetTempPath(), $"detach_{Guid.NewGuid():N}.db");
            db.AttachDatabase(path, "auxAttached");
        });

    [Fact]
    public Task BackupToAsync_DestinationContended()
    {
        return Run();

        static async Task Run()
        {
            using TestDatabase src = new();
            using TestDatabase dest = new();
            src.Table<Book>().Schema.CreateTable();

            using ManualResetEventSlim acquired = new();
            using ManualResetEventSlim release = new();
            Thread holder = new(() =>
            {
                using IDisposable l = dest.Lock();
                acquired.Set();
                release.Wait();
            });
            holder.IsBackground = true;
            holder.Start();

            acquired.Wait();

            Task t = src.BackupToAsync(dest);
            Assert.False(t.IsCompleted);

            release.Set();
            holder.Join();
            await t;
        }
    }

#pragma warning disable CS0618
    [Fact]
    public Task ObsoleteCreateTableAsync_Contended() => RunContended(db => db.Table<Author>().CreateTableAsync());

    [Fact]
    public Task ObsoleteDropTableAsync_Contended() => RunContended(db => db.Table<Book>().DropTableAsync());
#pragma warning restore CS0618

    [Fact]
    public Task AddRangeAsync_NoTransaction_Contended() => RunContended(
        db => db.Table<Book>().AddRangeAsync(
            new[] { new Book { Id = 700, Title = "ART", AuthorId = 1, Price = 700 } },
            runInTransaction: false));

    [Fact]
    public Task AddRangeAsync_WithTransaction_Contended() => RunContended(
        db => db.Table<Book>().AddRangeAsync(
            new[] { new Book { Id = 701, Title = "ART2", AuthorId = 1, Price = 701 } }));
}
