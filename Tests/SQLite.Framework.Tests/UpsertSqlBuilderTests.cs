using SQLite.Framework.Internals.Helpers;
using SQLite.Framework.Models;
using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class UpsertSqlBuilderTests
{
    private static string N(string s) => s.Replace("\r\n", "\n");

    [Fact]
    public void Build_ConflictColumnGivenAsSqlColumnName_ResolvesViaColumnLookup()
    {
        using TestDatabase db = new();
        TableMapping mapping = db.TableMapping(typeof(Book));

        UpsertConflictTarget<Book> target = new(["BookId"]);
        target.DoNothing();

        (TableColumn[] _, string sql) = UpsertSqlBuilder.Build(mapping, target, (_, p) => p);

        Assert.Equal(
            N("INSERT INTO \"Books\" (BookId, BookTitle, BookAuthorId, BookPrice) VALUES (@p0, @p1, @p2, @p3) ON CONFLICT (BookId) DO NOTHING"),
            N(sql));
    }

    [Fact]
    public void Build_ConflictColumnUnknown_Throws()
    {
        using TestDatabase db = new();
        TableMapping mapping = db.TableMapping(typeof(Book));

        UpsertConflictTarget<Book> target = new(["NotARealColumnOrProperty"]);
        target.DoNothing();

        Assert.Throws<InvalidOperationException>(() => UpsertSqlBuilder.Build(mapping, target, (_, p) => p));
    }
}
