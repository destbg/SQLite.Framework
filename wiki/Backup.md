# Backup

`SQLiteDatabase.BackupTo` wraps SQLite's backup API. It copies the source database into a destination. The source stays open for reads and writes during the copy. If a page changes during the copy, SQLite re-copies it for you.

## Backup to another database file

```csharp
using SQLiteDatabase source = new(new SQLiteOptionsBuilder("app.db").Build());
source.BackupTo("backup.db");
```

The destination path is opened, written, and closed for you. If the file exists, it is overwritten.

## Backup to an already-open database

```csharp
using SQLiteDatabase source = new(new SQLiteOptionsBuilder("app.db").Build());
using SQLiteDatabase destination = new(new SQLiteOptionsBuilder("backup.db").Build());
source.BackupTo(destination);
```

Use this overload when you want to keep using the destination after the backup. For example, to run a check on the copy.

## In-memory and file copies

You can load a file into memory at startup for fast reads, then save it back to disk later:

```csharp
SQLiteDatabase memory = new(new SQLiteOptionsBuilder(":memory:").Build());
SQLiteDatabase file = new(new SQLiteOptionsBuilder("disk.db").Build());

file.BackupTo(memory);

memory.BackupTo(file);
```

## Attached databases

The two optional schema name parameters let you back up an attached database instead of `main`:

```csharp
source.BackupTo(destination, sourceName: "aux", destName: "main");
```

## Concurrency

Both the source and the destination connections are locked while the copy runs. Other writes on the source wait until the backup is done. Reads keep working as normal in WAL mode.

## VACUUM and VACUUM INTO

`Vacuum()` rebuilds the database file to reclaim free space and defragment pages. `VacuumInto(path)` writes a clean copy of the database to a separate file, similar to `BackupTo` but in a single SQL statement.

```csharp
db.Vacuum();
db.VacuumInto("clean-copy.db");

await db.VacuumAsync();
await db.VacuumIntoAsync("clean-copy.db");
```

Pass an attached schema name to operate on that schema instead of `main`:

```csharp
db.AttachDatabase("aux.db", "aux");
db.Vacuum("aux");
db.VacuumInto("aux-copy.db", "aux");
```

`VACUUM` cannot run inside a transaction. `VACUUM INTO` requires SQLite 3.27.0 or newer; the `SQLite.Framework.Bundled` package always satisfies that, the OS-provided SQLite needs Android 30 (API level) or iOS 13.

`VacuumInto` differs from `BackupTo` in that it is a single SQLite statement, the destination file is created fresh and must not already exist, and the copy is fully checkpointed and defragmented. `BackupTo` is incremental, can re-copy pages that change mid-flight, and can target an already-open connection.
