using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class InterceptorExceptionLoggingElapsedTests
{
    [Fact]
    public void ThrowInLaterOnExecuted_NonQueryLogElapsedIsReal()
    {
        List<string> lines = [];
        OneShotThrowingInterceptor throwing = new();
        using TestDatabase db = new(b => b.LogCommands(lines.Add).AddCommandInterceptor(throwing));

        Assert.Throws<InvalidOperationException>(() => db.Execute("CREATE TABLE ElapsedSample (X INTEGER)"));

        Assert.Equal(1L, db.ExecuteScalar<long>("SELECT COUNT(*) FROM sqlite_master WHERE name = 'ElapsedSample'"));
        Assert.All(lines, line => Assert.InRange(ParseElapsedMilliseconds(line), 0, 10000));
    }

    [Fact]
    public void ThrowInLaterOnExecuted_ReaderLogElapsedIsReal()
    {
        List<string> lines = [];
        OneShotThrowingInterceptor throwing = new();
        using TestDatabase db = new(b => b.LogCommands(lines.Add).AddCommandInterceptor(throwing));

        Assert.Throws<InvalidOperationException>(() => db.ExecuteScalar<long>("SELECT 42"));

        Assert.All(lines, line => Assert.InRange(ParseElapsedMilliseconds(line), 0, 10000));
    }

    private static long ParseElapsedMilliseconds(string line)
    {
        int start = line.IndexOf('(') + 1;
        int end = line.IndexOf("ms", StringComparison.Ordinal);
        return long.Parse(line[start..end]);
    }

    private sealed class OneShotThrowingInterceptor : ISQLiteCommandInterceptor
    {
        private bool thrown;

        public void OnExecuting(SQLiteCommand command)
        {
        }

        public void OnExecuted(SQLiteCommand command, int? rowsAffected)
        {
            if (!thrown)
            {
                thrown = true;
                throw new InvalidOperationException("interceptor exception");
            }
        }

        public void OnFailed(SQLiteCommand command, Exception exception)
        {
        }

        public void OnRowRead(SQLiteCommand command, SQLiteDataReader reader)
        {
        }

        public void OnReaderClosing(SQLiteCommand command, SQLiteDataReader reader, int readCount)
        {
        }
    }
}
