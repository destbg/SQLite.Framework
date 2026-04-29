using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class FullTextSearchTokenizerEscapingTests
{
    private static string N(string? s) => (s ?? string.Empty).Replace("\r\n", "\n");

    [Fact]
    public void Tokenizer_BareIdentifierArg_NotQuotedInSql()
    {
        using TestDatabase db = new();
        db.Table<TokenChars_Underscore_Search>().Schema.CreateTable();

        string? sql = db.ExecuteScalar<string>("SELECT sql FROM sqlite_master WHERE name = 'TokenChars_Underscore_Search'");

        Assert.Equal(
            """CREATE VIRTUAL TABLE "TokenChars_Underscore_Search" USING fts5(Body, tokenize='unicode61 remove_diacritics 2 tokenchars _')""",
            N(sql));
    }

    [Fact]
    public void Tokenizer_PunctuationArg_GetsSingleQuotedAndEscaped()
    {
        using TestDatabase db = new();
        db.Table<Separators_Punct_Search>().Schema.CreateTable();

        string? sql = db.ExecuteScalar<string>("SELECT sql FROM sqlite_master WHERE name = 'Separators_Punct_Search'");

        Assert.Equal(
            """CREATE VIRTUAL TABLE "Separators_Punct_Search" USING fts5(Body, tokenize='unicode61 remove_diacritics 2 separators ''.,;''')""",
            N(sql));
    }

    [Fact]
    public void Tokenizer_ArgWithSpaces_GetsSingleQuoted()
    {
        using TestDatabase db = new();
        db.Table<Categories_Letters_Search>().Schema.CreateTable();

        string? sql = db.ExecuteScalar<string>("SELECT sql FROM sqlite_master WHERE name = 'Categories_Letters_Search'");

        Assert.Equal(
            """CREATE VIRTUAL TABLE "Categories_Letters_Search" USING fts5(Body, tokenize='unicode61 remove_diacritics 2 categories ''L* N* Co''')""",
            N(sql));
    }

    [Fact]
    public void Tokenizer_TokenCharsUnderscore_KeepsUnderscoreInTokens()
    {
        using TestDatabase db = new();
        db.Table<TokenChars_Underscore_Search>().Schema.CreateTable();
        db.CreateCommand("INSERT INTO TokenChars_Underscore_Search(rowid, Body) VALUES (1, 'hello_world standalone')", []).ExecuteNonQuery();

        long hits = db.Table<TokenChars_Underscore_Search>()
            .LongCount(c => SQLiteFTS5Functions.Match(c, f => f.Term("hello_world")));

        Assert.Equal(1, hits);
    }

    [Fact]
    public void Tokenizer_SeparatorsPunct_TreatsPunctAsBoundary()
    {
        using TestDatabase db = new();
        db.Table<Separators_Punct_Search>().Schema.CreateTable();
        db.CreateCommand("INSERT INTO Separators_Punct_Search(rowid, Body) VALUES (1, 'apple,banana;cherry')", []).ExecuteNonQuery();

        long hitsBanana = db.Table<Separators_Punct_Search>()
            .LongCount(c => SQLiteFTS5Functions.Match(c, f => f.Term("banana")));
        long hitsCherry = db.Table<Separators_Punct_Search>()
            .LongCount(c => SQLiteFTS5Functions.Match(c, f => f.Term("cherry")));

        Assert.Equal(1, hitsBanana);
        Assert.Equal(1, hitsCherry);
    }
}
