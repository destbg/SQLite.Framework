using SQLite.Framework.Internals.Helpers;

namespace SQLite.Framework.Tests;

public class SqlTailTests
{
    [Fact]
    public void NullTailIsClean()
    {
        Assert.True(SqlTail.IsWhitespaceOrComments(null));
    }

    [Fact]
    public void EmptyTailIsClean()
    {
        Assert.True(SqlTail.IsWhitespaceOrComments(""));
    }

    [Fact]
    public void WhitespaceTailIsClean()
    {
        Assert.True(SqlTail.IsWhitespaceOrComments(" \t\r\n"));
    }

    [Fact]
    public void LineCommentTailIsClean()
    {
        Assert.True(SqlTail.IsWhitespaceOrComments(" -- done"));
    }

    [Fact]
    public void LineCommentThenBlockCommentTailIsClean()
    {
        Assert.True(SqlTail.IsWhitespaceOrComments("-- a\n /* b */ "));
    }

    [Fact]
    public void UnterminatedBlockCommentTailIsClean()
    {
        Assert.True(SqlTail.IsWhitespaceOrComments("/* open"));
    }

    [Fact]
    public void SemicolonTailIsNotClean()
    {
        Assert.False(SqlTail.IsWhitespaceOrComments("; "));
    }

    [Fact]
    public void StatementAfterLineCommentIsNotClean()
    {
        Assert.False(SqlTail.IsWhitespaceOrComments("-- c\nSELECT 2"));
    }

    [Fact]
    public void StatementAfterBlockCommentIsNotClean()
    {
        Assert.False(SqlTail.IsWhitespaceOrComments("/* c */ SELECT 2"));
    }

    [Fact]
    public void LoneDashIsNotClean()
    {
        Assert.False(SqlTail.IsWhitespaceOrComments(" -"));
    }

    [Fact]
    public void DashBeforeValueIsNotClean()
    {
        Assert.False(SqlTail.IsWhitespaceOrComments("- 1"));
    }

    [Fact]
    public void LoneSlashIsNotClean()
    {
        Assert.False(SqlTail.IsWhitespaceOrComments(" /"));
    }

    [Fact]
    public void SlashBeforeValueIsNotClean()
    {
        Assert.False(SqlTail.IsWhitespaceOrComments("/ 2"));
    }

    [Fact]
    public void TrimKeepsFragmentWithoutTail()
    {
        Assert.Equal("SELECT 1", SqlTail.TrimStatementTail("SELECT 1"));
    }

    [Fact]
    public void TrimRemovesTrailingSemicolon()
    {
        Assert.Equal("SELECT 1", SqlTail.TrimStatementTail("SELECT 1;"));
    }

    [Fact]
    public void TrimRemovesSemicolonWhitespaceAndLineComment()
    {
        Assert.Equal("SELECT 1", SqlTail.TrimStatementTail("SELECT 1; -- done"));
    }

    [Fact]
    public void TrimRemovesStackedLineComments()
    {
        Assert.Equal("SELECT 1", SqlTail.TrimStatementTail("SELECT 1; -- a\n -- b"));
    }

    [Fact]
    public void TrimRemovesBlockComment()
    {
        Assert.Equal("SELECT 2", SqlTail.TrimStatementTail("SELECT 2; /* note */ "));
    }

    [Fact]
    public void TrimRemovesUnterminatedBlockComment()
    {
        Assert.Equal("SELECT 2", SqlTail.TrimStatementTail("SELECT 2; /* open"));
    }

    [Fact]
    public void TrimKeepsSemicolonBetweenStatements()
    {
        Assert.Equal("SELECT 1; SELECT 2", SqlTail.TrimStatementTail("SELECT 1; SELECT 2;"));
    }

    [Fact]
    public void TrimKeepsCommentMarkerInsideStringLiteral()
    {
        Assert.Equal("SELECT 'a--b'", SqlTail.TrimStatementTail("SELECT 'a--b';"));
    }

    [Fact]
    public void TrimKeepsSemicolonInsideStringLiteral()
    {
        Assert.Equal("SELECT 'a;'", SqlTail.TrimStatementTail("SELECT 'a;' ; "));
    }

    [Fact]
    public void TrimKeepsEscapedQuoteInsideStringLiteral()
    {
        Assert.Equal("SELECT 'it''s'", SqlTail.TrimStatementTail("SELECT 'it''s';"));
    }

    [Fact]
    public void TrimKeepsLiteralClosingAtEndOfText()
    {
        Assert.Equal("SELECT ';'", SqlTail.TrimStatementTail("SELECT ';'"));
    }

    [Fact]
    public void TrimKeepsSemicolonInsideQuotedIdentifier()
    {
        Assert.Equal("SELECT \"col;\" FROM t", SqlTail.TrimStatementTail("SELECT \"col;\" FROM t;"));
    }

    [Fact]
    public void TrimKeepsCommentMarkerInsideBacktickIdentifier()
    {
        Assert.Equal("SELECT `a--b` FROM t", SqlTail.TrimStatementTail("SELECT `a--b` FROM t ;"));
    }

    [Fact]
    public void TrimKeepsCommentMarkerInsideBracketIdentifier()
    {
        Assert.Equal("SELECT [a--b] FROM t", SqlTail.TrimStatementTail("SELECT [a--b] FROM t;"));
    }

    [Fact]
    public void TrimKeepsUnterminatedBracketIdentifier()
    {
        Assert.Equal("SELECT [x", SqlTail.TrimStatementTail("SELECT [x"));
    }

    [Fact]
    public void TrimKeepsUnterminatedStringLiteral()
    {
        Assert.Equal("SELECT 'a", SqlTail.TrimStatementTail("SELECT 'a"));
    }

    [Fact]
    public void TrimKeepsLiteralEndingInEscapedQuote()
    {
        Assert.Equal("SELECT 'a''", SqlTail.TrimStatementTail("SELECT 'a''"));
    }

    [Fact]
    public void TrimKeepsTrailingDash()
    {
        Assert.Equal("SELECT 1 -", SqlTail.TrimStatementTail("SELECT 1 -"));
    }

    [Fact]
    public void TrimKeepsSubtraction()
    {
        Assert.Equal("SELECT 1 - 2", SqlTail.TrimStatementTail("SELECT 1 - 2"));
    }

    [Fact]
    public void TrimKeepsTrailingSlash()
    {
        Assert.Equal("SELECT 1 /", SqlTail.TrimStatementTail("SELECT 1 /"));
    }

    [Fact]
    public void TrimKeepsDivision()
    {
        Assert.Equal("SELECT 1 / 2", SqlTail.TrimStatementTail("SELECT 1 / 2;"));
    }

    [Fact]
    public void TrimOfOnlySeparatorsIsEmpty()
    {
        Assert.Equal("", SqlTail.TrimStatementTail(" ; ; -- x"));
    }
}
