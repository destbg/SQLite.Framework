using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class MultiStatementTailGuardTests
{
    [Fact]
    public void ScalarQueryAllowsUnterminatedBlockCommentAfterFinalSemicolon()
    {
        using TestDatabase db = new();

        long value = db.ExecuteScalar<long>("SELECT 3; /* open");

        Assert.Equal(3, value);
    }

    [Fact]
    public void ScalarQueryAllowsWhitespaceAfterFinalSemicolon()
    {
        using TestDatabase db = new();

        long value = db.ExecuteScalar<long>("SELECT 4; \n\t ");

        Assert.Equal(4, value);
    }

    [Fact]
    public void ScalarQueryRejectsSecondStatement()
    {
        using TestDatabase db = new();

        Assert.Throws<InvalidOperationException>(() => db.ExecuteScalar<long>("SELECT 1; SELECT 2"));
    }

    [Fact]
    public void ScalarQueryRejectsSecondStatementAfterLineComment()
    {
        using TestDatabase db = new();

        Assert.Throws<InvalidOperationException>(() => db.ExecuteScalar<long>("SELECT 1; -- c\nSELECT 2"));
    }

    [Fact]
    public void ScalarQueryRejectsSecondStatementAfterBlockComment()
    {
        using TestDatabase db = new();

        Assert.Throws<InvalidOperationException>(() => db.ExecuteScalar<long>("SELECT 1; /* c */ SELECT 2"));
    }

    [Fact]
    public void ExecuteRunsBatchWithTrailingComment()
    {
        using TestDatabase db = new();

        db.Execute("CREATE TABLE TailGuardBatch (Id INTEGER); INSERT INTO TailGuardBatch VALUES (1); -- done");

        Assert.Equal(1, db.ExecuteScalar<long>("SELECT COUNT(*) FROM TailGuardBatch"));
    }

    [Fact]
    public void ExecuteRunsBatchWithCommentBetweenStatements()
    {
        using TestDatabase db = new();

        db.Execute("CREATE TABLE TailGuardMiddle (Id INTEGER); -- create\nINSERT INTO TailGuardMiddle VALUES (2);");

        Assert.Equal(2, db.ExecuteScalar<long>("SELECT Id FROM TailGuardMiddle"));
    }

    [Fact]
    public void ExecuteRunsBatchWithTrailingWhitespace()
    {
        using TestDatabase db = new();

        int changes = db.Execute("CREATE TABLE TailGuardSpace (Id INTEGER); INSERT INTO TailGuardSpace VALUES (3); \n ");

        Assert.Equal(1, changes);
        Assert.Equal(3, db.ExecuteScalar<long>("SELECT Id FROM TailGuardSpace"));
    }

    [Fact]
    public void ExecuteOfOnlyCommentsAffectsNothing()
    {
        using TestDatabase db = new();

        int changes = db.Execute("-- nothing\n/* still nothing */");

        Assert.Equal(0, changes);
    }
}
