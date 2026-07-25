using SQLite.Framework.Internals.Helpers;

namespace SQLite.Framework.Tests;

public class CreateTableInspectorTests
{
    [Fact]
    public void PlainTableHasRowId()
    {
        Assert.False(CreateTableInspector.HasWithoutRowIdClause("CREATE TABLE t (a INTEGER PRIMARY KEY)"));
    }

    [Fact]
    public void WithoutRowIdClauseMatches()
    {
        Assert.True(CreateTableInspector.HasWithoutRowIdClause("CREATE TABLE t (a PRIMARY KEY) WITHOUT ROWID"));
    }

    [Fact]
    public void ExtraWhitespaceBetweenKeywordsMatches()
    {
        Assert.True(CreateTableInspector.HasWithoutRowIdClause("CREATE TABLE t (a PRIMARY KEY) WITHOUT  ROWID"));
    }

    [Fact]
    public void BlockCommentBetweenKeywordsMatches()
    {
        Assert.True(CreateTableInspector.HasWithoutRowIdClause("CREATE TABLE t (a PRIMARY KEY) WITHOUT /* x */ ROWID"));
    }

    [Fact]
    public void LineCommentBetweenKeywordsMatches()
    {
        Assert.True(CreateTableInspector.HasWithoutRowIdClause("CREATE TABLE t (a PRIMARY KEY) WITHOUT -- x\n ROWID"));
    }

    [Fact]
    public void EscapedQuoteLiteralBeforeClauseMatches()
    {
        Assert.True(CreateTableInspector.HasWithoutRowIdClause("CREATE TABLE t (a TEXT DEFAULT 'it''s', b PRIMARY KEY) WITHOUT ROWID"));
    }

    [Fact]
    public void ClauseInsideStringLiteralIsIgnored()
    {
        Assert.False(CreateTableInspector.HasWithoutRowIdClause("CREATE TABLE t (a TEXT, CHECK (a <> 'WITHOUT ROWID'))"));
    }

    [Fact]
    public void ClauseInsideDoubleQuotedIdentifierIsIgnored()
    {
        Assert.False(CreateTableInspector.HasWithoutRowIdClause("CREATE TABLE t (\"WITHOUT ROWID\" TEXT)"));
    }

    [Fact]
    public void ClauseInsideBacktickIdentifierIsIgnored()
    {
        Assert.False(CreateTableInspector.HasWithoutRowIdClause("CREATE TABLE t (`WITHOUT ROWID` TEXT)"));
    }

    [Fact]
    public void ClauseInsideBracketIdentifierIsIgnored()
    {
        Assert.False(CreateTableInspector.HasWithoutRowIdClause("CREATE TABLE t ([WITHOUT ROWID] TEXT)"));
    }

    [Fact]
    public void ClauseInsideLineCommentIsIgnored()
    {
        Assert.False(CreateTableInspector.HasWithoutRowIdClause("CREATE TABLE t (a INTEGER) -- WITHOUT ROWID"));
    }

    [Fact]
    public void ClauseInsideBlockCommentIsIgnored()
    {
        Assert.False(CreateTableInspector.HasWithoutRowIdClause("CREATE TABLE t (a INTEGER) /* WITHOUT ROWID */"));
    }

    [Fact]
    public void PunctuationBetweenKeywordsDoesNotMatch()
    {
        Assert.False(CreateTableInspector.HasWithoutRowIdClause("CREATE TABLE t (WITHOUT, ROWID TEXT)"));
    }

    [Fact]
    public void WithoutFollowedByOtherWordDoesNotMatch()
    {
        Assert.False(CreateTableInspector.HasWithoutRowIdClause("CREATE TABLE t (a) WITHOUT OTHER"));
    }

    [Fact]
    public void UnterminatedStringLiteralIsIgnored()
    {
        Assert.False(CreateTableInspector.HasWithoutRowIdClause("CREATE TABLE t (a TEXT DEFAULT 'WITHOUT ROWID"));
    }

    [Fact]
    public void UnterminatedBracketIdentifierIsIgnored()
    {
        Assert.False(CreateTableInspector.HasWithoutRowIdClause("CREATE TABLE t ([WITHOUT ROWID"));
    }

    [Fact]
    public void UnterminatedBlockCommentIsIgnored()
    {
        Assert.False(CreateTableInspector.HasWithoutRowIdClause("CREATE TABLE t (a) /* WITHOUT ROWID"));
    }

    [Fact]
    public void SlashBeforeNonCommentIsScannedAsText()
    {
        Assert.True(CreateTableInspector.HasWithoutRowIdClause("CREATE TABLE t (a AS (b / 2)) WITHOUT ROWID"));
    }

    [Fact]
    public void TrailingSlashIsScannedAsText()
    {
        Assert.False(CreateTableInspector.HasWithoutRowIdClause("CREATE TABLE t (a) /"));
    }

    [Fact]
    public void DashBeforeNonCommentIsScannedAsText()
    {
        Assert.True(CreateTableInspector.HasWithoutRowIdClause("CREATE TABLE t (a AS (b - 2)) WITHOUT ROWID"));
    }

    [Fact]
    public void TrailingDashIsScannedAsText()
    {
        Assert.False(CreateTableInspector.HasWithoutRowIdClause("CREATE TABLE t (a) -"));
    }

    [Fact]
    public void UnderscoreAndDollarStartedIdentifiersAreScanned()
    {
        Assert.True(CreateTableInspector.HasWithoutRowIdClause("CREATE TABLE t (_id INTEGER, $x INTEGER) WITHOUT ROWID"));
    }
}
