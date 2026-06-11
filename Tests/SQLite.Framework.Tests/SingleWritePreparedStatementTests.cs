using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.CompilerServices;
using SQLite.Framework.Enums;
using SQLite.Framework.Exceptions;
using SQLite.Framework.Models;
using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("PreparedWriteDefaultRows")]
public class PreparedWriteDefaultRow
{
    [Key]
    public int Id { get; set; }

    [DefaultValue(10)]
    public int Rating { get; set; }
}

public sealed class SeededWritesDatabase : TestDatabase
{
    public SeededWritesDatabase([CallerMemberName] string? methodName = null)
        : base(methodName)
    {
    }

    protected override void OnModelCreating(SQLiteModelBuilder builder)
    {
        Table<Book>().Schema.CreateTable();
        Table<Book>().Add(new Book { Id = 1, Title = "seed", AuthorId = 1, Price = 1 });
        Table<Book>().Add(new Book { Id = 2, Title = "temp", AuthorId = 1, Price = 2 });
        Table<Book>().Update(new Book { Id = 1, Title = "seeded", AuthorId = 1, Price = 1 });
        Table<Book>().Remove(new Book { Id = 2, Title = "temp", AuthorId = 1, Price = 2 });
        Table<Book>().AddOrUpdate(new Book { Id = 3, Title = "upserted", AuthorId = 1, Price = 3 });
    }
}

public class SingleWritePreparedStatementTests
{
    [Fact]
    public void Add_SameTableTwice_InsertsBothRows()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        int first = db.Table<Book>().Add(new Book { Id = 1, Title = "A", AuthorId = 1, Price = 1 });
        int second = db.Table<Book>().Add(new Book { Id = 2, Title = "B", AuthorId = 2, Price = 2 });

