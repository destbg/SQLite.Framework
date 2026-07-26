using SQLite.Framework.Internals.Helpers;

namespace SQLite.Framework.Tests;

public class SchemaSqlNormalizerTests
{
    [Theory]
    [InlineData("CREATE INDEX \"IX_A\" ON \"T\" (\"Code\")", "create index [IX_A] on `T` (Code)")]
    [InlineData("CREATE INDEX \"IX_A\" ON main.\"T\" (\"Code\")", "create index [IX_A] on main.`T` (Code)")]
    [InlineData("SELECT * FROM \"T\"", "select * from [T]")]
    [InlineData("FOREIGN KEY (a) REFERENCES \"P\" (b)", "foreign key (a) references [P] (b)")]
    [InlineData("a COLLATE \"NOCASE\"", "a collate [NOCASE]")]
    [InlineData("CREATE TABLE \"T\" (a)", "create table [T] (a)")]
    [InlineData("CREATE VIEW \"V\" AS SELECT 1", "create view [V] as select 1")]
    [InlineData("CREATE TRIGGER \"TR\" AFTER INSERT ON \"T\" BEGIN SELECT 1; END", "create trigger [TR] after insert on [T] begin select 1; end")]
    [InlineData("SELECT * FROM a JOIN \"B\" ON x", "select * from a join [B] on x")]
    [InlineData("x USING \"U\"", "x using [U]")]
    [InlineData("AS \"X\"", "as \"X\"")]
    [InlineData("WHEN \"X\"", "when \"X\"")]
    [InlineData("WHERE \"X\"", "where \"X\"")]
    [InlineData("BETWEEN \"X\"", "between \"X\"")]
    [InlineData("CONSTRAINT \"X\"", "constraint \"X\"")]
    [InlineData("INSERT INTO main.\"T\" (\"Code\") VALUES (1)", "insert into main.[T](Code) values (1)")]
    [InlineData("select a.b(c)", "SELECT a.b(c)")]
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
