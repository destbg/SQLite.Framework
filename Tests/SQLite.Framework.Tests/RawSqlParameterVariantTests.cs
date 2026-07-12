using System.Collections.Generic;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class RawSqlParameterVariantTests
{
    [Fact]
    public void DictionaryParameterWithNestedParameterValueBinds()
    {
        using TestDatabase db = new();

        string? actual = db.Query<string>(
            "SELECT @v",
            new Dictionary<string, object?>
            {
                ["@v"] = new SQLiteParameter { Name = "@v", Value = "inner" }
            }).Single();

        Assert.Equal("inner", actual);
    }

    [Fact]
    public void NumberedPositionalParameterNameBinds()
    {
        using TestDatabase db = new();

        string? actual = db.Query<string>("SELECT ?1", new SQLiteParameter { Name = "?1", Value = "pos" }).Single();

        Assert.Equal("pos", actual);
    }

    [Fact]
    public void EmptyParameterNameLeavesValueUnbound()
    {
        using TestDatabase db = new();

        string? actual = db.Query<string?>("SELECT @v", new SQLiteParameter { Name = "", Value = "x" }).Single();

        Assert.Null(actual);
    }

    [Fact]
    public void CommentOnlyFromSqlStatementTailIsAccepted()
    {
        using TestDatabase db = new();
        db.Table<FdMultiBook>().Schema.CreateTable();
        db.Table<FdMultiBook>().Add(new FdMultiBook { Id = 1, Title = "a", Pages = 80 });

        List<int> block = db.FromSql<FdMultiBook>("SELECT * FROM FdMultiBook; /* trailing ; note */").Select(b => b.Id).ToList();
        List<int> unterminated = db.FromSql<FdMultiBook>("SELECT * FROM FdMultiBook; /* trailing ; note").Select(b => b.Id).ToList();
        List<int> bracketed = db.FromSql<FdMultiBook>("SELECT * FROM [FdMultiBook]").Select(b => b.Id).ToList();

        Assert.Equal([1], block);
        Assert.Equal([1], unterminated);
        Assert.Equal([1], bracketed);
    }

    [Fact]
    public void BracketedIdentifierAfterSemicolonFromSqlThrows()
    {
        using TestDatabase db = new();
        db.Table<FdMultiBook>().Schema.CreateTable();

        NotSupportedException ex = Assert.Throws<NotSupportedException>(() =>
            db.FromSql<FdMultiBook>("SELECT * FROM FdMultiBook; DELETE FROM [FdMultiBook]").ToList());

        Assert.Equal("The SQL contains more than one statement, which a query can only run partially. Use Execute for multi-statement batches.", ex.Message);
    }
}
