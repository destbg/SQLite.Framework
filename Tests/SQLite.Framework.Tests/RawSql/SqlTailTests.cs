using SQLite.Framework.Internals.Helpers;

namespace SQLite.Framework.Tests;

public class SqlTailTests
{
    [Fact]
    public void NullTailHasNoStatement()
    {
        Assert.False(SqlTail.HasStatement(null));
    }

    [Fact]
    public void EmptyTailHasNoStatement()
    {
        Assert.False(SqlTail.HasStatement(""));
    }

    [Fact]
    public void WhitespaceTailHasNoStatement()
    {
        Assert.False(SqlTail.HasStatement(" \t\r\n"));
    }

    [Fact]
    public void LineCommentTailHasNoStatement()
    {
        Assert.False(SqlTail.HasStatement(" -- done"));
    }

    [Fact]
    public void LineCommentThenBlockCommentTailHasNoStatement()
    {
        Assert.False(SqlTail.HasStatement("-- a\n /* b */ "));
    }

    [Fact]
    public void UnterminatedBlockCommentTailHasNoStatement()
    {
        Assert.False(SqlTail.HasStatement("/* open"));
    }

    [Fact]
    public void SemicolonTailHasNoStatement()
    {
        Assert.False(SqlTail.HasStatement("; "));
    }

    [Fact]
    public void StackedEmptyStatementsHaveNoStatement()
    {
        Assert.False(SqlTail.HasStatement(" ;; -- x"));
    }

    [Fact]
    public void EmptyStatementBeforeStatementHasStatement()
    {
        Assert.True(SqlTail.HasStatement("; SELECT 2"));
    }

    [Fact]
    public void StatementAfterLineCommentHasStatement()
    {
        Assert.True(SqlTail.HasStatement("-- c\nSELECT 2"));
    }

    [Fact]
    public void StatementAfterBlockCommentHasStatement()
    {
        Assert.True(SqlTail.HasStatement("/* c */ SELECT 2"));
    }

    [Fact]
    public void LoneDashHasStatement()
    {
        Assert.True(SqlTail.HasStatement(" -"));
    }

    [Fact]
    public void DashBeforeValueHasStatement()
    {
        Assert.True(SqlTail.HasStatement("- 1"));
    }

    [Fact]
    public void LoneSlashHasStatement()
    {
        Assert.True(SqlTail.HasStatement(" /"));
    }

    [Fact]
    public void SlashBeforeValueHasStatement()
    {
        Assert.True(SqlTail.HasStatement("/ 2"));
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

    [Theory]
    [InlineData("SELECT 1; SELECT 2", true)]
    [InlineData("SELECT 1; DELETE FROM [t]", true)]
    [InlineData("SELECT [a;b] FROM t", false)]
    [InlineData("SELECT [a;b", false)]
    [InlineData("SELECT 1; /* trailing ; note */", false)]
    [InlineData("SELECT 1; /* trailing ; note", false)]
    [InlineData("SELECT 1; -- done ;", false)]
    [InlineData("SELECT ';'", false)]
    [InlineData("SELECT `a;b`", false)]
    [InlineData("; SELECT 1", false)]
    [InlineData(";;", false)]
    public void MultipleStatementDetectionMatchesStatementBoundaries(string sql, bool expected)
    {
        Assert.Equal(expected, SqlTail.HasMultipleStatements(sql));
    }
}
