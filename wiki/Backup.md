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
