using SQLite.Framework.Enums;
using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class WithColumnsUpdateHookTests
{
    [Fact]
    public void WithColumnsUpdateAddsOwnColumnAndSkipsHookOverriddenColumn()
    {
        using ModelTestDatabase db = new(
            model => model.Entity<Book>()
                .Column("HookCol", SQLiteColumnType.Integer)
                .Column("WithCol", SQLiteColumnType.Integer)
                .Column("Shared", SQLiteColumnType.Integer),
            options => options.OnUpdate<Book>((_, _, columns) =>
            {
                columns["HookCol"] = 1L;
                columns["Shared"] = 2L;
                return true;
            }));
        db.Schema.CreateTable<Book>();
        db.Table<Book>().Add(new Book { Id = 1, Title = "x", AuthorId = 1, Price = 1 });

        db.Table<Book>()
            .WithColumns(c => c
                .Set(b => SQLiteColumn.Of<long>(b, "WithCol"), 3L)
                .Set(b => SQLiteColumn.Of<long>(b, "Shared"), 9L))
            .Update(new Book { Id = 1, Title = "y", AuthorId = 1, Price = 2 });

        var row = db.Table<Book>()
            .Select(b => new
            {
                Hook = SQLiteColumn.Of<long>(b, "HookCol"),
                With = SQLiteColumn.Of<long>(b, "WithCol"),
                Shared = SQLiteColumn.Of<long>(b, "Shared")
            })
            .Single();

        Assert.Equal(1L, row.Hook);
        Assert.Equal(3L, row.With);
        Assert.Equal(2L, row.Shared);
    }
}
