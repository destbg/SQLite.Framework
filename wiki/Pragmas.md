# Pragmas

`db.Pragmas` gives you typed access to the SQLite pragmas the framework cares about. Read or write them as properties. No string SQL needed.

## Common pragmas

```csharp
db.Pragmas.ForeignKeys = true;
db.Pragmas.JournalMode = "WAL";
db.Pragmas.SynchronousMode = SQLiteSynchronousMode.Normal;
db.Pragmas.CacheSize = -4000;
db.Pragmas.UserVersion = 3;

bool enforced = db.Pragmas.ForeignKeys;
string mode = db.Pragmas.JournalMode;
long pageSize = db.Pragmas.PageSize;
long freePages = db.Pragmas.FreelistCount;
```

The full set of built-in accessors:

| Property | Pragma | Notes |
|---|---|---|
| `ForeignKeys` | `foreign_keys` | True or false. Off by default in SQLite. |
| `JournalMode` | `journal_mode` | `DELETE`, `WAL`, `MEMORY`, `TRUNCATE`, `PERSIST`, `OFF`. |
| `CacheSize` | `cache_size` | Number of pages, or kibibytes if you pass a negative number. |
| `SynchronousMode` | `synchronous` | An `SQLiteSynchronousMode` enum: `Off`, `Normal`, `Full`, `Extra`. |
| `UserVersion` | `user_version` | An integer your app picks. |
| `PageSize` | `page_size` | Read only after the file has been written. |
| `FreelistCount` | `freelist_count` | Read only. |
| `RecursiveTriggers` | `recursive_triggers` | True or false. |
| `TempStore` | `temp_store` | `0` default, `1` file, `2` memory. |
| `SecureDelete` | `secure_delete` | True or false. |

## Adding more pragmas

Make a class that inherits `SQLitePragmas`. Then register it on the options builder:

```csharp
public sealed class AppPragmas : SQLitePragmas
{
    public AppPragmas(SQLiteDatabase database) : base(database) { }

    public int BusyTimeoutMs
    {
        get => Database.ExecuteScalar<int>("PRAGMA busy_timeout");
        set => Database.Execute($"PRAGMA busy_timeout = {value}");
    }
}

SQLiteOptions options = new SQLiteOptionsBuilder("app.db")
    .UsePragmas(db => new AppPragmas(db))
    .Build();

using SQLiteDatabase db = new(options);
((AppPragmas)db.Pragmas).BusyTimeoutMs = 5000;
```

The built-in accessors still work. `ForeignKeys`, `JournalMode`, and the others come along for free.

## Notes

Pragmas affect the open connection, not the database file. If you do not use `SQLiteOptions.IsWalMode` (which sets WAL when the connection opens), you may need to set them again on each new connection.

Some pragmas like `journal_mode` and `secure_delete` return a row when you set them. The setter reads that row for you, so you do not need to.
