# Logging

The framework runs every SQL statement through `SQLiteCommand`. To watch those statements (for debugging, audit, or performance work), register an interceptor on the options builder.

## Quick logging

The shortest path is `LogCommands`. It takes any `Action<string>` and writes a single line per command.

```csharp
SQLiteOptions options = new SQLiteOptionsBuilder("library.db")
    .LogCommands(Console.WriteLine)
    .Build();
```

A line looks like:

```
(12ms, 3 rows) SELECT b0.BookId AS "Id", b0.BookTitle AS "Title" FROM "Books" AS b0 WHERE b0.BookAuthorId = @p0 | @p0=?
```

The numbers are the elapsed time and the number of rows the framework reported. For commands that read rows lazily through a data reader, the number of rows is omitted.

## Sensitive parameter logging

Parameter values are masked with `?` by default so logs are safe to ship to production. If you want the real values inlined, opt in:

```csharp
SQLiteOptions options = new SQLiteOptionsBuilder("library.db")
    .LogCommands(Console.WriteLine)
    .EnableSensitiveParameterLogging()
    .Build();
```

With sensitive logging on, the same line shows `@p0=1` (or `@p0='abc'` for strings) instead of `@p0=?`.

## Custom interceptor

For more than a single log line, write a class that implements `ISQLiteCommandInterceptor`:

```csharp
public sealed class TimingInterceptor : ISQLiteCommandInterceptor
{
    private readonly Dictionary<SQLiteCommand, Stopwatch> watches = new();

    public void OnExecuting(SQLiteCommand command)
    {
        watches[command] = Stopwatch.StartNew();
    }

    public void OnExecuted(SQLiteCommand command, int? rowsAffected)
    {
        if (watches.Remove(command, out Stopwatch? sw))
        {
            Console.WriteLine($"{sw.ElapsedMilliseconds}ms - {command.CommandText}");
        }
    }

    public void OnFailed(SQLiteCommand command, Exception exception)
    {
        watches.Remove(command);
        Console.WriteLine($"FAILED: {exception.Message}");
    }
}

builder.AddCommandInterceptor(new TimingInterceptor());
```

The interceptor methods are:

| Method | When it fires |
|---|---|
| `OnExecuting` | Right before the command runs. |
| `OnExecuted` | After the command runs without throwing. `rowsAffected` is the row count for write paths and `null` for the data-reader path. |
| `OnFailed` | When the command throws. The exception is rethrown after every interceptor sees it. |

## Multiple interceptors

You can register more than one interceptor. They are called in registration order for each event.

```csharp
builder
    .AddCommandInterceptor(new AuditInterceptor())
    .AddCommandInterceptor(new MetricsInterceptor());
```
