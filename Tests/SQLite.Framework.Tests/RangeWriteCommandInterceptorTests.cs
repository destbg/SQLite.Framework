using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class RangeWriteCommandInterceptorTests
{
    [Fact]
    public void AddRangeNotifiesInterceptorOfInsert()
    {
        InsertCapture capture = new();
        using TestDatabase db = new(b => b.AddCommandInterceptor(capture));
        db.Table<Book>().Schema.CreateTable();

        capture.Texts.Clear();
        db.Table<Book>().AddRange(new List<Book> { new Book { Id = 1, Title = "a", AuthorId = 1, Price = 1 } });

        Assert.Contains(capture.Texts, t => t.Contains("INSERT", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void UpdateRangeNotifiesInterceptorOfUpdate()
    {
        InsertCapture capture = new();
        using TestDatabase db = new(b => b.AddCommandInterceptor(capture));
        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().Add(new Book { Id = 1, Title = "a", AuthorId = 1, Price = 1 });

        capture.Texts.Clear();
        db.Table<Book>().UpdateRange(new List<Book> { new Book { Id = 1, Title = "b", AuthorId = 1, Price = 2 } });

        Assert.Contains(capture.Texts, t => t.Contains("UPDATE", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void RemoveRangeNotifiesInterceptorOfDelete()
    {
        InsertCapture capture = new();
        using TestDatabase db = new(b => b.AddCommandInterceptor(capture));
        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().Add(new Book { Id = 1, Title = "a", AuthorId = 1, Price = 1 });

        capture.Texts.Clear();
        db.Table<Book>().RemoveRange(new List<Book> { new Book { Id = 1, Title = "a", AuthorId = 1, Price = 1 } });

        Assert.Contains(capture.Texts, t => t.Contains("DELETE", StringComparison.OrdinalIgnoreCase));
    }

    private sealed class InsertCapture : ISQLiteCommandInterceptor
    {
        public List<string> Texts { get; } = [];

        public void OnExecuting(SQLiteCommand command)
        {
            Texts.Add(command.CommandText);
        }

        public void OnExecuted(SQLiteCommand command, int? rowsAffected)
        {
        }

        public void OnFailed(SQLiteCommand command, Exception exception)
        {
        }

        public void OnRowRead(SQLiteCommand command, SQLiteDataReader reader)
        {
        }
    }
}
