using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class StringConcatJoinNullOracleBugTests
{
    private static TestDatabase Seed()
    {
        TestDatabase db = new();
        db.Table<NullableStringEntity>().Schema.CreateTable();
        db.CreateCommand("INSERT INTO NullableStringEntity (\"Id\",\"Name\") VALUES (1,NULL),(2,'x')", []).ExecuteNonQuery();
        return db;
    }

    [Fact]
    public void ConcatWithNullColumnTreatsNullAsEmpty()
    {
        using TestDatabase db = Seed();

        List<string> actual = db.Table<NullableStringEntity>().OrderBy(x => x.Id).Select(x => string.Concat(x.Name, "!")).ToList();
        List<string> oracle = [string.Concat((string?)null, "!"), string.Concat("x", "!")];

        Assert.Equal(oracle, actual);
    }

    [Fact]
    public void ConcatTwoNullColumnsTreatsNullAsEmpty()
    {
        using TestDatabase db = Seed();

        string actual = db.Table<NullableStringEntity>().Where(x => x.Id == 1).Select(x => string.Concat(x.Name, x.Name)).First();
        string oracle = string.Concat((string?)null, (string?)null);

        Assert.Equal(oracle, actual);
    }

    [Fact]
    public void JoinWithNullColumnTreatsNullAsEmpty()
    {
        using TestDatabase db = Seed();

        List<string> actual = db.Table<NullableStringEntity>().OrderBy(x => x.Id).Select(x => string.Join("-", new[] { x.Name, "b" })).ToList();
        List<string> oracle =
        [
            string.Join("-", new[] { (string?)null, "b" }),
            string.Join("-", new[] { "x", "b" })
        ];

        Assert.Equal(oracle, actual);
    }

    [Fact]
    public void JoinAllNullColumnsTreatsNullAsEmpty()
    {
        using TestDatabase db = Seed();

        string actual = db.Table<NullableStringEntity>().Where(x => x.Id == 1).Select(x => string.Join("-", new[] { x.Name, x.Name })).First();
        string oracle = string.Join("-", new[] { (string?)null, (string?)null });

        Assert.Equal(oracle, actual);
    }

    [Fact]
    public void ConcatNonNullColumnUnaffected()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().Add(new Book { Id = 1, Title = "abc", AuthorId = 1, Price = 1 });

        string actual = db.Table<Book>().Select(x => string.Concat(x.Title, "!")).First();
        string oracle = string.Concat("abc", "!");

        Assert.Equal(oracle, actual);
    }
}
