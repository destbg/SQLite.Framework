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
#42 (12ms, 3 rows) SELECT b0."BookId" AS "Id", b0."BookTitle" AS "Title" FROM "Books" AS b0 WHERE b0."BookAuthorId" = @p0 | @p0=?
```

`#42` is the command id (see [Command id](#command-id)). After it come the elapsed time and the number of rows the framework reported. For commands that read rows lazily through a data reader, the number of rows is omitted.

## Sensitive parameter logging

Parameter values are masked with `?` by default so logs are safe to ship to production. If you want the real values inlined, opt in:

```csharp
SQLiteOptions options = new SQLiteOptionsBuilder("library.db")
    .LogCommands(Console.WriteLine)
    .EnableSensitiveParameterLogging()
    .Build();
```

With sensitive logging on, the same line shows `@p0=1` (or `@p0='abc'` for strings) instead of `@p0=?`.

## Command id

Every `SQLiteCommand` is given a number when it is created, exposed as `SQLiteCommand.Id`. The number grows by one for each command the database makes, so it is unique within one `SQLiteDatabase`. It is assigned at creation, before the command runs, and never changes. An interceptor reads it to tie the calls for one command together, for example to match a per-row callback back to the statement that produced it. The built-in `LogCommands` line starts with it as `#42`.

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

    public void OnRowRead(SQLiteCommand command, SQLiteDataReader reader)
    {
    }
}

builder.AddCommandInterceptor(new TimingInterceptor());
```

`ISQLiteCommandInterceptor` has four methods. Every one is required, so implement the ones you do not need with an empty body, as `OnRowRead` is above.

| Method | When it fires |
|---|---|
| `OnExecuting` | Right before the command runs. |
| `OnExecuted` | After the command runs without throwing. `rowsAffected` is the row count for write paths and `null` for the data-reader path. |
| `OnFailed` | When the command throws. The exception is rethrown after every interceptor sees it. |
| `OnRowRead` | Once for each row read through a data reader. Use it to observe the data a query returns. |

## Observing returned rows

`OnExecuted` reports a row count for writes, but never the data a query returns. To watch that data, implement `OnRowRead`. It fires once for each row a data reader steps over, with the same command (so `command.Id` matches its other calls) and the live reader to read columns from.

```csharp
public sealed class RowLogger : ISQLiteCommandInterceptor
{
    public void OnExecuting(SQLiteCommand command) { }
    public void OnExecuted(SQLiteCommand command, int? rowsAffected) { }
    public void OnFailed(SQLiteCommand command, Exception exception) { }

    public void OnRowRead(SQLiteCommand command, SQLiteDataReader reader)
    {
        long id = reader.GetInt64(0);
        Console.WriteLine($"#{command.Id} returned id {id}");
    }
}
```

Read column values only. Do not call `Read` or `Dispose` on the reader inside `OnRowRead`, since that advances or closes the stream the caller is reading. Only the data-reader path raises `OnRowRead`. Scalar reads and writes do not stream rows, so they never call it.

## Multiple interceptors

You can register more than one interceptor. They are called in registration order for each event.

```csharp
builder
    .AddCommandInterceptor(new AuditInterceptor())
    .AddCommandInterceptor(new MetricsInterceptor());
```
