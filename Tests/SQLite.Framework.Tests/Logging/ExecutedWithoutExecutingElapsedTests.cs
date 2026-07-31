using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class ExecutedWithoutExecutingElapsedTests
{
    [Fact]
    public void AnExecutedNotificationWithoutAStartStillWritesALogLine()
    {
        List<string> lines = [];
        using TestDatabase db = new(b => b.LogCommands(lines.Add));

        SQLiteCommand command = db.CreateCommand("SELECT 1", []);
        command.NotifyExecuted(rowsAffected: null);

        Assert.Single(lines);
    }
}
