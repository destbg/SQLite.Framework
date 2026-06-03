using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class StringFunctionSqliteSemanticsTests
{
    [Fact]
    public void ReplaceWithEmptyOldValue_MatchesSqliteReplace()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().Add(new Book { Id = 1, Title = "abc", AuthorId = 1, Price = 1 });

        string framework = db.Table<Book>().Where(b => b.Id == 1).Select(b => b.Title.Replace("", "X")).First();
        string oracle = db.ExecuteScalar<string>(
            "SELECT REPLACE(\"BookTitle\", '', 'X') FROM \"Books\" WHERE \"BookId\" = 1")!;

        Assert.Equal(oracle, framework);
    }

    [Fact]
    public void SubstringWithNegativeCount_MatchesSqliteSubstr()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().Add(new Book { Id = 1, Title = "hello", AuthorId = 1, Price = 1 });

        string framework = db.Table<Book>().Where(b => b.Id == 1).Select(b => b.Title.Substring(1, -3)).First();
        string oracle = db.ExecuteScalar<string>(
            "SELECT SUBSTR(\"BookTitle\", 2, -3) FROM \"Books\" WHERE \"BookId\" = 1")!;

        Assert.Equal(oracle, framework);
    }
}
