using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class CommandInterceptorTests
{
    [Fact]
    public void NoInterceptor_DoesNotAffectQueries()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().Add(new Book { Id = 1, Title = "T", AuthorId = 1, Price = 1 });

        List<Book> books = db.Table<Book>().ToList();

        Assert.Single(books);
    }

    [Fact]
    public void Interceptor_FiresExecutingThenExecuted_OnNonQuery()
    {
        Capture capture = new();
        using TestDatabase db = new(b => b.AddCommandInterceptor(capture));
        db.Table<Book>().Schema.CreateTable();

        capture.Reset();
        db.Execute("DELETE FROM Books");

        Assert.Single(capture.ExecutingTexts);
        Assert.Single(capture.ExecutedTexts);
        Assert.Empty(capture.FailedTexts);
        Assert.Contains("DELETE FROM Books", capture.ExecutingTexts[0]);
        Assert.Equal(0, capture.ExecutedRows[0]);
    }

    [Fact]
    public void Interceptor_FiresExecutingThenExecuted_OnReader()
    {
        Capture capture = new();
        using TestDatabase db = new(b => b.AddCommandInterceptor(capture));
        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().Add(new Book { Id = 1, Title = "T", AuthorId = 1, Price = 1 });

        capture.Reset();
        List<Book> books = db.Table<Book>().ToList();

        Assert.NotEmpty(capture.ExecutingTexts);
        Assert.NotEmpty(capture.ExecutedTexts);
        Assert.Empty(capture.FailedTexts);
        Assert.Null(capture.ExecutedRows.Last());
    }

    [Fact]
    public void Interceptor_FiresFailed_OnSyntaxError()
    {
        Capture capture = new();
        using TestDatabase db = new(b => b.AddCommandInterceptor(capture));

        Assert.ThrowsAny<Exception>(() => db.Execute("THIS IS NOT VALID SQL"));

        Assert.Single(capture.ExecutingTexts);
        Assert.Empty(capture.ExecutedTexts);
        Assert.Single(capture.FailedTexts);
    }

    [Fact]
    public void LogCommands_MasksParametersByDefault()
    {
        List<string> log = [];
        using TestDatabase db = new(b => b.LogCommands(log.Add));
        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().Add(new Book { Id = 1, Title = "Hidden", AuthorId = 7, Price = 99 });

        Assert.Contains(log, line => line.Contains("@p0=?") && line.Contains("@p1=?"));
        Assert.DoesNotContain(log, line => line.Contains("Hidden"));
    }

    [Fact]
    public void LogCommands_WithSensitiveParameterLogging_InlinesValues()
    {
        List<string> log = [];
        using TestDatabase db = new(b => b
            .LogCommands(log.Add)
            .EnableSensitiveParameterLogging());
        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().Add(new Book { Id = 1, Title = "Hidden", AuthorId = 7, Price = 99 });

        Assert.Contains(log, line => line.Contains("'Hidden'"));
        Assert.Contains(log, line => line.Contains("=7"));
    }

    [Fact]
    public void LogCommands_IncludesElapsedAndRowCount()
    {
        List<string> log = [];
        using TestDatabase db = new(b => b.LogCommands(log.Add));
        db.Table<Book>().Schema.CreateTable();

        Assert.Contains(log, line => line.Contains("ms") && line.Contains("rows"));
    }

    [Fact]
    public void LogCommands_SensitiveLogging_FormatsValueShapes()
    {
        List<string> log = [];
        using TestDatabase db = new(b => b
            .LogCommands(log.Add)
            .EnableSensitiveParameterLogging());

        SQLiteCommand cmd = db.CreateCommand("SELECT @s, @b, @bf, @blob, @nil",
        [
            new SQLiteParameter { Name = "@s", Value = "with 'quote'" },
            new SQLiteParameter { Name = "@b", Value = true },
            new SQLiteParameter { Name = "@bf", Value = false },
            new SQLiteParameter { Name = "@blob", Value = new byte[] { 1, 2, 3 } },
            new SQLiteParameter { Name = "@nil", Value = null },
        ]);
        using SQLiteDataReader reader = cmd.ExecuteReader();
        while (reader.Read())
        {
        }

        string line = log.Single(l => l.Contains("SELECT @s"));
        Assert.Contains("@s='with ''quote'''", line);
        Assert.Contains("@b=1", line);
        Assert.Contains("@bf=0", line);
        Assert.Contains("@blob=<3 bytes>", line);
        Assert.Contains("@nil=NULL", line);
    }

    [Fact]
    public void LogCommands_FormatsFailedCommandsWithExceptionType()
    {
        List<string> log = [];
        using TestDatabase db = new(b => b.LogCommands(log.Add));

        Assert.ThrowsAny<Exception>(() => db.Execute("BAD SQL"));

        Assert.Contains(log, line => line.Contains("FAILED:") && line.Contains("BAD SQL"));
    }

    [Fact]
    public void MultipleInterceptors_FireInRegistrationOrder()
    {
        List<string> order = [];
        OrderedInterceptor first = new("first", order);
        OrderedInterceptor second = new("second", order);

        using TestDatabase db = new(b => b
            .AddCommandInterceptor(first)
            .AddCommandInterceptor(second));
        db.Table<Book>().Schema.CreateTable();
        order.Clear();

        db.Execute("DELETE FROM Books");

        Assert.Equal(["first:executing", "second:executing", "first:executed", "second:executed"], order);
    }

    [Fact]
    public void CommandId_IsAssignedAtCreation_AndIncreases()
    {
        using TestDatabase db = new();

        SQLiteCommand first = db.CreateCommand("SELECT 1", []);
        SQLiteCommand second = db.CreateCommand("SELECT 2", []);

        Assert.True(first.Id > 0);
        Assert.True(second.Id > first.Id);
    }

    [Fact]
    public void OnRowRead_FiresOncePerRow_WithTheCommandId()
    {
        RowCapture capture = new();
        using TestDatabase db = new(b => b.AddCommandInterceptor(capture));
        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().AddRange(
        [
            new Book { Id = 1, Title = "A", AuthorId = 1, Price = 1 },
            new Book { Id = 2, Title = "B", AuthorId = 1, Price = 2 },
            new Book { Id = 3, Title = "C", AuthorId = 1, Price = 3 },
        ]);

        capture.Reset();
        List<Book> books = db.Table<Book>().ToList();

        Assert.Equal(3, books.Count);
        Assert.Equal(3, capture.RowIds.Count);
        Assert.All(capture.RowIds, id => Assert.Equal(capture.RowIds[0], id));
        Assert.Contains(capture.RowIds[0], capture.ExecutingIds);
    }

    [Fact]
    public void OnRowRead_ExposesReturnedRowData()
    {
        ValueCapture capture = new();
        using TestDatabase db = new(b => b.AddCommandInterceptor(capture));

        using (SQLiteDataReader reader = db.CreateCommand("SELECT 42", []).ExecuteReader())
        {
            while (reader.Read())
            {
            }
        }

        Assert.Equal([42L], capture.Values);
    }

    [Fact]
    public void LogCommands_IncludesCommandId()
    {
        List<string> log = [];
        using TestDatabase db = new(b => b.LogCommands(log.Add));
        db.Table<Book>().Schema.CreateTable();

        Assert.Contains(log, line => line.StartsWith("#"));
    }

    [Fact]
    public void OnReaderClosing_FiresOnceOnDispose_WithRowsRead()
    {
        RowCapture capture = new();
        using TestDatabase db = new(b => b.AddCommandInterceptor(capture));
        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().AddRange(
        [
            new Book { Id = 1, Title = "A", AuthorId = 1, Price = 1 },
            new Book { Id = 2, Title = "B", AuthorId = 1, Price = 2 },
            new Book { Id = 3, Title = "C", AuthorId = 1, Price = 3 },
        ]);

        capture.Reset();
        List<Book> books = db.Table<Book>().ToList();

        Assert.Equal(3, books.Count);
        Assert.Equal(3, capture.ClosingReadCounts.Single());
        Assert.Contains(capture.ClosingIds[0], capture.ExecutingIds);
    }

    [Fact]
    public void OnReaderClosing_PartialRead_ReportsRowsActuallyRead()
    {
        RowCapture capture = new();
        using TestDatabase db = new(b => b.AddCommandInterceptor(capture));
        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().AddRange(
        [
            new Book { Id = 1, Title = "A", AuthorId = 1, Price = 1 },
            new Book { Id = 2, Title = "B", AuthorId = 1, Price = 2 },
            new Book { Id = 3, Title = "C", AuthorId = 1, Price = 3 },
        ]);

        capture.Reset();
        Book first = db.Table<Book>().OrderBy(b => b.Id).First();

        Assert.Equal(1, first.Id);
        Assert.Equal(1, capture.ClosingReadCounts.Single());
    }

    private sealed class Capture : ISQLiteCommandInterceptor
    {
        public List<string> ExecutingTexts { get; } = [];
        public List<string> ExecutedTexts { get; } = [];
        public List<int?> ExecutedRows { get; } = [];
        public List<string> FailedTexts { get; } = [];

        public void Reset()
        {
            ExecutingTexts.Clear();
            ExecutedTexts.Clear();
            ExecutedRows.Clear();
            FailedTexts.Clear();
        }

        public void OnExecuting(SQLiteCommand command)
        {
            ExecutingTexts.Add(command.CommandText);
        }

        public void OnExecuted(SQLiteCommand command, int? rowsAffected)
        {
            ExecutedTexts.Add(command.CommandText);
            ExecutedRows.Add(rowsAffected);
        }

        public void OnFailed(SQLiteCommand command, Exception exception)
        {
            FailedTexts.Add(command.CommandText);
        }

        public void OnRowRead(SQLiteCommand command, SQLiteDataReader reader)
        {
        }

        public void OnReaderClosing(SQLiteCommand command, SQLiteDataReader reader, int readCount)
        {
        }
    }

    private sealed class OrderedInterceptor : ISQLiteCommandInterceptor
    {
        private readonly string name;
        private readonly List<string> order;

        public OrderedInterceptor(string name, List<string> order)
        {
            this.name = name;
            this.order = order;
        }

        public void OnExecuting(SQLiteCommand command) => order.Add($"{name}:executing");
        public void OnExecuted(SQLiteCommand command, int? rowsAffected) => order.Add($"{name}:executed");
        public void OnFailed(SQLiteCommand command, Exception exception) => order.Add($"{name}:failed");
        public void OnRowRead(SQLiteCommand command, SQLiteDataReader reader) { }
        public void OnReaderClosing(SQLiteCommand command, SQLiteDataReader reader, int readCount) => order.Add($"{name}:closing");
    }

    private sealed class RowCapture : ISQLiteCommandInterceptor
    {
        public List<long> ExecutingIds { get; } = [];
        public List<long> RowIds { get; } = [];
        public List<long> ClosingIds { get; } = [];
        public List<int> ClosingReadCounts { get; } = [];

        public void Reset()
        {
            ExecutingIds.Clear();
            RowIds.Clear();
            ClosingIds.Clear();
            ClosingReadCounts.Clear();
        }

        public void OnExecuting(SQLiteCommand command) => ExecutingIds.Add(command.Id);
        public void OnExecuted(SQLiteCommand command, int? rowsAffected) { }
        public void OnFailed(SQLiteCommand command, Exception exception) { }
        public void OnRowRead(SQLiteCommand command, SQLiteDataReader reader) => RowIds.Add(command.Id);

        public void OnReaderClosing(SQLiteCommand command, SQLiteDataReader reader, int readCount)
        {
            ClosingIds.Add(command.Id);
            ClosingReadCounts.Add(readCount);
        }
    }

    private sealed class ValueCapture : ISQLiteCommandInterceptor
    {
        public List<long> Values { get; } = [];

        public void OnExecuting(SQLiteCommand command) { }
        public void OnExecuted(SQLiteCommand command, int? rowsAffected) { }
        public void OnFailed(SQLiteCommand command, Exception exception) { }
        public void OnRowRead(SQLiteCommand command, SQLiteDataReader reader) => Values.Add(reader.GetInt64(0));
        public void OnReaderClosing(SQLiteCommand command, SQLiteDataReader reader, int readCount) { }
    }
}
