using SQLite.Framework.Internals.Helpers;

namespace SQLite.Framework.Tests;

public class SchemaSqlNormalizerTests
{
    [Theory]
    [InlineData("CREATE INDEX \"IX_A\" ON \"T\" (\"Code\")", "create index [IX_A] on `T` (Code)")]
    [InlineData("CREATE INDEX IF NOT EXISTS \"IX_A\" ON \"T\" (\"Code\")", "CREATE INDEX \"IX_A\" ON \"T\" (\"Code\")")]
    [InlineData("SELECT 'it''s'", "SELECT   'it''s'")]
    [InlineData("\"a\"\"b\"", "[a\"b]")]
    [InlineData("\"a\"\"b\"", "`a\"b`")]
    [InlineData("'abc", "'abc")]
    [InlineData("\"abc", "\"abc\"")]
    [InlineData("[abc", "[abc]")]
    [InlineData("a -- x\nb", "a b")]
    [InlineData("a b -- x", "a b")]
    [InlineData("a /* x */ b", "a b")]
    [InlineData("a /* x", "a")]
    [InlineData("a-b", "a - b")]
    [InlineData("a-", "a -")]
    [InlineData("a/b", "a / b")]
    [InlineData("a/", "a /")]
    [InlineData("if a exists", "if a exists")]
    [InlineData("if not a", "if not a")]
    [InlineData("A_1$ where", "a_1$ WHERE")]
    [InlineData("$param a", "$PARAM a")]
    [InlineData("_lead b", "_LEAD b")]
    public void EquivalentDefinitionsMatch(string expected, string actual)
    {
        Assert.True(SchemaSqlNormalizer.AreEquivalent(expected, actual));
    }

    [Theory]
    [InlineData("SELECT 'A'", "SELECT 'a'")]
    [InlineData("'it''s'", "'its'")]
    [InlineData("'abc", "'abc'")]
    [InlineData("CREATE INDEX \"IX_A\" ON \"T\" (\"Code\" DESC)", "CREATE INDEX \"IX_A\" ON \"T\" (\"Code\")")]
    [InlineData("CREATE INDEX \"IX_A\" ON \"T\" (\"Code\" COLLATE NOCASE)", "CREATE INDEX \"IX_A\" ON \"T\" (\"Code\")")]
    [InlineData("a", null)]
    public void DifferentDefinitionsDoNotMatch(string expected, string? actual)
    {
        Assert.False(SchemaSqlNormalizer.AreEquivalent(expected, actual));
    }

    [Theory]
    [InlineData("(a)", "( a )")]
    [InlineData("insert into t values ('A', 1)", "INSERT INTO t VALUES('A',1)")]
    [InlineData("select * from t where x in ('A')", "SELECT * FROM t WHERE x IN ('A')")]
    [InlineData("'Ab'", "'aB'")]
    [InlineData("select x from 'T'", "SELECT x FROM 't'")]
    public void QuotedListDefinitionsMatch(string expected, string actual)
    {
        Assert.True(SchemaSqlNormalizer.AreEquivalent(expected, actual));
    }

    [Theory]
    [InlineData("x = 'a'", "x   =   'a'")]
    [InlineData("x < 'a'", "x<'a'")]
    [InlineData("x > 'a'", "x>'a'")]
    [InlineData("x IS 'a'", "x is 'a'")]
    [InlineData("x LIKE 'a'", "x like 'a'")]
    [InlineData("x GLOB 'a'", "x glob 'a'")]
    [InlineData("x REGEXP 'a'", "x regexp 'a'")]
    [InlineData("x MATCH 'a'", "x match 'a'")]
    [InlineData("x BETWEEN 'a' AND 'b'", "x between 'a' and 'b'")]
    [InlineData("x DEFAULT 'a'", "x default 'a'")]
    [InlineData("SELECT 'a'", "select 'a'")]
    [InlineData("case when x then 'a' end", "CASE WHEN x THEN 'a' END")]
    [InlineData("case when x then 1 else 'a' end", "CASE WHEN x THEN 1 ELSE 'a' END")]
    [InlineData("case 'a' when 1 then 2 end", "CASE 'a' WHEN 1 THEN 2 END")]
    [InlineData("x or 'a'", "x OR 'a'")]
    [InlineData("x NOT 'a'", "x not 'a'")]
    [InlineData("select x from t where 'a'", "SELECT x FROM t WHERE 'a'")]
    [InlineData("case x when 'a' then 1 end", "CASE x WHEN 'a' THEN 1 END")]
    [InlineData("x + 'a'", "x+'a'")]
    [InlineData("x - 'a'", "x-'a'")]
    [InlineData("x * 'a'", "x*'a'")]
    [InlineData("x / 'a'", "x/'a'")]
    [InlineData("x % 'a'", "x%'a'")]
    [InlineData("[<>] 'a'", "[<>] 'a'")]
    [InlineData("[!=] 'a'", "[!=] 'a'")]
    [InlineData("[==] 'a'", "[==] 'a'")]
    [InlineData("[<=] 'a'", "[<=] 'a'")]
    [InlineData("[>=] 'a'", "[>=] 'a'")]
    [InlineData("[||] 'a'", "[||] 'a'")]
    public void LiteralContextDefinitionsMatch(string expected, string actual)
    {
        Assert.True(SchemaSqlNormalizer.AreEquivalent(expected, actual));
    }

    [Theory]
    [InlineData("x = 'A'", "x = 'a'")]
    [InlineData("values ('A')", "values ('a')")]
    [InlineData("x in ('A')", "x in ('a')")]
    public void LiteralCaseDifferenceDoesNotMatch(string expected, string actual)
    {
        Assert.False(SchemaSqlNormalizer.AreEquivalent(expected, actual));
    }
}
