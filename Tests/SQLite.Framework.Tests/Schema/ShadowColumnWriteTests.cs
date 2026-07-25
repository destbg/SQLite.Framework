using SQLite.Framework.Enums;
using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

file enum ShadowEnum
{
    Zero = 0,
    Two = 2,
}

public class ShadowColumnWriteTests
{
    [Fact]
    public void WithColumnsEnumValueIsWrittenAsUnderlyingInteger()
    {
        using ModelTestDatabase db = new(
            model => model.Entity<Book>().Column("EnumCol", SQLiteColumnType.Integer));
        db.Schema.CreateTable<Book>();

        db.Table<Book>()
            .WithColumns(c => c.Set(b => SQLiteColumn.Of<ShadowEnum>(b, "EnumCol"), ShadowEnum.Two))
            .Add(new Book { Id = 1, Title = "x", AuthorId = 1, Price = 1 });

        long stored = db.Table<Book>()
            .Select(b => SQLiteColumn.Of<long>(b, "EnumCol"))
            .Single();

        Assert.Equal(2L, stored);
    }

    [Fact]
    public void WithColumnsValueIsWrittenWhenColumnHookIsAlsoRegistered()
    {
        using ModelTestDatabase db = new(
            model => model.Entity<Book>()
                .Column("HookCol", SQLiteColumnType.Integer)
                .Column("WithCol", SQLiteColumnType.Integer),
            options => options.OnAdd<Book>((_, _, columns) =>
            {
                columns["HookCol"] = 1L;
                return true;
            }));
        db.Schema.CreateTable<Book>();

        db.Table<Book>()
            .WithColumns(c => c.Set(b => SQLiteColumn.Of<long>(b, "WithCol"), 2L))
            .Add(new Book { Id = 1, Title = "x", AuthorId = 1, Price = 1 });

        var row = db.Table<Book>()
            .Select(b => new
            {
                Hook = SQLiteColumn.Of<long>(b, "HookCol"),
                With = SQLiteColumn.Of<long>(b, "WithCol")
            })
            .Single();

        Assert.Equal(1L, row.Hook);
        Assert.Equal(2L, row.With);
    }
}
