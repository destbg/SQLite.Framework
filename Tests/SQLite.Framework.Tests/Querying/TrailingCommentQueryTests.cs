using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class TrailingCommentQueryTests
{
    [Fact]
    public void ScalarQueryAllowsLineCommentAfterFinalSemicolon()
    {
        using TestDatabase db = new();

        long value = db.ExecuteScalar<long>("SELECT 1; -- done");

        Assert.Equal(1, value);
    }

    [Fact]
    public void ScalarQueryAllowsBlockCommentAfterFinalSemicolon()
    {
        using TestDatabase db = new();

        long value = db.ExecuteScalar<long>("SELECT 2; /* note */");

        Assert.Equal(2, value);
    }
}
