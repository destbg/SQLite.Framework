using System.Diagnostics;

namespace SQLite.Framework.Internals.Helpers;

/// <summary>
/// Built-in <see cref="ISQLiteCommandInterceptor" /> that formats every command into a
/// single line and passes it to a user-supplied action. Registered through
/// <see cref="SQLiteOptionsBuilder.LogCommands" />. Includes the elapsed time per command.
/// </summary>
internal sealed class LoggingCommandInterceptor : ISQLiteCommandInterceptor
{
    private readonly Action<string> logger;
    private readonly Dictionary<SQLiteCommand, long> startedAt = [];
    private readonly object lockObject = new();

    public LoggingCommandInterceptor(Action<string> logger)
    {
        this.logger = logger;
    }

    public void OnExecuting(SQLiteCommand command)
    {
        long timestamp = Stopwatch.GetTimestamp();
        lock (lockObject)
        {
            startedAt[command] = timestamp;
        }
    }

    public void OnExecuted(SQLiteCommand command, int? rowsAffected)
    {
        TimeSpan elapsed = TakeElapsed(command);
        bool sensitive = command.Database.Options.SensitiveParameterLoggingEnabled;
        logger(FormatLine(command, elapsed, rowsAffected, exception: null, sensitive));
    }

    public void OnFailed(SQLiteCommand command, Exception exception)
    {
        TimeSpan elapsed = TakeElapsed(command);
        bool sensitive = command.Database.Options.SensitiveParameterLoggingEnabled;
        logger(FormatLine(command, elapsed, rowsAffected: null, exception, sensitive));
    }

    private TimeSpan TakeElapsed(SQLiteCommand command)
    {
        long now = Stopwatch.GetTimestamp();
        long start;

        lock (lockObject)
        {
            startedAt.Remove(command, out start);
        }

        return Stopwatch.GetElapsedTime(start, now);
    }

    private static string FormatLine(SQLiteCommand command, TimeSpan elapsed, int? rowsAffected, Exception? exception, bool sensitive)
    {
        StringBuilder sb = new();
        sb.Append('(');
        sb.Append((long)elapsed.TotalMilliseconds);
        sb.Append("ms");

        if (rowsAffected != null)
        {
            sb.Append(", ");
            sb.Append(rowsAffected.Value);
            sb.Append(" rows");
        }

        if (exception != null)
        {
            sb.Append(", FAILED: ");
            sb.Append(exception.GetType().Name);
            sb.Append(": ");
            sb.Append(exception.Message);
        }

        sb.Append(") ");
        sb.Append(command.CommandText);

        if (command.Parameters.Count > 0)
        {
            sb.Append(" | ");
            for (int i = 0; i < command.Parameters.Count; i++)
            {
                if (i > 0)
                {
                    sb.Append(' ');
                }

                SQLiteParameter parameter = command.Parameters[i];
                sb.Append(parameter.Name);
                sb.Append('=');
                sb.Append(sensitive ? FormatValue(parameter.Value) : "?");
            }
        }

        return sb.ToString();
    }

    private static string FormatValue(object? value)
    {
        return value switch
        {
            null => "NULL",
            string s => "'" + s.Replace("'", "''") + "'",
            bool b => b ? "1" : "0",
            byte[] b => $"<{b.Length} bytes>",
            _ => Convert.ToString(value, CultureInfo.InvariantCulture)!,
        };
    }
}
