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
}
