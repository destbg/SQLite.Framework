using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class PrintfFormatCapturedArgsTests
{
    private static TestDatabase CreateDb()
    {
        TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().Add(new Book { Id = 1, Title = "x", AuthorId = 1, Price = 1 });
        return db;
    }

    [Fact]
    public void Printf_CapturedArray_MatchesInlineAndContract()
    {
        using TestDatabase db = CreateDb();
        object[] args = [7, "Alpha"];

        string captured = db.Table<Book>().Select(b => SQLiteFunctions.Printf("Book %d: %s", args)).First();
        string inline = db.Table<Book>().Select(b => SQLiteFunctions.Printf("Book %d: %s", 7, "Alpha")).First();

        Assert.Equal("Book 7: Alpha", captured);
        Assert.Equal(inline, captured);
    }

    [Fact]
    public void Format_CapturedArray_MatchesInlineAndContract()
    {
        using TestDatabase db = CreateDb();
        object[] args = [42];

        string captured = db.Table<Book>().Select(b => SQLiteFunctions.Format("N=%d", args)).First();
        string inline = db.Table<Book>().Select(b => SQLiteFunctions.Format("N=%d", 42)).First();

        Assert.Equal("N=42", captured);
        Assert.Equal(inline, captured);
    }

    [Fact]
    public void Printf_CapturedArray_MixedTypes()
    {
        using TestDatabase db = CreateDb();
        object[] args = [3, "kg", 5];

        string captured = db.Table<Book>().Select(b => SQLiteFunctions.Printf("%d %s x%d", args)).First();

        Assert.Equal("3 kg x5", captured);
    }

    [Fact]
    public void Printf_EmptyCapturedArray_LeavesFormatLiteral()
    {
        using TestDatabase db = CreateDb();
        object[] args = [];

        string captured = db.Table<Book>().Select(b => SQLiteFunctions.Printf("plain text", args)).First();

        Assert.Equal("plain text", captured);
    }
}
