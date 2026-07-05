# Pragmas

`db.Pragmas` gives you typed access to the SQLite pragmas the framework cares about. Read or write them as properties. No string SQL needed.

## Common pragmas

```csharp
db.Pragmas.ForeignKeys = true;
db.Pragmas.JournalMode = SQLiteJournalMode.Wal;
db.Pragmas.SynchronousMode = SQLiteSynchronousMode.Normal;
db.Pragmas.CacheSize = -4000;
db.Pragmas.UserVersion = 3;

bool enforced = db.Pragmas.ForeignKeys;
SQLiteJournalMode mode = db.Pragmas.JournalMode;
long pageSize = db.Pragmas.PageSize;
long freePages = db.Pragmas.FreelistCount;
```

The full set of built-in accessors:

| Property | Pragma | Notes |
|---|---|---|
| `ForeignKeys` | `foreign_keys` | True or false. Off by default in SQLite. Cannot change inside a transaction and throws there, since SQLite ignores the change. |
| `JournalMode` | `journal_mode` | `DELETE`, `WAL`, `MEMORY`, `TRUNCATE`, `PERSIST`, `OFF`. |
| `CacheSize` | `cache_size` | Number of pages or kibibytes if you pass a negative number. |
| `SynchronousMode` | `synchronous` | An `SQLiteSynchronousMode` enum: `Off`, `Normal`, `Full`, `Extra`. |
| `UserVersion` | `user_version` | An integer in the file header. The migration runner uses it to track the schema version, so do not set it by hand when you use migrations. |
| `PageSize` | `page_size` | Read only after the file has been written. |
| `FreelistCount` | `freelist_count` | Read only. |
| `RecursiveTriggers` | `recursive_triggers` | True or false. |
| `TempStore` | `temp_store` | `0` default, `1` file, `2` memory. |
| `SecureDelete` | `secure_delete` | True or false. |
| `BusyTimeout` | `busy_timeout` | Milliseconds the busy handler waits before returning `SQLITE_BUSY`. |
| `MmapSize` | `mmap_size` | Bytes SQLite will memory-map. `0` disables. |
| `AutoVacuum` | `auto_vacuum` | `SQLiteAutoVacuumMode` enum: `None`, `Full`, `Incremental`. Only takes effect before the first write. |
| `IncrementalVacuum(pages)` | `incremental_vacuum` | Reclaims free pages. Pass `null` for all or a count. |
| `WalAutoCheckpoint` | `wal_autocheckpoint` | Pages-in-WAL threshold for auto checkpoint. |
| `WalCheckpoint(mode)` | `wal_checkpoint` | Runs a checkpoint with the given `SQLiteWalCheckpointMode`. Returns `true` when fully checkpointed. |
| `IntegrityCheck()` | `integrity_check` | Returns a list. `["ok"]` on a healthy database. |
| `QuickCheck()` | `quick_check` | Faster but less thorough than `IntegrityCheck`. |
| `Optimize()` | `optimize` | Runs planner maintenance. Safe to call at app shutdown. |
| `DeferForeignKeys` | `defer_foreign_keys` | Defers foreign key checks to commit. Resets at commit/rollback. |
| `Encoding` | `encoding` | `SQLiteEncoding` enum: `Utf8`, `Utf16`, `Utf16le`, `Utf16be`. |
| `LockingMode` | `locking_mode` | `SQLiteLockingMode` enum: `Normal`, `Exclusive`. |
| `ApplicationId` | `application_id` | 32-bit magic number stored in the file header. |
| `DataVersion` | `data_version` | Read-only. Increases when another connection modifies the database. |
| `SchemaVersion` | `schema_version` | Read-only. Increases when the schema changes. |

## SQLCipher pragmas

On the `SQLite.Framework.Cipher` package the same accessor adds the `cipher_*` family. These are only visible when the `SQLITECIPHER` compile symbol is defined (the Cipher package sets it):

| Property or method | Pragma | Notes |
|---|---|---|
| `CipherVersion` | `cipher_version` | Read-only SQLCipher build string. |
| `CipherProvider` | `cipher_provider` | Read-only crypto provider name. |
| `CipherProviderVersion` | `cipher_provider_version` | Read-only provider version. |
| `CipherCompatibility` | `cipher_compatibility` | Compatibility version 1-4. Set before opening. |
| `CipherPageSize` | `cipher_page_size` | Page size for the encrypted file. Set before opening. |
| `CipherUseHmac` | `cipher_use_hmac` | Enables HMAC per page. Set before opening. |
| `CipherKdfIter` | `cipher_kdf_iter` | PBKDF2 iterations. Set before opening. |
| `CipherMemorySecurity` | `cipher_memory_security` | Zero internal buffers after use. |
| `Rekey(newKey)` | `rekey` | Re-encrypts the database with a new passphrase. The connection must already be authenticated. |

The cipher set-only properties (compatibility, page size, HMAC, KDF iterations, memory security) only take effect when applied before the encryption key is set on the connection. The setters do not throw if you apply them later, but the new value will not be used until the next key operation.

## System tables

`db.Pragmas.Master` and `db.Pragmas.Sequence` are LINQ-queryable views over `sqlite_master` and `sqlite_sequence`.

```csharp
List<string> tables = await db.Pragmas.Master
    .Where(m => m.Type == "table")
    .Select(m => m.Name)
    .ToListAsync();
```

`sqlite_sequence` only exists after the first `[AutoIncrement]` table is created.

## Pragma table-valued functions

`db.Pragmas.TableInfo(name)`, `IndexList(name)` and `ForeignKeyList(name)` wrap the SQLite pragma TVFs and return `IQueryable<T>`. The argument can be a column from an outer query, in which case the framework emits a single correlated SQL statement.

```csharp
var rows = await (
    from m in db.Pragmas.Master
    from p in db.Pragmas.TableInfo(m.Name)
    where m.Type == "table" && !m.Name.StartsWith("sqlite_")
    select new { Table = m.Name, Column = p.Name, Type = p.Type }
).ToListAsync();
```

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

The built-in accessors still work. `ForeignKeys`, `JournalMode` and the others come along for free.

## Notes

Pragmas affect the open connection, not the database file. If you do not use `SQLiteOptions.IsWalMode` (which sets WAL when the connection opens), you may need to set them again on each new connection.

Some pragmas like `journal_mode` and `secure_delete` return a row when you set them. The setter reads that row for you, so you do not need to.
