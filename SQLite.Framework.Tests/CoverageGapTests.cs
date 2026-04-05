using System.ComponentModel.DataAnnotations;
using SQLite.Framework.Attributes;
using SQLite.Framework.Enums;
using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class CoverageGapTests
{
    [Fact]
    public void IndexedAttribute_NameOrderConstructor_SetsProperties()
    {
        IndexedAttribute attr = new("IX_Test", 2);
        Assert.Equal("IX_Test", attr.Name);
        Assert.Equal(2, attr.Order);
        Assert.False(attr.IsUnique);
    }

    [Fact]
    public void ExecuteDelete_OnNonSQLiteQueryable_Throws()
    {
        IQueryable<Book> queryable = Array.Empty<Book>().AsQueryable();
        Assert.Throws<InvalidOperationException>(() => queryable.ExecuteDelete());
    }

    [Fact]
    public void ExecuteDelete_WithPredicate_OnNonSQLiteQueryable_Throws()
    {
        IQueryable<Book> queryable = Array.Empty<Book>().AsQueryable();
        Assert.Throws<InvalidOperationException>(() => queryable.ExecuteDelete(b => b.Id == 1));
    }

    [Fact]
    public void ExecuteUpdate_OnNonSQLiteQueryable_Throws()
    {
        IQueryable<Book> queryable = Array.Empty<Book>().AsQueryable();
        Assert.Throws<InvalidOperationException>(() => queryable.ExecuteUpdate(s => s.Set(b => b.Title, "x")));
    }

    [Fact]
    public void ExecuteUpdate_SetOnMethodExpression_Throws()
    {
        using TestDatabase db = new();
        db.Table<Book>().CreateTable();

        Assert.Throws<ArgumentException>(() =>
            db.Table<Book>().ExecuteUpdate(s => s.Set(b => b.Title.ToUpper(), "X")));
    }

    [Fact]
    public void ExecuteUpdate_SetOnNonDirectProperty_Throws()
    {
        using TestDatabase db = new();
        db.Table<Book>().CreateTable();

        Author other = new() { Name = "X", Email = "X", BirthDate = default };
        Assert.Throws<ArgumentException>(() =>
            db.Table<Book>().ExecuteUpdate(s => s.Set(b => other.Name, "X")));
    }

    [Fact]
    public void ExecuteUpdate_SetOnField_Throws()
    {
        using TestDatabase db = new();
        db.Table<Book>().CreateTable();

        Assert.Throws<ArgumentException>(() =>
            db.Table<Book>().ExecuteUpdate(s => s.Set(b => ((BookWithField)(object)b).Title, "X")));
    }

    [Fact]
    public void ExecuteUpdate_SetExpressionNotTranslatable_Throws()
    {
        using TestDatabase db = new();
        db.Table<Book>().CreateTable();

        Assert.Throws<ArgumentException>(() =>
            db.Table<Book>().ExecuteUpdate(s => s.Set(b => b.Title, b => string.Intern(b.Title))));
    }

    [Fact]
    public void SQLiteCteTyped_GetEnumerator_ExecutesQuery()
    {
        using TestDatabase db = new();
        db.Table<Book>().CreateTable();
        db.Table<Book>().Add(new Book { Id = 1, Title = "Test", AuthorId = 1, Price = 1 });

        SQLiteCte<Book> cte = db.With(() => db.Table<Book>());

        List<Book> results = [];
        foreach (Book book in cte)
        {
            results.Add(book);
        }

        Assert.Single(results);
    }

    [Fact]
    public void SQLiteCte_NonGenericGetEnumerator_ExecutesQuery()
    {
        using TestDatabase db = new();
        db.Table<Book>().CreateTable();
        db.Table<Book>().Add(new Book { Id = 1, Title = "Test", AuthorId = 1, Price = 1 });

        SQLiteCte<Book> cte = db.With(() => db.Table<Book>());

        int count = 0;
        foreach (object _ in (System.Collections.IEnumerable)cte)
        {
            count++;
        }

        Assert.Equal(1, count);
    }

    [Fact]
    public void Queryable_IEnumerable_GetEnumerator_IteratesRows()
    {
        using TestDatabase db = new();
        db.Table<Book>().CreateTable();
        db.Table<Book>().Add(new Book { Id = 1, Title = "Test", AuthorId = 1, Price = 1 });

        System.Collections.IEnumerable query = (System.Collections.IEnumerable)db.Table<Book>().Where(b => b.Id == 1);

        int count = 0;
        foreach (object _ in query)
        {
            count++;
        }

        Assert.Equal(1, count);
    }

    [Fact]
    public async Task BeginTransactionAwaiter_OnCompleted_InvokedWhenContended()
    {
        using TestDatabase db = new();
        db.Table<Book>().CreateTable();

        ManualResetEventSlim lockHeld = new(false);
        ManualResetEventSlim releaseSignal = new(false);

        Task lockHolder = Task.Run(() =>
        {
            using SQLiteTransaction tx = db.BeginTransaction();
            lockHeld.Set();
            releaseSignal.Wait();
            tx.Commit();
        });

        lockHeld.Wait();

        SQLiteBeginTransactionAwaiter awaiter = db.BeginTransactionAsync().GetAwaiter();
        Assert.False(awaiter.IsCompleted);

        TaskCompletionSource tcs = new();
        awaiter.OnCompleted(tcs.SetResult);

        releaseSignal.Set();

        await tcs.Task.WaitAsync(TimeSpan.FromSeconds(5));

        SQLiteTransaction tx2 = awaiter.GetResult();
        tx2.Rollback();

        await lockHolder;
    }

    [Fact]
    public void GroupJoin_WithoutDefaultIfEmpty_ThrowsNotSupported()
    {
        using TestDatabase db = new();

        Assert.Throws<NotSupportedException>(() =>
        {
            db.Table<Book>()
                .GroupJoin(
                    db.Table<Author>(),
                    b => b.AuthorId,
                    a => a.Id,
                    (book, authors) => new { book, authors }
                )
                .ToSqlCommand();
        });
    }

    [Fact]
    public void DateOnly_StoredAsText_RoundTrip()
    {
        using TestDatabase db = new();
        db.StorageOptions.DateOnlyStorage = DateOnlyStorageMode.Text;
        db.Table<DateOnlyEntity>().CreateTable();

        db.Table<DateOnlyEntity>().Add(new DateOnlyEntity { Id = 1, Date = new DateOnly(2024, 6, 15) });
        DateOnlyEntity result = db.Table<DateOnlyEntity>().First();

        Assert.Equal(new DateOnly(2024, 6, 15), result.Date);
    }

    [Fact]
    public void TimeOnly_StoredAsText_RoundTrip()
    {
        using TestDatabase db = new();
        db.StorageOptions.TimeOnlyStorage = TimeOnlyStorageMode.Text;
        db.Table<TimeOnlyEntity>().CreateTable();

        db.Table<TimeOnlyEntity>().Add(new TimeOnlyEntity { Id = 1, Time = new TimeOnly(14, 30, 45) });
        TimeOnlyEntity result = db.Table<TimeOnlyEntity>().First();

        Assert.Equal(new TimeOnly(14, 30, 45), result.Time);
    }

    [Fact]
    public void DateTimeOffset_TextFormatted_WhereProperty_Throws()
    {
        using TestDatabase db = new();
        db.StorageOptions.DateTimeOffsetStorage = DateTimeOffsetStorageMode.TextFormatted;
        db.Table<DateTimeOffsetEntity>().CreateTable();

        Assert.Throws<NotSupportedException>(() =>
            db.Table<DateTimeOffsetEntity>().Where(e => e.Date.Year == 2024).ToList());
    }

    [Fact]
    public void TimeSpan_Text_WhereProperty_Throws()
    {
        using TestDatabase db = new();
        db.StorageOptions.TimeSpanStorage = TimeSpanStorageMode.Text;
        db.Table<TimeSpanEntity>().CreateTable();

        Assert.Throws<NotSupportedException>(() =>
            db.Table<TimeSpanEntity>().Where(e => e.Duration.Days == 1).ToList());
    }

    [Fact]
    public void DateOnly_StoredAsText_SelectProperty_ReturnsClientSide()
    {
        using TestDatabase db = new();
        db.StorageOptions.DateOnlyStorage = DateOnlyStorageMode.Text;
        db.Table<DateOnlyEntity>().CreateTable();
        db.Table<DateOnlyEntity>().Add(new DateOnlyEntity { Id = 1, Date = new DateOnly(2024, 6, 15) });

        int year = db.Table<DateOnlyEntity>().Select(e => e.Date.Year).First();

        Assert.Equal(2024, year);
    }

    [Fact]
    public void TimeOnly_StoredAsText_SelectProperty_ReturnsClientSide()
    {
        using TestDatabase db = new();
        db.StorageOptions.TimeOnlyStorage = TimeOnlyStorageMode.Text;
        db.Table<TimeOnlyEntity>().CreateTable();
        db.Table<TimeOnlyEntity>().Add(new TimeOnlyEntity { Id = 1, Time = new TimeOnly(14, 30, 45) });

        int hour = db.Table<TimeOnlyEntity>().Select(e => e.Time.Hour).First();

        Assert.Equal(14, hour);
    }

    [Fact]
    public void DateOnly_Text_WhereProperty_Throws()
    {
        using TestDatabase db = new();
        db.StorageOptions.DateOnlyStorage = DateOnlyStorageMode.Text;
        db.Table<DateOnlyEntity>().CreateTable();

        Assert.Throws<NotSupportedException>(() =>
            db.Table<DateOnlyEntity>().Where(e => e.Date.Year == 2024).ToList());
    }

    [Fact]
    public void TimeOnly_Text_WhereProperty_Throws()
    {
        using TestDatabase db = new();
        db.StorageOptions.TimeOnlyStorage = TimeOnlyStorageMode.Text;
        db.Table<TimeOnlyEntity>().CreateTable();

        Assert.Throws<NotSupportedException>(() =>
            db.Table<TimeOnlyEntity>().Where(e => e.Time.Hour == 14).ToList());
    }

    [Fact]
    public void DateTime_TextFormatted_AddDaysInWhere_Throws()
    {
        using TestDatabase db = new();
        db.StorageOptions.DateTimeStorage = DateTimeStorageMode.TextFormatted;
        db.Table<DateTimeEntity>().CreateTable();

        Assert.Throws<NotSupportedException>(() =>
            db.Table<DateTimeEntity>().Where(e => e.Date.AddDays(1) > DateTime.Now).ToList());
    }

    [Fact]
    public void Join_WithComputedMethodCallAssignment_ProducesCorrectSql()
    {
        using TestDatabase db = new();

        SQLiteCommand command = (
            from book in db.Table<Book>()
            join author in db.Table<Author>() on book.AuthorId equals author.Id
            select new ComputedJoinDto
            {
                UpperTitle = book.Title.ToUpper(),
                AuthorName = author.Name
            }
        ).ToSqlCommand();

        Assert.Contains("UPPER", command.CommandText);
    }

    [Fact]
    public void Select_ToRecordWithComputedConstructorArg_ProducesCorrectSql()
    {
        using TestDatabase db = new();

        SQLiteCommand command = db.Table<Book>()
            .Select(b => new SingleStringRecord(b.Title))
            .ToSqlCommand();

        Assert.Contains("BookTitle", command.CommandText);
    }

    [Fact]
    public void Join_WithCapturedQueryableVariable_ProducesSubquery()
    {
        using TestDatabase db = new();

        IQueryable<Author> filteredAuthors = db.Table<Author>().Where(a => a.Id > 0);

        SQLiteCommand command = db.Table<Book>()
            .Join(filteredAuthors, b => b.AuthorId, a => a.Id, (b, a) => new { b.Title, a.Name })
            .ToSqlCommand();

        Assert.Contains("SELECT", command.CommandText);
        Assert.Contains("JOIN", command.CommandText);
    }

    private class DateOnlyEntity
    {
        [Key]
        public int Id { get; set; }
        public DateOnly Date { get; set; }
    }

    private class TimeOnlyEntity
    {
        [Key]
        public int Id { get; set; }
        public TimeOnly Time { get; set; }
    }

    private class DateTimeOffsetEntity
    {
        [Key]
        public int Id { get; set; }
        public DateTimeOffset Date { get; set; }
    }

    private class TimeSpanEntity
    {
        [Key]
        public int Id { get; set; }
        public TimeSpan Duration { get; set; }
    }

    private class DateTimeEntity
    {
        [Key]
        public int Id { get; set; }
        public DateTime Date { get; set; }
    }

    [Fact]
    public void MemberInit_NonSimpleTypeMember_MapsNestedColumns()
    {
        using TestDatabase db = new();
        db.Table<Book>().CreateTable();
        db.Table<Author>().CreateTable();
        db.Table<Author>().Add(new Author { Id = 1, Name = "Alice", Email = "alice@example.com", BirthDate = new DateTime(1980, 1, 1) });
        db.Table<Book>().Add(new Book { Id = 1, Title = "Test Book", AuthorId = 1, Price = 9.99 });

        BookWithAuthorDto result = (
            from b in db.Table<Book>()
            join a in db.Table<Author>() on b.AuthorId equals a.Id
            select new { b, a } into x
            select new BookWithAuthorDto { Title = x.b.Title, Author = x.a }
        ).First();

        Assert.Equal("Test Book", result.Title);
        Assert.Equal("Alice", result.Author.Name);
    }

    private class BookWithAuthorDto
    {
        public string Title { get; set; } = string.Empty;
        public Author Author { get; set; } = null!;
    }

    private class ComputedJoinDto
    {
        public string UpperTitle { get; set; } = string.Empty;
        public string AuthorName { get; set; } = string.Empty;
    }

    [Fact]
    public void Where_WithInlineFieldInitializer_FiltersCorrectly()
    {
        using TestDatabase db = new();
        db.Table<Book>().CreateTable();
        db.Table<Book>().AddRange([
            new Book { Id = 1, Title = "Match", AuthorId = 1, Price = 10 },
            new Book { Id = 2, Title = "Other", AuthorId = 1, Price = 20 }
        ]);

        List<Book> results = db.Table<Book>()
            .Where(b => b.Title == new TitleFilter { Value = "Match" }.Value)
            .ToList();

        Assert.Single(results);
        Assert.Equal("Match", results[0].Title);
    }

    private class TitleFilter
    {
        public string Value = string.Empty;
    }

    private record SingleStringRecord(string Title);

    [Fact]
    public void Select_WithMemberListBinding_PopulatesCollection()
    {
        using TestDatabase db = new();
        db.Table<Book>().CreateTable();
        db.Table<Book>().Add(new Book { Id = 1, Title = "WAL Book", AuthorId = 1, Price = 9.99 });

        BookWithTags result = db.Table<Book>()
            .Where(b => b.Id == 1)
            .Select(b => new BookWithTags
            {
                Title = b.Title,
                Tags = { "fiction", "bestseller" }
            })
            .First();

        Assert.Equal("WAL Book", result.Title);
        Assert.Equal(2, result.Tags.Count);
        Assert.Contains("fiction", result.Tags);
        Assert.Contains("bestseller", result.Tags);
    }

    [Fact]
    public void Select_Chained_WithMemberListBinding_PopulatesCollection()
    {
        using TestDatabase db = new();
        db.Table<Book>().CreateTable();
        db.Table<Book>().Add(new Book { Id = 1, Title = "WAL Book", AuthorId = 1, Price = 9.99 });

        BookWithTags result = db.Table<Book>()
            .Where(b => b.Id == 1)
            .Select(b => b.Title)
            .Select(t => new BookWithTags
            {
                Title = t,
                Tags = { "fiction", "bestseller" }
            })
            .First();

        Assert.Equal("WAL Book", result.Title);
        Assert.Equal(2, result.Tags.Count);
        Assert.Contains("fiction", result.Tags);
        Assert.Contains("bestseller", result.Tags);
    }

    [Fact]
    public void ConstantEnumCastToInt_Where_FiltersCorrectly()
    {
        using TestDatabase db = new();
        db.Table<Book>().CreateTable();
        db.Table<Book>().Add(new Book { Id = 1, Title = "A", AuthorId = 1, Price = 1.0 });
        db.Table<Book>().Add(new Book { Id = 2, Title = "B", AuthorId = 2, Price = 2.0 });

        BookCategory category = BookCategory.Fiction;
        List<Book> results = db.Table<Book>().Where(b => b.AuthorId == (int)category).ToList();

        Assert.Single(results);
        Assert.Equal(1, results[0].Id);
    }

    [Fact]
    public void ConstantLongCastToInt_Where_FiltersCorrectly()
    {
        using TestDatabase db = new();
        db.Table<Book>().CreateTable();
        db.Table<Book>().Add(new Book { Id = 1, Title = "A", AuthorId = 1, Price = 1.0 });
        db.Table<Book>().Add(new Book { Id = 2, Title = "B", AuthorId = 2, Price = 2.0 });

        long id = 1L;
        List<Book> results = db.Table<Book>().Where(b => b.Id == (int)id).ToList();

        Assert.Single(results);
        Assert.Equal(1, results[0].Id);
    }

    [Fact]
    public void CapturedTableVariable_InSubquery_FiltersCorrectly()
    {
        using TestDatabase db = new();
        db.Table<Book>().CreateTable();
        db.Table<Author>().CreateTable();
        db.Table<Author>().Add(new Author { Id = 1, Name = "Alice", Email = "a@a.com", BirthDate = DateTime.Today });
        db.Table<Author>().Add(new Author { Id = 2, Name = "Bob", Email = "b@b.com", BirthDate = DateTime.Today });
        db.Table<Book>().Add(new Book { Id = 1, Title = "Book A", AuthorId = 1, Price = 1.0 });

        var books = db.Table<Book>();
        List<Author> results = db.Table<Author>()
            .Where(a => books.Any(b => b.AuthorId == a.Id))
            .ToList();

        Assert.Single(results);
        Assert.Equal(1, results[0].Id);
    }

    private class BookWithField
    {
        public string Title = string.Empty;
    }

    private enum BookCategory
    {
        Fiction = 1,
        NonFiction = 2
    }

    private class BookWithTags
    {
        public string Title { get; set; } = string.Empty;
        public List<string> Tags { get; } = [];
    }
}
