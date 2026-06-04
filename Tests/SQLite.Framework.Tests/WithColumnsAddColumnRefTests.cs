using SQLite.Framework.Enums;
using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class WithColumnsAddColumnRefTests
{
    [Fact]
    public void WithColumnsAddValueExpressionReferencingColumnThrows()
    {
        using ModelTestDatabase db = new(model => model.Entity<Book>().Column("Slug", SQLiteColumnType.Text));
        db.Schema.CreateTable<Book>();

        Assert.Throws<NotSupportedException>(() =>
            db.Table<Book>()
                .WithColumns(c => c.Set(b => SQLiteColumn.Of<string>(b, "Slug"), b => b.Title))
                .Add(new Book { Id = 1, Title = "Real Title", AuthorId = 1, Price = 1 }));
    }
}