        Assert.Equal(1, first);
        Assert.Equal(1, second);
        List<Book> rows = db.Table<Book>().OrderBy(b => b.Id).ToList();
        Assert.Equal(new[] { "A", "B" }, rows.Select(b => b.Title));
    }

    [Fact]
    public void Update_SameTableTwice_PersistsLatestValues()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().Add(new Book { Id = 1, Title = "A", AuthorId = 1, Price = 1 });

        int first = db.Table<Book>().Update(new Book { Id = 1, Title = "B", AuthorId = 2, Price = 2 });
        int second = db.Table<Book>().Update(new Book { Id = 1, Title = "C", AuthorId = 3, Price = 3 });

        Assert.Equal(1, first);
        Assert.Equal(1, second);
        Book row = db.Table<Book>().Single();
        Assert.Equal("C", row.Title);
        Assert.Equal(3, row.AuthorId);
        Assert.Equal(3, row.Price);
    }

    [Fact]
    public void Remove_SameTableTwice_DeletesEachRow()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().Add(new Book { Id = 1, Title = "A", AuthorId = 1, Price = 1 });
        db.Table<Book>().Add(new Book { Id = 2, Title = "B", AuthorId = 2, Price = 2 });

        int first = db.Table<Book>().Remove(new Book { Id = 1, Title = "A", AuthorId = 1, Price = 1 });
        int second = db.Table<Book>().Remove(new Book { Id = 2, Title = "B", AuthorId = 2, Price = 2 });
        int missing = db.Table<Book>().Remove(new Book { Id = 3, Title = "C", AuthorId = 3, Price = 3 });

        Assert.Equal(1, first);
        Assert.Equal(1, second);
        Assert.Equal(0, missing);
        Assert.Equal(0, db.Table<Book>().Count());
    }

    [Fact]
    public void AddOrUpdate_SameKeyTwice_KeepsSingleRowWithLatestValues()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        db.Table<Book>().AddOrUpdate(new Book { Id = 1, Title = "A", AuthorId = 1, Price = 1 });
        db.Table<Book>().AddOrUpdate(new Book { Id = 1, Title = "B", AuthorId = 2, Price = 2 });

        Book row = db.Table<Book>().Single();
        Assert.Equal("B", row.Title);
    }

    [Fact]
    public void AddOrUpdate_UnknownConflictValue_BehavesLikeReplace()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        db.Table<Book>().AddOrUpdate(new Book { Id = 1, Title = "A", AuthorId = 1, Price = 1 }, (SQLiteConflict)99);
        db.Table<Book>().AddOrUpdate(new Book { Id = 1, Title = "B", AuthorId = 2, Price = 2 }, (SQLiteConflict)99);

        Book row = db.Table<Book>().Single();
        Assert.Equal("B", row.Title);
    }

    [Fact]
    public void AddOrUpdate_IgnoreConflictOnExistingRow_KeepsOriginalValues()
    {
        using TestDatabase db = new();
        db.Table<Article>().Schema.CreateTable();
        Article original = new() { Title = "original", Body = "b", PublishedAt = DateTime.UtcNow };
        db.Table<Article>().Add(original);

        int changes = db.Table<Article>().AddOrUpdate(
            new Article { Id = original.Id, Title = "replacement", Body = "b", PublishedAt = DateTime.UtcNow },
            SQLiteConflict.Ignore);

        Assert.Equal(0, changes);
        Article row = db.Table<Article>().Single();
        Assert.Equal("original", row.Title);
    }

    [Fact]
    public void Add_AutoIncrementKey_AssignsKeyOnEachInsert()
    {
        using TestDatabase db = new();
        db.Table<Article>().Schema.CreateTable();
        Article first = new() { Title = "a", Body = "b", PublishedAt = DateTime.UtcNow };
        Article second = new() { Title = "c", Body = "d", PublishedAt = DateTime.UtcNow };

        db.Table<Article>().Add(first);
        db.Table<Article>().Add(second);

        Assert.Equal(1, first.Id);
        Assert.Equal(2, second.Id);
    }

    [Fact]
    public void Add_DuplicateKey_ThrowsAndKeepsTableUsable()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().Add(new Book { Id = 1, Title = "A", AuthorId = 1, Price = 1 });

        Assert.Throws<SQLiteException>(() =>
            db.Table<Book>().Add(new Book { Id = 1, Title = "B", AuthorId = 2, Price = 2 }));

        db.Table<Book>().Add(new Book { Id = 2, Title = "C", AuthorId = 3, Price = 3 });
        Assert.Equal(2, db.Table<Book>().Count());
    }

    [Fact]
    public void Add_DefaultValueColumnAtClrDefault_AppliesDatabaseDefault()
    {
        using TestDatabase db = new();
        db.Table<PreparedWriteDefaultRow>().Schema.CreateTable();

        db.Table<PreparedWriteDefaultRow>().Add(new PreparedWriteDefaultRow { Id = 1 });

        PreparedWriteDefaultRow row = db.Table<PreparedWriteDefaultRow>().Single();
        Assert.Equal(10, row.Rating);
    }

    [Fact]
    public void Add_DefaultValueColumnWithExplicitValue_KeepsValue()
    {
        using TestDatabase db = new();
        db.Table<PreparedWriteDefaultRow>().Schema.CreateTable();

        db.Table<PreparedWriteDefaultRow>().Add(new PreparedWriteDefaultRow { Id = 1, Rating = 7 });
        db.Table<PreparedWriteDefaultRow>().Add(new PreparedWriteDefaultRow { Id = 2, Rating = 8 });

        List<int> ratings = db.Table<PreparedWriteDefaultRow>().OrderBy(r => r.Id).Select(r => r.Rating).ToList();
        Assert.Equal(new[] { 7, 8 }, ratings);
    }

    [Fact]
    public void SingleWrites_InsideOnModelCreating_WorkAndLaterWritesStillWork()
    {
        using SeededWritesDatabase db = new();

        List<Book> rows = db.Table<Book>().OrderBy(b => b.Id).ToList();

        Assert.Equal(2, rows.Count);
        Assert.Equal("seeded", rows[0].Title);
        Assert.Equal("upserted", rows[1].Title);

        db.Table<Book>().Add(new Book { Id = 4, Title = "later", AuthorId = 4, Price = 4 });
        Assert.Equal(3, db.Table<Book>().Count());
    }

    [Fact]
    public void SingleWrites_WithCommandInterceptor_NotifyInterceptorForEachWrite()
    {
        RecordingInterceptor recorder = new();
        using TestDatabase db = new(b => b.AddCommandInterceptor(recorder));
        db.Table<Book>().Schema.CreateTable();

        recorder.ExecutingTexts.Clear();
        db.Table<Book>().Add(new Book { Id = 1, Title = "A", AuthorId = 1, Price = 1 });
        db.Table<Book>().Update(new Book { Id = 1, Title = "B", AuthorId = 2, Price = 2 });
        db.Table<Book>().AddOrUpdate(new Book { Id = 1, Title = "C", AuthorId = 3, Price = 3 });
        db.Table<Book>().Remove(new Book { Id = 1, Title = "C", AuthorId = 3, Price = 3 });

        Assert.Equal(
        [
            "INSERT INTO \"Books\" (\"BookId\", \"BookTitle\", \"BookAuthorId\", \"BookPrice\") VALUES (@p0, @p1, @p2, @p3)",
            "UPDATE \"Books\" SET \"BookId\" = @p0, \"BookTitle\" = @p1, \"BookAuthorId\" = @p2, \"BookPrice\" = @p3 WHERE \"BookId\" = @p4",
            "INSERT OR REPLACE INTO \"Books\" (\"BookId\", \"BookTitle\", \"BookAuthorId\", \"BookPrice\") VALUES (@p0, @p1, @p2, @p3)",
            "DELETE FROM \"Books\" WHERE \"BookId\" = @p0",
        ], recorder.ExecutingTexts);
    }

    [Fact]
    public void AddOrUpdate_AbortConflictOnExistingRow_WithInterceptor_ThrowsAndKeepsRow()
    {
        RecordingInterceptor recorder = new();
        using TestDatabase db = new(b => b.AddCommandInterceptor(recorder));
        db.Table<Article>().Schema.CreateTable();
        Article original = new() { Title = "original", Body = "b", PublishedAt = DateTime.UtcNow };
        db.Table<Article>().Add(original);

        Assert.Throws<SQLiteException>(() => db.Table<Article>().AddOrUpdate(
            new Article { Id = original.Id, Title = "replacement", Body = "b", PublishedAt = DateTime.UtcNow },
            SQLiteConflict.Abort));

        Assert.Equal("original", db.Table<Article>().Single().Title);
    }

    [Fact]
    public void Add_HookRegisteredForDifferentType_DoesNotBlockWrite()
    {
        using TestDatabase db = new(b => b.OnAdd<Article>((_, _) => false));
        db.Table<Book>().Schema.CreateTable();

        int changes = db.Table<Book>().Add(new Book { Id = 1, Title = "A", AuthorId = 1, Price = 1 });

        Assert.Equal(1, changes);
        Assert.Equal(1, db.Table<Book>().Count());
    }

    [Fact]
    public void Update_TableMappingSharedAcrossDatabases_WritesToTheCallingDatabase()
    {
        using TestDatabase db1 = new();
        using TestDatabase db2 = new();
        db1.Table<Book>().Schema.CreateTable();
        db2.Table<Book>().Schema.CreateTable();
        db1.Table<Book>().Add(new Book { Id = 1, Title = "one", AuthorId = 1, Price = 1 });
        db2.Table<Book>().Add(new Book { Id = 1, Title = "two", AuthorId = 1, Price = 1 });

        TableMapping shared = db1.TableMapping<Book>();
        SQLiteTable<Book> viaDb2 = new(db2, shared);

        int changes = viaDb2.Update(new Book { Id = 1, Title = "two-updated", AuthorId = 2, Price = 2 });
        Assert.Equal(1, changes);
        Assert.Equal("two-updated", db2.Table<Book>().Single().Title);
        Assert.Equal("one", db1.Table<Book>().Single().Title);

        int back = db1.Table<Book>().Update(new Book { Id = 1, Title = "one-updated", AuthorId = 3, Price = 3 });
        Assert.Equal(1, back);
        Assert.Equal("one-updated", db1.Table<Book>().Single().Title);
        Assert.Equal("two-updated", db2.Table<Book>().Single().Title);
    }

    private sealed class RecordingInterceptor : ISQLiteCommandInterceptor
    {
        public List<string> ExecutingTexts { get; } = [];

        public void OnExecuting(SQLiteCommand command)
        {
            ExecutingTexts.Add(command.CommandText);
        }

        public void OnExecuted(SQLiteCommand command, int? rowsAffected)
        {
        }

        public void OnFailed(SQLiteCommand command, Exception exception)
        {
        }
    }
}
