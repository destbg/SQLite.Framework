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
    }
}
