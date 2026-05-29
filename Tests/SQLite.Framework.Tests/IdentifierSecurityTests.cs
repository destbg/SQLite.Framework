using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class IdentifierSecurityTests
{
    [Fact]
    public void CreateIndex_NameWithDoubleQuote_IsEscapedAndCreated()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<Book>();

        db.Schema.CreateIndex<Book>(b => b.Title, name: "weird\"name");

        Assert.True(db.Schema.IndexExists("weird\"name"));
    }

    [Fact]
    public void CreateIndex_NameWithInjectionAttempt_DoesNotTouchOtherObjects()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<Book>();

        string malicious = "x\") ON \"Books\"(\"BookTitle\"); DROP TABLE \"Books\"; --";
        db.Schema.CreateIndex<Book>(b => b.Title, name: malicious);

        Assert.True(db.Schema.TableExists("Books"));
        Assert.True(db.Schema.IndexExists(malicious));
    }

    [Fact]
    public void TableBuilderIndex_NameWithDoubleQuote_IsEscapedAndCreated()
    {
        using TestDatabase db = new();

        db.Schema.Table<Book>().Index(b => b.Title, name: "tb\"idx").CreateTable();

        Assert.True(db.Schema.IndexExists("tb\"idx"));
    }
}
