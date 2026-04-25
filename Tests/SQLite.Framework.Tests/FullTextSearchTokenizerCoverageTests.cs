using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class FullTextSearchTokenizerCoverageTests
{
    private static string N(string? s) => (s ?? string.Empty).Replace("\r\n", "\n");

    private static string ReadSchema(TestDatabase db, string name)
    {
        return N(db.ExecuteScalar<string>($"SELECT sql FROM sqlite_master WHERE name = '{name}'"));
    }

    [Fact]
    public void Ascii_NoOptions_RendersBareTokenizerName()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<Ascii_Default_Search>();

        Assert.Equal(
            """CREATE VIRTUAL TABLE "Ascii_Default_Search" USING fts5(Body, tokenize='ascii')""",
            ReadSchema(db, "Ascii_Default_Search"));
    }

    [Fact]
    public void Ascii_SeparatorsOnly_RendersSeparatorsArg()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<Ascii_Separators_Search>();

        Assert.Equal(
            """CREATE VIRTUAL TABLE "Ascii_Separators_Search" USING fts5(Body, tokenize='ascii separators '';|''')""",
            ReadSchema(db, "Ascii_Separators_Search"));
    }

    [Fact]
    public void Ascii_TokenCharsOnly_RendersTokenCharsArg()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<Ascii_TokenChars_Search>();

        Assert.Equal(
            """CREATE VIRTUAL TABLE "Ascii_TokenChars_Search" USING fts5(Body, tokenize='ascii tokenchars ''-_''')""",
            ReadSchema(db, "Ascii_TokenChars_Search"));
    }

    [Fact]
    public void Ascii_BothOptions_RendersBothArgs()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<Ascii_Both_Search>();

        Assert.Equal(
            """CREATE VIRTUAL TABLE "Ascii_Both_Search" USING fts5(Body, tokenize='ascii separators '';|'' tokenchars ''-_''')""",
            ReadSchema(db, "Ascii_Both_Search"));
    }

    [Fact]
    public void Porter_DefaultBase_WrapsUnicode61WithRemoveDiacritics()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<Porter_Default_Search>();

        Assert.Equal(
            """CREATE VIRTUAL TABLE "Porter_Default_Search" USING fts5(Body, tokenize='porter unicode61 remove_diacritics 2')""",
            ReadSchema(db, "Porter_Default_Search"));
    }

    [Fact]
    public void Porter_AsciiBase_WrapsAsciiWithoutUnicodeOptions()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<Porter_Ascii_Search>();

        Assert.Equal(
            """CREATE VIRTUAL TABLE "Porter_Ascii_Search" USING fts5(Body, tokenize='porter ascii')""",
            ReadSchema(db, "Porter_Ascii_Search"));
    }

    [Fact]
    public void Porter_Categories_RendersCategoriesArg()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<Porter_Categories_Search>();

        Assert.Equal(
            """CREATE VIRTUAL TABLE "Porter_Categories_Search" USING fts5(Body, tokenize='porter unicode61 remove_diacritics 2 categories ''L* N*''')""",
            ReadSchema(db, "Porter_Categories_Search"));
    }

    [Fact]
    public void Porter_Separators_RendersSeparatorsArg()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<Porter_Separators_Search>();

        Assert.Equal(
            """CREATE VIRTUAL TABLE "Porter_Separators_Search" USING fts5(Body, tokenize='porter unicode61 remove_diacritics 2 separators ''.,;''')""",
            ReadSchema(db, "Porter_Separators_Search"));
    }

    [Fact]
    public void Porter_TokenChars_RendersTokenCharsArg()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<Porter_TokenChars_Search>();

        Assert.Equal(
            """CREATE VIRTUAL TABLE "Porter_TokenChars_Search" USING fts5(Body, tokenize='porter unicode61 remove_diacritics 2 tokenchars ''-_''')""",
            ReadSchema(db, "Porter_TokenChars_Search"));
    }

    [Fact]
    public void Porter_StemsEnglishWords_RoundTrip()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<Porter_Default_Search>();
        db.CreateCommand("INSERT INTO Porter_Default_Search(rowid, Body) VALUES (1, 'running')", []).ExecuteNonQuery();

        long hits = db.Table<Porter_Default_Search>()
            .LongCount(c => SQLiteFunctions.Match(c, f => f.Term("run")));

        Assert.Equal(1, hits);
    }

    [Fact]
    public void Custom_NameOnly_RendersTokenizerNameAlone()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<Custom_NoArgs_Search>();

        Assert.Equal(
            """CREATE VIRTUAL TABLE "Custom_NoArgs_Search" USING fts5(Body, tokenize='ascii')""",
            ReadSchema(db, "Custom_NoArgs_Search"));
    }

    [Fact]
    public void Custom_PlainArgs_RendersBareArgs()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<Custom_PlainArgs_Search>();

        Assert.Equal(
            """CREATE VIRTUAL TABLE "Custom_PlainArgs_Search" USING fts5(Body, tokenize='unicode61 remove_diacritics 1')""",
            ReadSchema(db, "Custom_PlainArgs_Search"));
    }

    [Fact]
    public void Custom_ArgsWithSpecialChars_GetSingleQuoted()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<Custom_QuotedArgs_Search>();

        Assert.Equal(
            """CREATE VIRTUAL TABLE "Custom_QuotedArgs_Search" USING fts5(Body, tokenize='unicode61 categories ''L* N*'' separators ''.,''')""",
            ReadSchema(db, "Custom_QuotedArgs_Search"));
    }

    [Fact]
    public void Custom_EmptyArg_GetsQuotedAsEmptyString()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<Custom_EmptyArg_Search>();

        Assert.Equal(
            """CREATE VIRTUAL TABLE "Custom_EmptyArg_Search" USING fts5(Body, tokenize='ascii tokenchars ''''')""",
            ReadSchema(db, "Custom_EmptyArg_Search"));
    }

    [Fact]
    public void Custom_ArgWithSingleQuote_DoublesItInsideQuotedArg()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<Custom_SingleQuoteArg_Search>();

        Assert.Equal(
            """CREATE VIRTUAL TABLE "Custom_SingleQuoteArg_Search" USING fts5(Body, tokenize='ascii tokenchars ''a''''b''')""",
            ReadSchema(db, "Custom_SingleQuoteArg_Search"));
    }

    [Fact]
    public void Custom_ArgWithDoubleQuote_PassesThroughUnescaped()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<Custom_DoubleQuoteArg_Search>();

        Assert.Equal(
            """CREATE VIRTUAL TABLE "Custom_DoubleQuoteArg_Search" USING fts5(Body, tokenize='ascii tokenchars ''a"b''')""",
            ReadSchema(db, "Custom_DoubleQuoteArg_Search"));
    }

    [Fact]
    public void Custom_ArgWithBacktick_PassesThroughUnescaped()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<Custom_BacktickArg_Search>();

        Assert.Equal(
            """CREATE VIRTUAL TABLE "Custom_BacktickArg_Search" USING fts5(Body, tokenize='ascii tokenchars ''a`b''')""",
            ReadSchema(db, "Custom_BacktickArg_Search"));
    }

    [Fact]
    public void Validation_TwoTokenizerAttributes_Throws()
    {
        using TestDatabase db = new();

        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() =>
            db.Schema.CreateTable<Bad_TwoTokenizers_Search>());

        Assert.Contains("more than one tokenizer attribute", ex.Message);
    }

    [Fact]
    public void Validation_ExternalWithoutContentTable_Throws()
    {
        using TestDatabase db = new();

        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() =>
            db.Schema.CreateTable<Bad_ExternalNoContentTable_Search>());

        Assert.Contains("ContentMode.External but does not set ContentTable", ex.Message);
    }

    [Fact]
    public void Validation_TwoFullTextRowIds_Throws()
    {
        using TestDatabase db = new();

        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() =>
            db.Schema.CreateTable<Bad_TwoRowIds_Search>());

        Assert.Contains("more than one [FullTextRowId] property", ex.Message);
    }

    [Fact]
    public void Validation_StringFullTextRowId_Throws()
    {
        using TestDatabase db = new();

        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() =>
            db.Schema.CreateTable<Bad_StringRowId_Search>());

        Assert.Contains("not int or long", ex.Message);
    }

    [Fact]
    public void Validation_NoIndexedColumns_Throws()
    {
        using TestDatabase db = new();

        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() =>
            db.Schema.CreateTable<Bad_NoIndexedColumns_Search>());

        Assert.Contains("must have at least one property marked [FullTextIndexed]", ex.Message);
    }
}
