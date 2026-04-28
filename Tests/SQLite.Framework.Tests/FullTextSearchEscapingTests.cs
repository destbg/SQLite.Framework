using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class FullTextSearchEscapingTests
{
    private static string N(string s) => s.Replace("\r\n", "\n");

    private static string MatchValue(SQLiteCommand command)
    {
        return (string)command.Parameters[0].Value!;
    }

    private static SQLiteCommand BuildMatch(TestDatabase db, Func<IQueryable<ArticleSearch>, IQueryable<ArticleSearch>> shape)
    {
        return shape(db.Table<ArticleSearch>()).ToSqlCommand();
    }

    private static long Run(TestDatabase db, Func<IQueryable<ArticleSearch>, IQueryable<ArticleSearch>> shape)
    {
        return shape(db.Table<ArticleSearch>()).LongCount();
    }

    private static TestDatabase OpenDb(string method)
    {
        TestDatabase db = new(null, method);
        db.Schema.CreateTable<Article>();
        db.Schema.CreateTable<ArticleSearch>();
        return db;
    }

    private static void Seed(TestDatabase db, string title, string body)
    {
        db.Table<Article>().Add(new Article
        {
            Title = title,
            Body = body,
            PublishedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
        });
    }

    [Fact]
    public void Term_BareAlphanumeric_EmitsBareAndMatches()
    {
        using TestDatabase db = OpenDb(nameof(Term_BareAlphanumeric_EmitsBareAndMatches));
        Seed(db, "title", "native aot apps");

        Func<IQueryable<ArticleSearch>, IQueryable<ArticleSearch>> shape = q => q.Where(a => SQLiteFTS5Functions.Match(a, f => f.Term("aot")));

        Assert.Equal("aot", MatchValue(BuildMatch(db, shape)));
        Assert.Equal(1, Run(db, shape));
    }

    [Fact]
    public void Term_WithDigits_EmitsBareAndMatches()
    {
        using TestDatabase db = OpenDb(nameof(Term_WithDigits_EmitsBareAndMatches));
        Seed(db, "title", "release v9 today");

        Func<IQueryable<ArticleSearch>, IQueryable<ArticleSearch>> shape = q => q.Where(a => SQLiteFTS5Functions.Match(a, f => f.Term("v9")));

        Assert.Equal("v9", MatchValue(BuildMatch(db, shape)));
        Assert.Equal(1, Run(db, shape));
    }

    [Fact]
    public void Term_AllDigits_EmitsBareAndMatches()
    {
        using TestDatabase db = OpenDb(nameof(Term_AllDigits_EmitsBareAndMatches));
        Seed(db, "title", "released in 2024");

        Func<IQueryable<ArticleSearch>, IQueryable<ArticleSearch>> shape = q => q.Where(a => SQLiteFTS5Functions.Match(a, f => f.Term("2024")));

        Assert.Equal("2024", MatchValue(BuildMatch(db, shape)));
        Assert.Equal(1, Run(db, shape));
    }

    [Fact]
    public void Term_WithUnderscore_EmitsBareAndMatches()
    {
        using TestDatabase db = OpenDb(nameof(Term_WithUnderscore_EmitsBareAndMatches));
        Seed(db, "title", "hello_world stuff");

        Func<IQueryable<ArticleSearch>, IQueryable<ArticleSearch>> shape = q => q.Where(a => SQLiteFTS5Functions.Match(a, f => f.Term("hello_world")));

        Assert.Equal("hello_world", MatchValue(BuildMatch(db, shape)));
        Assert.Equal(1, Run(db, shape));
    }

    [Fact]
    public void Term_WithSpace_GetsQuotedAndMatchesPhrase()
    {
        using TestDatabase db = OpenDb(nameof(Term_WithSpace_GetsQuotedAndMatchesPhrase));
        Seed(db, "title", "say hello world today");

        Func<IQueryable<ArticleSearch>, IQueryable<ArticleSearch>> shape = q => q.Where(a => SQLiteFTS5Functions.Match(a, f => f.Term("hello world")));

        Assert.Equal("\"hello world\"", MatchValue(BuildMatch(db, shape)));
        Assert.Equal(1, Run(db, shape));
    }

    [Fact]
    public void Term_WithHyphen_GetsQuotedAndMatchesAdjacentTokens()
    {
        using TestDatabase db = OpenDb(nameof(Term_WithHyphen_GetsQuotedAndMatchesAdjacentTokens));
        Seed(db, "title", "the hello world is here");

        Func<IQueryable<ArticleSearch>, IQueryable<ArticleSearch>> shape = q => q.Where(a => SQLiteFTS5Functions.Match(a, f => f.Term("hello-world")));

        Assert.Equal("\"hello-world\"", MatchValue(BuildMatch(db, shape)));
        Assert.Equal(1, Run(db, shape));
    }

    [Fact]
    public void Term_WithSingleQuote_GetsQuotedAndMatchesAdjacentTokens()
    {
        using TestDatabase db = OpenDb(nameof(Term_WithSingleQuote_GetsQuotedAndMatchesAdjacentTokens));
        Seed(db, "title", "saw the foo bar today");

        Func<IQueryable<ArticleSearch>, IQueryable<ArticleSearch>> shape = q => q.Where(a => SQLiteFTS5Functions.Match(a, f => f.Term("foo'bar")));

        Assert.Equal("\"foo'bar\"", MatchValue(BuildMatch(db, shape)));
        Assert.Equal(1, Run(db, shape));
    }

    [Fact]
    public void Term_WithDoubleQuote_GetsDoubledAndMatchesAdjacentTokens()
    {
        using TestDatabase db = OpenDb(nameof(Term_WithDoubleQuote_GetsDoubledAndMatchesAdjacentTokens));
        Seed(db, "title", "say hello world here");

        Func<IQueryable<ArticleSearch>, IQueryable<ArticleSearch>> shape = q => q.Where(a => SQLiteFTS5Functions.Match(a, f => f.Term("hello\"world")));

        Assert.Equal("\"hello\"\"world\"", MatchValue(BuildMatch(db, shape)));
        Assert.Equal(1, Run(db, shape));
    }

    [Fact]
    public void Term_WithBacktick_GetsQuotedAndMatchesAdjacentTokens()
    {
        using TestDatabase db = OpenDb(nameof(Term_WithBacktick_GetsQuotedAndMatchesAdjacentTokens));
        Seed(db, "title", "the foo bar shows up");

        Func<IQueryable<ArticleSearch>, IQueryable<ArticleSearch>> shape = q => q.Where(a => SQLiteFTS5Functions.Match(a, f => f.Term("foo`bar")));

        Assert.Equal("\"foo`bar\"", MatchValue(BuildMatch(db, shape)));
        Assert.Equal(1, Run(db, shape));
    }

    [Fact]
    public void Term_MixedQuoteChars_OnlyDoubleQuoteIsDoubledAndMatchesAdjacentTokens()
    {
        using TestDatabase db = OpenDb(nameof(Term_MixedQuoteChars_OnlyDoubleQuoteIsDoubledAndMatchesAdjacentTokens));
        Seed(db, "title", "the a b c d ends");

        Func<IQueryable<ArticleSearch>, IQueryable<ArticleSearch>> shape = q => q.Where(a => SQLiteFTS5Functions.Match(a, f => f.Term("a\"b'c`d")));

        Assert.Equal("\"a\"\"b'c`d\"", MatchValue(BuildMatch(db, shape)));
        Assert.Equal(1, Run(db, shape));
    }

    [Fact]
    public void Term_Empty_GetsQuotedAsEmptyTokenAndMatchesNothing()
    {
        using TestDatabase db = OpenDb(nameof(Term_Empty_GetsQuotedAsEmptyTokenAndMatchesNothing));
        Seed(db, "title", "any content here");

        Func<IQueryable<ArticleSearch>, IQueryable<ArticleSearch>> shape = q => q.Where(a => SQLiteFTS5Functions.Match(a, f => f.Term("")));

        Assert.Equal("\"\"", MatchValue(BuildMatch(db, shape)));
        Assert.Equal(0, Run(db, shape));
    }

    [Fact]
    public void Term_UppercaseAnd_GetsQuotedAndMatchesLowercased()
    {
        using TestDatabase db = OpenDb(nameof(Term_UppercaseAnd_GetsQuotedAndMatchesLowercased));
        Seed(db, "title", "X AND Y combined");

        Func<IQueryable<ArticleSearch>, IQueryable<ArticleSearch>> shape = q => q.Where(a => SQLiteFTS5Functions.Match(a, f => f.Term("AND")));

        Assert.Equal("\"AND\"", MatchValue(BuildMatch(db, shape)));
        Assert.Equal(1, Run(db, shape));
    }

    [Fact]
    public void Term_UppercaseOr_GetsQuotedAndMatchesLowercased()
    {
        using TestDatabase db = OpenDb(nameof(Term_UppercaseOr_GetsQuotedAndMatchesLowercased));
        Seed(db, "title", "X OR Y picked");

        Func<IQueryable<ArticleSearch>, IQueryable<ArticleSearch>> shape = q => q.Where(a => SQLiteFTS5Functions.Match(a, f => f.Term("OR")));

        Assert.Equal("\"OR\"", MatchValue(BuildMatch(db, shape)));
        Assert.Equal(1, Run(db, shape));
    }

    [Fact]
    public void Term_UppercaseNot_GetsQuotedAndMatchesLowercased()
    {
        using TestDatabase db = OpenDb(nameof(Term_UppercaseNot_GetsQuotedAndMatchesLowercased));
        Seed(db, "title", "tea is NOT coffee");

        Func<IQueryable<ArticleSearch>, IQueryable<ArticleSearch>> shape = q => q.Where(a => SQLiteFTS5Functions.Match(a, f => f.Term("NOT")));

        Assert.Equal("\"NOT\"", MatchValue(BuildMatch(db, shape)));
        Assert.Equal(1, Run(db, shape));
    }

    [Fact]
    public void Term_UppercaseNear_GetsQuotedAndMatchesLowercased()
    {
        using TestDatabase db = OpenDb(nameof(Term_UppercaseNear_GetsQuotedAndMatchesLowercased));
        Seed(db, "title", "look NEAR the corner");

        Func<IQueryable<ArticleSearch>, IQueryable<ArticleSearch>> shape = q => q.Where(a => SQLiteFTS5Functions.Match(a, f => f.Term("NEAR")));

        Assert.Equal("\"NEAR\"", MatchValue(BuildMatch(db, shape)));
        Assert.Equal(1, Run(db, shape));
    }

    [Fact]
    public void Term_LowercaseKeyword_StaysBareAndMatches()
    {
        using TestDatabase db = OpenDb(nameof(Term_LowercaseKeyword_StaysBareAndMatches));
        Seed(db, "title", "salt and pepper");

        Func<IQueryable<ArticleSearch>, IQueryable<ArticleSearch>> shape = q => q.Where(a => SQLiteFTS5Functions.Match(a, f => f.Term("and")));

        Assert.Equal("and", MatchValue(BuildMatch(db, shape)));
        Assert.Equal(1, Run(db, shape));
    }

    [Fact]
    public void Phrase_Plain_GetsQuotedAndMatchesAdjacent()
    {
        using TestDatabase db = OpenDb(nameof(Phrase_Plain_GetsQuotedAndMatchesAdjacent));
        Seed(db, "title", "shipping native aot today");

        Func<IQueryable<ArticleSearch>, IQueryable<ArticleSearch>> shape = q => q.Where(a => SQLiteFTS5Functions.Match(a, f => f.Phrase("native aot")));

        Assert.Equal("\"native aot\"", MatchValue(BuildMatch(db, shape)));
        Assert.Equal(1, Run(db, shape));
    }

    [Fact]
    public void Phrase_BareWord_StillGetsQuotedAndMatches()
    {
        using TestDatabase db = OpenDb(nameof(Phrase_BareWord_StillGetsQuotedAndMatches));
        Seed(db, "title", "native code generation");

        Func<IQueryable<ArticleSearch>, IQueryable<ArticleSearch>> shape = q => q.Where(a => SQLiteFTS5Functions.Match(a, f => f.Phrase("native")));

        Assert.Equal("\"native\"", MatchValue(BuildMatch(db, shape)));
        Assert.Equal(1, Run(db, shape));
    }

    [Fact]
    public void Phrase_WithDoubleQuote_DoublesInnerQuoteAndMatches()
    {
        using TestDatabase db = OpenDb(nameof(Phrase_WithDoubleQuote_DoublesInnerQuoteAndMatches));
        Seed(db, "title", "she said hi yesterday");

        Func<IQueryable<ArticleSearch>, IQueryable<ArticleSearch>> shape = q => q.Where(a => SQLiteFTS5Functions.Match(a, f => f.Phrase("said \"hi\"")));

        Assert.Equal("\"said \"\"hi\"\"\"", MatchValue(BuildMatch(db, shape)));
        Assert.Equal(1, Run(db, shape));
    }

    [Fact]
    public void Phrase_WithSingleQuote_PassesThroughAndMatches()
    {
        using TestDatabase db = OpenDb(nameof(Phrase_WithSingleQuote_PassesThroughAndMatches));
        Seed(db, "title", "today it s fine actually");

        Func<IQueryable<ArticleSearch>, IQueryable<ArticleSearch>> shape = q => q.Where(a => SQLiteFTS5Functions.Match(a, f => f.Phrase("it's fine")));

        Assert.Equal("\"it's fine\"", MatchValue(BuildMatch(db, shape)));
        Assert.Equal(1, Run(db, shape));
    }

    [Fact]
    public void Phrase_WithBacktick_PassesThroughAndMatches()
    {
        using TestDatabase db = OpenDb(nameof(Phrase_WithBacktick_PassesThroughAndMatches));
        Seed(db, "title", "the foo bar example");

        Func<IQueryable<ArticleSearch>, IQueryable<ArticleSearch>> shape = q => q.Where(a => SQLiteFTS5Functions.Match(a, f => f.Phrase("foo`bar")));

        Assert.Equal("\"foo`bar\"", MatchValue(BuildMatch(db, shape)));
        Assert.Equal(1, Run(db, shape));
    }

    [Fact]
    public void Phrase_Empty_GetsEmptyQuotesAndMatchesNothing()
    {
        using TestDatabase db = OpenDb(nameof(Phrase_Empty_GetsEmptyQuotesAndMatchesNothing));
        Seed(db, "title", "any content here");

        Func<IQueryable<ArticleSearch>, IQueryable<ArticleSearch>> shape = q => q.Where(a => SQLiteFTS5Functions.Match(a, f => f.Phrase("")));

        Assert.Equal("\"\"", MatchValue(BuildMatch(db, shape)));
        Assert.Equal(0, Run(db, shape));
    }

    [Fact]
    public void Prefix_Bare_AppendsStarAndMatches()
    {
        using TestDatabase db = OpenDb(nameof(Prefix_Bare_AppendsStarAndMatches));
        Seed(db, "title", "use native code");

        Func<IQueryable<ArticleSearch>, IQueryable<ArticleSearch>> shape = q => q.Where(a => SQLiteFTS5Functions.Match(a, f => f.Prefix("nativ")));

        Assert.Equal("nativ*", MatchValue(BuildMatch(db, shape)));
        Assert.Equal(1, Run(db, shape));
    }

    [Fact]
    public void Prefix_NeedsQuoting_QuotesAndAppendsStarAndMatches()
    {
        using TestDatabase db = OpenDb(nameof(Prefix_NeedsQuoting_QuotesAndAppendsStarAndMatches));
        Seed(db, "title", "say hello worldwide today");

        Func<IQueryable<ArticleSearch>, IQueryable<ArticleSearch>> shape = q => q.Where(a => SQLiteFTS5Functions.Match(a, f => f.Prefix("hello world")));

        Assert.Equal("\"hello world\"*", MatchValue(BuildMatch(db, shape)));
        Assert.Equal(1, Run(db, shape));
    }

    [Fact]
    public void Prefix_WithDoubleQuote_DoublesAndQuotesAndAppendsStarAndMatches()
    {
        using TestDatabase db = OpenDb(nameof(Prefix_WithDoubleQuote_DoublesAndQuotesAndAppendsStarAndMatches));
        Seed(db, "title", "say hello worldwide together");

        Func<IQueryable<ArticleSearch>, IQueryable<ArticleSearch>> shape = q => q.Where(a => SQLiteFTS5Functions.Match(a, f => f.Prefix("hello\"world")));

        Assert.Equal("\"hello\"\"world\"*", MatchValue(BuildMatch(db, shape)));
        Assert.Equal(1, Run(db, shape));
    }

    [Fact]
    public void Near_PlainTerms_EmitsBareTokensAndMatches()
    {
        using TestDatabase db = OpenDb(nameof(Near_PlainTerms_EmitsBareTokensAndMatches));
        Seed(db, "title", "looking ahead in time");

        Func<IQueryable<ArticleSearch>, IQueryable<ArticleSearch>> shape = q => q.Where(a => SQLiteFTS5Functions.Match(a, f => f.Near(2, "ahead", "time")));

        Assert.Equal("NEAR(ahead time, 2)", MatchValue(BuildMatch(db, shape)));
        Assert.Equal(1, Run(db, shape));
    }

    [Fact]
    public void Near_TermWithSpace_QuotesItAndMatches()
    {
        using TestDatabase db = OpenDb(nameof(Near_TermWithSpace_QuotesItAndMatches));
        Seed(db, "title", "today the hello world saw foo nearby");

        Func<IQueryable<ArticleSearch>, IQueryable<ArticleSearch>> shape = q => q.Where(a => SQLiteFTS5Functions.Match(a, f => f.Near(5, "hello world", "foo")));

        Assert.Equal("NEAR(\"hello world\" foo, 5)", MatchValue(BuildMatch(db, shape)));
        Assert.Equal(1, Run(db, shape));
    }

    [Fact]
    public void Near_TermWithDoubleQuote_DoublesQuoteAndMatches()
    {
        using TestDatabase db = OpenDb(nameof(Near_TermWithDoubleQuote_DoublesQuoteAndMatches));
        Seed(db, "title", "the a b extra c here");

        Func<IQueryable<ArticleSearch>, IQueryable<ArticleSearch>> shape = q => q.Where(a => SQLiteFTS5Functions.Match(a, f => f.Near(3, "a\"b", "c")));

        Assert.Equal("NEAR(\"a\"\"b\" c, 3)", MatchValue(BuildMatch(db, shape)));
        Assert.Equal(1, Run(db, shape));
    }

    [Fact]
    public void And_TwoTerms_JoinsWithAndAndMatchesBoth()
    {
        using TestDatabase db = OpenDb(nameof(And_TwoTerms_JoinsWithAndAndMatchesBoth));
        Seed(db, "title", "alpha and beta together");

        Func<IQueryable<ArticleSearch>, IQueryable<ArticleSearch>> shape = q => q.Where(a => SQLiteFTS5Functions.Match(a, f => f.Term("alpha") && f.Term("beta")));

        Assert.Equal("alpha AND beta", MatchValue(BuildMatch(db, shape)));
        Assert.Equal(1, Run(db, shape));
    }

    [Fact]
    public void Or_TwoTerms_JoinsWithOrAndMatchesEither()
    {
        using TestDatabase db = OpenDb(nameof(Or_TwoTerms_JoinsWithOrAndMatchesEither));
        Seed(db, "title", "only alpha appears here");

        Func<IQueryable<ArticleSearch>, IQueryable<ArticleSearch>> shape = q => q.Where(a => SQLiteFTS5Functions.Match(a, f => f.Term("alpha") || f.Term("beta")));

        Assert.Equal("alpha OR beta", MatchValue(BuildMatch(db, shape)));
        Assert.Equal(1, Run(db, shape));
    }

    [Fact]
    public void Not_BinaryViaAndRight_EmitsFts5NotAndMatches()
    {
        using TestDatabase db = OpenDb(nameof(Not_BinaryViaAndRight_EmitsFts5NotAndMatches));
        Seed(db, "title", "alpha gamma delta words");

        Func<IQueryable<ArticleSearch>, IQueryable<ArticleSearch>> shape = q => q.Where(a => SQLiteFTS5Functions.Match(a, f => f.Term("alpha") && !f.Term("beta")));

        Assert.Equal("alpha NOT beta", MatchValue(BuildMatch(db, shape)));
        Assert.Equal(1, Run(db, shape));
    }

    [Fact]
    public void Not_StandaloneUnary_Throws()
    {
        using TestDatabase db = OpenDb(nameof(Not_StandaloneUnary_Throws));

        NotSupportedException ex = Assert.Throws<NotSupportedException>(() =>
            db.Table<ArticleSearch>()
                .Where(a => SQLiteFTS5Functions.Match(a, f => !f.Term("a")))
                .ToSqlCommand());

        Assert.Contains("FTS5 has no unary NOT operator", ex.Message);
    }

    [Fact]
    public void Precedence_AndOverOr_DoesNotParenthesizeAndMatches()
    {
        using TestDatabase db = OpenDb(nameof(Precedence_AndOverOr_DoesNotParenthesizeAndMatches));
        Seed(db, "title", "alpha is here today");

        Func<IQueryable<ArticleSearch>, IQueryable<ArticleSearch>> shape = q => q.Where(a => SQLiteFTS5Functions.Match(a, f => f.Term("alpha") || f.Term("beta") && f.Term("gamma")));

        Assert.Equal("alpha OR beta AND gamma", MatchValue(BuildMatch(db, shape)));
        Assert.Equal(1, Run(db, shape));
    }

    [Fact]
    public void Precedence_OrInsideAnd_GetsParenthesizedAndMatches()
    {
        using TestDatabase db = OpenDb(nameof(Precedence_OrInsideAnd_GetsParenthesizedAndMatches));
        Seed(db, "title", "alpha and gamma show up");

        Func<IQueryable<ArticleSearch>, IQueryable<ArticleSearch>> shape = q => q.Where(a => SQLiteFTS5Functions.Match(a, f => f.Term("alpha") && (f.Term("beta") || f.Term("gamma"))));

        Assert.Equal("alpha AND (beta OR gamma)", MatchValue(BuildMatch(db, shape)));
        Assert.Equal(1, Run(db, shape));
    }

    [Fact]
    public void Precedence_OrInsideAndLeft_GetsParenthesizedAndMatches()
    {
        using TestDatabase db = OpenDb(nameof(Precedence_OrInsideAndLeft_GetsParenthesizedAndMatches));
        Seed(db, "title", "alpha and gamma show up");

        Func<IQueryable<ArticleSearch>, IQueryable<ArticleSearch>> shape = q => q.Where(a => SQLiteFTS5Functions.Match(a, f => (f.Term("alpha") || f.Term("beta")) && f.Term("gamma")));

        Assert.Equal("(alpha OR beta) AND gamma", MatchValue(BuildMatch(db, shape)));
        Assert.Equal(1, Run(db, shape));
    }

    [Fact]
    public void Not_BinaryViaAndLeft_ReordersAndEmitsFts5NotAndMatches()
    {
        using TestDatabase db = OpenDb(nameof(Not_BinaryViaAndLeft_ReordersAndEmitsFts5NotAndMatches));
        Seed(db, "title", "gamma and beta only");

        Func<IQueryable<ArticleSearch>, IQueryable<ArticleSearch>> shape = q => q.Where(a => SQLiteFTS5Functions.Match(a, f => !f.Term("alpha") && f.Term("beta")));

        Assert.Equal("beta NOT alpha", MatchValue(BuildMatch(db, shape)));
        Assert.Equal(1, Run(db, shape));
    }

    [Fact]
    public void Column_StaticTerm_EmitsColumnScopeAndMatches()
    {
        using TestDatabase db = OpenDb(nameof(Column_StaticTerm_EmitsColumnScopeAndMatches));
        Seed(db, "the native talk", "unrelated body content");

        Func<IQueryable<ArticleSearch>, IQueryable<ArticleSearch>> shape = q => q.Where(a => SQLiteFTS5Functions.Match(a, f => f.Column(a.Title, f.Term("native"))));

        Assert.Equal("{Title} : native", MatchValue(BuildMatch(db, shape)));
        Assert.Equal(1, Run(db, shape));
    }

    [Fact]
    public void Column_TermWithSpace_QuotesInsideScopeAndMatches()
    {
        using TestDatabase db = OpenDb(nameof(Column_TermWithSpace_QuotesInsideScopeAndMatches));
        Seed(db, "say hello world today", "unrelated body");

        Func<IQueryable<ArticleSearch>, IQueryable<ArticleSearch>> shape = q => q.Where(a => SQLiteFTS5Functions.Match(a, f => f.Column(a.Title, f.Term("hello world"))));

        Assert.Equal("{Title} : \"hello world\"", MatchValue(BuildMatch(db, shape)));
        Assert.Equal(1, Run(db, shape));
    }

    [Fact]
    public void Column_OrInsideScope_WrapsWithParensAndMatches()
    {
        using TestDatabase db = OpenDb(nameof(Column_OrInsideScope_WrapsWithParensAndMatches));
        Seed(db, "alpha pages", "unrelated body");

        Func<IQueryable<ArticleSearch>, IQueryable<ArticleSearch>> shape = q => q.Where(a => SQLiteFTS5Functions.Match(a, f => f.Column(a.Title, f.Term("alpha") || f.Term("beta"))));

        Assert.Equal("{Title} : (alpha OR beta)", MatchValue(BuildMatch(db, shape)));
        Assert.Equal(1, Run(db, shape));
    }

    [Fact]
    public void Column_NotOnRightInsideScope_WrapsWithParensAndMatches()
    {
        using TestDatabase db = OpenDb(nameof(Column_NotOnRightInsideScope_WrapsWithParensAndMatches));
        Seed(db, "alpha gamma pages", "unrelated body");

        Func<IQueryable<ArticleSearch>, IQueryable<ArticleSearch>> shape = q => q.Where(a => SQLiteFTS5Functions.Match(a, f => f.Column(a.Title, f.Term("alpha") && !f.Term("beta"))));

        Assert.Equal("{Title} : (alpha NOT beta)", MatchValue(BuildMatch(db, shape)));
        Assert.Equal(1, Run(db, shape));
    }

    [Fact]
    public void Column_NotOnLeftInsideScope_WrapsWithParensAndMatches()
    {
        using TestDatabase db = OpenDb(nameof(Column_NotOnLeftInsideScope_WrapsWithParensAndMatches));
        Seed(db, "alpha gamma pages", "unrelated body");

        Func<IQueryable<ArticleSearch>, IQueryable<ArticleSearch>> shape = q => q.Where(a => SQLiteFTS5Functions.Match(a, f => f.Column(a.Title, !f.Term("beta") && f.Term("alpha"))));

        Assert.Equal("{Title} : (alpha NOT beta)", MatchValue(BuildMatch(db, shape)));
        Assert.Equal(1, Run(db, shape));
    }

    [Fact]
    public void Column_FirstArgWrappedInConvert_StripsConvertAndUsesMemberName()
    {
        using TestDatabase db = OpenDb(nameof(Column_FirstArgWrappedInConvert_StripsConvertAndUsesMemberName));
        Seed(db, "the native talk", "unrelated body");

        Func<IQueryable<ArticleSearch>, IQueryable<ArticleSearch>> shape = q => q.Where(a => SQLiteFTS5Functions.Match(a, f => f.Column((string)(object)a.Title, f.Term("native"))));

        Assert.Equal("{Title} : native", MatchValue(BuildMatch(db, shape)));
        Assert.Equal(1, Run(db, shape));
    }

    [Fact]
    public void StringForm_PassesQueryThroughUnchangedAndMatches()
    {
        using TestDatabase db = OpenDb(nameof(StringForm_PassesQueryThroughUnchangedAndMatches));
        Seed(db, "title", "shipping native aot ready binaries");

        Func<IQueryable<ArticleSearch>, IQueryable<ArticleSearch>> shape = q => q.Where(a => SQLiteFTS5Functions.Match(a, "native AND \"aot ready\""));

        Assert.Equal("native AND \"aot ready\"", MatchValue(BuildMatch(db, shape)));
        Assert.Equal(1, Run(db, shape));
    }

    [Fact]
    public void StringForm_ColumnScoped_WrapsInColumnPrefixAndMatches()
    {
        using TestDatabase db = OpenDb(nameof(StringForm_ColumnScoped_WrapsInColumnPrefixAndMatches));
        Seed(db, "the native talk", "unrelated body");

        Func<IQueryable<ArticleSearch>, IQueryable<ArticleSearch>> shape = q => q.Where(a => SQLiteFTS5Functions.Match(a.Title, "native"));

        Assert.Equal("{Title} : native", MatchValue(BuildMatch(db, shape)));
        Assert.Equal(1, Run(db, shape));
    }

    [Fact]
    public void Dynamic_StaticAndDynamicTerms_ConcatenatesViaPipesAndMatches()
    {
        using TestDatabase db = OpenDb(nameof(Dynamic_StaticAndDynamicTerms_ConcatenatesViaPipesAndMatches));
        Seed(db, "alpha", "alpha and native together");

        SQLiteCommand cmd = (
            from s in db.Table<ArticleSearch>()
            join a in db.Table<Article>() on s.Id equals a.Id
            where SQLiteFTS5Functions.Match(s, f => f.Term("native") && f.Term(a.Title))
            select s.Id).ToSqlCommand();

        Assert.Single(cmd.Parameters);
        Assert.Equal("native AND ", cmd.Parameters[0].Value);
        Assert.Equal(
            N("""
            SELECT a0.rowid AS "Id"
            FROM "ArticleSearch" AS a0
            JOIN "Article" AS a1 ON a0.rowid = a1.Id
            WHERE "ArticleSearch" MATCH (@p0 || printf('"%w"', a1.Title))
            """), N(cmd.CommandText));

        long matches = (
            from s in db.Table<ArticleSearch>()
            join a in db.Table<Article>() on s.Id equals a.Id
            where SQLiteFTS5Functions.Match(s, f => f.Term("native") && f.Term(a.Title))
            select s.Id).LongCount();
        Assert.Equal(1, matches);
    }

    [Fact]
    public void Dynamic_DynamicPrefix_AppendsLiteralStarAndMatches()
    {
        using TestDatabase db = OpenDb(nameof(Dynamic_DynamicPrefix_AppendsLiteralStarAndMatches));
        Seed(db, "nativ", "talking about native compilers");

        SQLiteCommand cmd = (
            from s in db.Table<ArticleSearch>()
            join a in db.Table<Article>() on s.Id equals a.Id
            where SQLiteFTS5Functions.Match(s, f => f.Prefix(a.Title))
            select s.Id).ToSqlCommand();

        Assert.Single(cmd.Parameters);
        Assert.Equal("*", cmd.Parameters[0].Value);
        Assert.Equal(
            N("""
            SELECT a0.rowid AS "Id"
            FROM "ArticleSearch" AS a0
            JOIN "Article" AS a1 ON a0.rowid = a1.Id
            WHERE "ArticleSearch" MATCH (printf('"%w"', a1.Title) || @p0)
            """), N(cmd.CommandText));

        long matches = (
            from s in db.Table<ArticleSearch>()
            join a in db.Table<Article>() on s.Id equals a.Id
            where SQLiteFTS5Functions.Match(s, f => f.Prefix(a.Title))
            select s.Id).LongCount();
        Assert.Equal(1, matches);
    }

    [Fact]
    public void Dynamic_DynamicPhrase_UsesPrintfWrapperAndMatches()
    {
        using TestDatabase db = OpenDb(nameof(Dynamic_DynamicPhrase_UsesPrintfWrapperAndMatches));
        Seed(db, "native aot", "shipping native aot binaries");

        SQLiteCommand cmd = (
            from s in db.Table<ArticleSearch>()
            join a in db.Table<Article>() on s.Id equals a.Id
            where SQLiteFTS5Functions.Match(s, f => f.Phrase(a.Title))
            select s.Id).ToSqlCommand();

        Assert.Empty(cmd.Parameters);
        Assert.Equal(
            N("""
            SELECT a0.rowid AS "Id"
            FROM "ArticleSearch" AS a0
            JOIN "Article" AS a1 ON a0.rowid = a1.Id
            WHERE "ArticleSearch" MATCH (printf('"%w"', a1.Title))
            """), N(cmd.CommandText));

        long matches = (
            from s in db.Table<ArticleSearch>()
            join a in db.Table<Article>() on s.Id equals a.Id
            where SQLiteFTS5Functions.Match(s, f => f.Phrase(a.Title))
            select s.Id).LongCount();
        Assert.Equal(1, matches);
    }

    [Fact]
    public void Dynamic_NearWithDynamicTerm_WrapsOnlyDynamicTermsAndMatches()
    {
        using TestDatabase db = OpenDb(nameof(Dynamic_NearWithDynamicTerm_WrapsOnlyDynamicTermsAndMatches));
        Seed(db, "alpha", "the alpha word and static word here");

        SQLiteCommand cmd = (
            from s in db.Table<ArticleSearch>()
            join a in db.Table<Article>() on s.Id equals a.Id
            where SQLiteFTS5Functions.Match(s, f => f.Near(2, a.Title, "static"))
            select s.Id).ToSqlCommand();

        Assert.Equal(2, cmd.Parameters.Count);
        Assert.Equal("NEAR(", cmd.Parameters[0].Value);
        Assert.Equal(" static, 2)", cmd.Parameters[1].Value);
        Assert.Equal(
            N("""
            SELECT a0.rowid AS "Id"
            FROM "ArticleSearch" AS a0
            JOIN "Article" AS a1 ON a0.rowid = a1.Id
            WHERE "ArticleSearch" MATCH (@p0 || printf('"%w"', a1.Title) || @p1)
            """), N(cmd.CommandText));

        long matches = (
            from s in db.Table<ArticleSearch>()
            join a in db.Table<Article>() on s.Id equals a.Id
            where SQLiteFTS5Functions.Match(s, f => f.Near(2, a.Title, "static"))
            select s.Id).LongCount();
        Assert.Equal(1, matches);
    }

    [Fact]
    public void RoundTrip_DynamicTermWithDoubleQuote_FindsRow()
    {
        using TestDatabase db = OpenDb(nameof(RoundTrip_DynamicTermWithDoubleQuote_FindsRow));
        Seed(db, "with\"quote", "body containing the special with\"quote token among others");

        var hits = (
                from s in db.Table<ArticleSearch>()
                join a in db.Table<Article>() on s.Id equals a.Id
                where SQLiteFTS5Functions.Match(s, f => f.Term(a.Title))
                select s.Id)
            .ToList();

        Assert.Single(hits);
    }

    [Fact]
    public void RoundTrip_TermWithDoubleQuote_StaticConstant_FindsNothingForTokenizedColumn()
    {
        using TestDatabase db = OpenDb(nameof(RoundTrip_TermWithDoubleQuote_StaticConstant_FindsNothingForTokenizedColumn));
        Seed(db, "plain title", "body containing a quoted word among the tokens");

        var hits = db.Table<ArticleSearch>()
            .Where(a => SQLiteFTS5Functions.Match(a, f => f.Term("hello\"world")))
            .ToList();

        Assert.Empty(hits);
    }

    [Fact]
    public void Near_TermsFromVariable_EvaluatesArrayConstantAndMatches()
    {
        using TestDatabase db = OpenDb(nameof(Near_TermsFromVariable_EvaluatesArrayConstantAndMatches));
        Seed(db, "title", "looking ahead in time");
        string[] terms = ["ahead", "time"];

        Func<IQueryable<ArticleSearch>, IQueryable<ArticleSearch>> shape = q => q.Where(a => SQLiteFTS5Functions.Match(a, f => f.Near(2, terms)));

        Assert.Equal("NEAR(ahead time, 2)", MatchValue(BuildMatch(db, shape)));
        Assert.Equal(1, Run(db, shape));
    }

    [Fact]
    public void Match_UnsupportedExpression_Throws()
    {
        using TestDatabase db = OpenDb(nameof(Match_UnsupportedExpression_Throws));

        NotSupportedException ex = Assert.Throws<NotSupportedException>(() =>
            db.Table<ArticleSearch>()
                .Where(a => SQLiteFTS5Functions.Match(a, f => a.Title == "x"))
                .ToSqlCommand());

        Assert.Contains("Unsupported expression inside SQLiteFTS5Functions.Match", ex.Message);
    }

    [Fact]
    public void Match_BooleanLiteralBody_Throws()
    {
        using TestDatabase db = OpenDb(nameof(Match_BooleanLiteralBody_Throws));

        NotSupportedException ex = Assert.Throws<NotSupportedException>(() =>
            db.Table<ArticleSearch>()
                .Where(a => SQLiteFTS5Functions.Match(a, f => true))
                .ToSqlCommand());

        Assert.Contains("Unsupported expression inside SQLiteFTS5Functions.Match", ex.Message);
    }

    [Fact]
    public void Column_StringLiteral_Throws()
    {
        using TestDatabase db = OpenDb(nameof(Column_StringLiteral_Throws));

        NotSupportedException ex = Assert.Throws<NotSupportedException>(() =>
            db.Table<ArticleSearch>()
                .Where(a => SQLiteFTS5Functions.Match(a, f => f.Column("Title", f.Term("native"))))
                .ToSqlCommand());

        Assert.Contains("expects a property reference like a.Title", ex.Message);
    }

    [Fact]
    public void Match_DynamicTermFromUnresolvableExpression_Throws()
    {
        using TestDatabase db = OpenDb(nameof(Match_DynamicTermFromUnresolvableExpression_Throws));

        NotSupportedException ex = Assert.Throws<NotSupportedException>(() =>
            (from s in db.Table<ArticleSearch>()
             join a in db.Table<Article>() on s.Id equals a.Id
             where SQLiteFTS5Functions.Match(s, f => f.Term(a.Title.ToString()))
             select s.Id).ToSqlCommand());

        Assert.Contains("could not be translated to SQL", ex.Message);
    }
}
