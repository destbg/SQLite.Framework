using SQLite.Framework.Enums;
using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class ViewAndTriggerTests
{
    [Fact]
    public void CreateView_EmitsCreateViewStatement()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().Add(new Book { Id = 1, Title = "T", AuthorId = 1, Price = 5 });
        db.Table<Book>().Add(new Book { Id = 2, Title = "U", AuthorId = 1, Price = 0 });

        db.Schema.CreateView<BookView>(() =>
            from b in db.Table<Book>()
            where b.Price > 0
            select new BookView { Id = b.Id, Title = b.Title, Price = b.Price });

        Assert.True(db.Schema.ViewExists<BookView>());

        List<BookView> rows = db.ReadOnlyTable<BookView>().ToList();
        Assert.Single(rows);
        Assert.Equal("T", rows[0].Title);
    }

    [Fact]
    public void CreateView_IsIdempotent()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        db.Schema.CreateView<BookView>(() =>
            from b in db.Table<Book>() select new BookView { Id = b.Id, Title = b.Title, Price = b.Price });
        db.Schema.CreateView<BookView>(() =>
            from b in db.Table<Book>() select new BookView { Id = b.Id, Title = b.Title, Price = b.Price });

        Assert.True(db.Schema.ViewExists<BookView>());
    }

    [Fact]
    public void DropView_Generic_Removes()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        db.Schema.CreateView<BookView>(() =>
            from b in db.Table<Book>() select new BookView { Id = b.Id, Title = b.Title, Price = b.Price });

        db.Schema.DropView<BookView>();

        Assert.False(db.Schema.ViewExists<BookView>());
    }

    [Fact]
    public void DropView_ByName_Removes()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        db.Schema.CreateView<BookView>(() =>
            from b in db.Table<Book>() select new BookView { Id = b.Id, Title = b.Title, Price = b.Price });

        db.Schema.DropView("vBookSummary");

        Assert.False(db.Schema.ViewExists("vBookSummary"));
    }

    [Fact]
    public void DropView_NonExistent_DoesNotThrow()
    {
        using TestDatabase db = new();

        db.Schema.DropView("NotThere");
        Assert.False(db.Schema.ViewExists("NotThere"));
    }

    [Fact]
    public void ViewExists_FalseWhenAbsent()
    {
        using TestDatabase db = new();
        Assert.False(db.Schema.ViewExists<BookView>());
        Assert.False(db.Schema.ViewExists("anything"));
    }

    [Fact]
    public void ListViews_ReturnsCreatedViews()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        db.Schema.CreateView<BookView>(() =>
            from b in db.Table<Book>() select new BookView { Id = b.Id, Title = b.Title, Price = b.Price });

        IReadOnlyList<string> views = db.Schema.ListViews();
        Assert.Contains("vBookSummary", views);
    }

    [Fact]
    public async Task CreateViewAsync_RoundTrips()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        await db.Schema.CreateViewAsync<BookView>(() =>
            from b in db.Table<Book>() select new BookView { Id = b.Id, Title = b.Title, Price = b.Price },
            TestContext.Current.CancellationToken);

        Assert.True(await db.Schema.ViewExistsAsync<BookView>(TestContext.Current.CancellationToken));
        IReadOnlyList<string> views = await db.Schema.ListViewsAsync(TestContext.Current.CancellationToken);
        Assert.Contains("vBookSummary", views);
        await db.Schema.DropViewAsync<BookView>(TestContext.Current.CancellationToken);
        Assert.False(db.Schema.ViewExists<BookView>());
    }

    [Fact]
    public async Task DropViewAsync_ByName_Removes()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        db.Schema.CreateView<BookView>(() =>
            from b in db.Table<Book>() select new BookView { Id = b.Id, Title = b.Title, Price = b.Price });

        await db.Schema.DropViewAsync("vBookSummary", TestContext.Current.CancellationToken);

        Assert.False(db.Schema.ViewExists("vBookSummary"));
    }

    [Fact]
    public void CreateTrigger_AfterUpdate_FiresAndWritesHistory()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        db.Table<BookHistory>().Schema.CreateTable();

        db.Schema.CreateTrigger<Book>(
            name: "trg_book_history",
            timing: SQLiteTriggerTiming.After,
            @event: SQLiteTriggerEvent.Update,
            body: "INSERT INTO BookHistory(BookId, OldPrice, NewPrice) VALUES (NEW.BookId, OLD.BookPrice, NEW.BookPrice)",
            when: "OLD.BookPrice <> NEW.BookPrice");

        db.Table<Book>().Add(new Book { Id = 1, Title = "T", AuthorId = 1, Price = 1 });
        Book book = db.Table<Book>().First();
        book.Price = 99;
        db.Table<Book>().Update(book);

        BookHistory entry = db.Table<BookHistory>().Single();
        Assert.Equal(1, entry.BookId);
        Assert.Equal(1, entry.OldPrice);
        Assert.Equal(99, entry.NewPrice);
    }

    [Fact]
    public void CreateTrigger_BeforeInsert_RoundTrips()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        db.Schema.CreateTrigger<Book>(
            name: "trg_block",
            timing: SQLiteTriggerTiming.Before,
            @event: SQLiteTriggerEvent.Insert,
            body: "SELECT RAISE(ABORT, 'nope') WHERE NEW.BookPrice < 0",
            forEachRow: true);

        Assert.ThrowsAny<Exception>(() =>
            db.Table<Book>().Add(new Book { Id = 1, Title = "T", AuthorId = 1, Price = -1 }));

        db.Schema.DropTrigger("trg_block");
        db.Table<Book>().Add(new Book { Id = 1, Title = "T", AuthorId = 1, Price = -1 });
    }

    [Fact]
    public void CreateTrigger_BodyAlreadyEndsWithSemicolon_NoDuplicate()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        db.Table<BookHistory>().Schema.CreateTable();

        db.Schema.CreateTrigger<Book>(
            name: "trg_with_semi",
            timing: SQLiteTriggerTiming.After,
            @event: SQLiteTriggerEvent.Insert,
            body: "INSERT INTO BookHistory(BookId, OldPrice, NewPrice) VALUES (NEW.BookId, 0, NEW.BookPrice);");

        db.Table<Book>().Add(new Book { Id = 1, Title = "T", AuthorId = 1, Price = 5 });

        Assert.Single(db.Table<BookHistory>().ToList());
    }

    [Fact]
    public void CreateTrigger_AfterDelete_Fires()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        db.Table<BookHistory>().Schema.CreateTable();

        db.Schema.CreateTrigger<Book>(
            name: "trg_del",
            timing: SQLiteTriggerTiming.After,
            @event: SQLiteTriggerEvent.Delete,
            body: "INSERT INTO BookHistory(BookId, OldPrice, NewPrice) VALUES (OLD.BookId, OLD.BookPrice, 0)");

        db.Table<Book>().Add(new Book { Id = 1, Title = "T", AuthorId = 1, Price = 5 });
        db.Table<Book>().Remove(new Book { Id = 1, Title = "T", AuthorId = 1, Price = 5 });

        Assert.Single(db.Table<BookHistory>().ToList());
    }

    [Fact]
    public void CreateTrigger_NotForEachRow_Works()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        db.Table<BookHistory>().Schema.CreateTable();

        db.Schema.CreateTrigger<Book>(
            name: "trg_stmt",
            timing: SQLiteTriggerTiming.After,
            @event: SQLiteTriggerEvent.Insert,
            body: "INSERT INTO BookHistory(BookId, OldPrice, NewPrice) VALUES (-1, 0, 0)",
            forEachRow: false);

        db.Table<Book>().Add(new Book { Id = 1, Title = "T", AuthorId = 1, Price = 5 });

        Assert.NotEmpty(db.Table<BookHistory>().ToList());
    }

    [Fact]
    public void CreateTrigger_InsteadOfOnView_RoundTrips()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        db.Table<BookHistory>().Schema.CreateTable();
        db.Schema.CreateView<BookView>(() =>
            from b in db.Table<Book>() select new BookView { Id = b.Id, Title = b.Title, Price = b.Price });

        db.Schema.CreateTrigger<BookView>(
            name: "trg_view_io",
            timing: SQLiteTriggerTiming.InsteadOf,
            @event: SQLiteTriggerEvent.Insert,
            body: "INSERT INTO BookHistory(BookId, OldPrice, NewPrice) VALUES (NEW.Id, 0, NEW.Price)");

        db.Execute("INSERT INTO vBookSummary(Id, Title, Price) VALUES (1, 'T', 5)");

        Assert.Single(db.Table<BookHistory>().ToList());
    }

    [Fact]
    public async Task CreateTriggerAsync_RoundTrips()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        db.Table<BookHistory>().Schema.CreateTable();

        await db.Schema.CreateTriggerAsync<Book>(
            "trg_async",
            SQLiteTriggerTiming.After,
            SQLiteTriggerEvent.Insert,
            "INSERT INTO BookHistory(BookId, OldPrice, NewPrice) VALUES (NEW.BookId, 0, NEW.BookPrice)",
            ct: TestContext.Current.CancellationToken);

        db.Table<Book>().Add(new Book { Id = 1, Title = "T", AuthorId = 1, Price = 5 });
        Assert.Single(db.Table<BookHistory>().ToList());

        await db.Schema.DropTriggerAsync("trg_async", TestContext.Current.CancellationToken);
    }

    [Fact]
    public void CreateTrigger_NullName_Throws()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        Assert.Throws<ArgumentNullException>(() =>
            db.Schema.CreateTrigger<Book>(null!, SQLiteTriggerTiming.After, SQLiteTriggerEvent.Insert, "SELECT 1"));
    }

    [Fact]
    public void CreateTrigger_NullBody_Throws()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        Assert.Throws<ArgumentNullException>(() =>
            db.Schema.CreateTrigger<Book>("trg_x", SQLiteTriggerTiming.After, SQLiteTriggerEvent.Insert, null!));
    }

    [Fact]
    public void CreateTrigger_InvalidTiming_Throws()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            db.Schema.CreateTrigger<Book>("trg_x", (SQLiteTriggerTiming)42, SQLiteTriggerEvent.Insert, "SELECT 1"));
    }

    [Fact]
    public void CreateTrigger_InvalidEvent_Throws()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            db.Schema.CreateTrigger<Book>("trg_x", SQLiteTriggerTiming.After, (SQLiteTriggerEvent)42, "SELECT 1"));
    }
}
