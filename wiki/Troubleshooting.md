# Troubleshooting

A map from the failures you are most likely to hit to their cause and fix. Error messages are quoted the way they appear, so searching this page for the text you see should land you in the right section.

## First, look at the SQL

Most query mysteries dissolve once you see what was sent. Turn on command logging, one line per command with timing and row counts:

```csharp
SQLiteOptions options = new SQLiteOptionsBuilder("app.db")
    .LogCommands(Console.WriteLine)
    .Build();
```

Parameter values are masked with `?` by default. Call `EnableSensitiveParameterLogging()` on the builder to see them. See [Logging](Logging) for interceptors and the full surface.

## SQLiteException

Errors from the engine surface as `SQLiteException` (namespace `SQLite.Framework.Exceptions`). It carries the native message, plus two properties worth logging, `Result` with the SQLite result code and `Sql` with the statement that failed. Codes you will actually meet:

| `Result` | Meaning |
|---|---|
| `Busy` | Another connection holds a conflicting lock. |
| `Locked` | A table is locked within this connection's session. |
| `Constraint` | A UNIQUE, NOT NULL, CHECK or FOREIGN KEY constraint rejected the write. |
| `ReadOnly` | Write attempted on a read-only database file or connection. |
| `Corrupt` | The database file is damaged. |
| `CannotOpen` | The file could not be opened, usually a path or permission problem. |

## "Unable to load shared library 'sqlite3'"

A `DllNotFoundException` naming `sqlite3` on the first database operation means the native SQLite library could not be loaded. On Linux this usually strikes even though SQLite looks installed. The .NET loader wants the unversioned `libsqlite3.so`, which on Debian and Ubuntu ships in `libsqlite3-dev`, not in the runtime package. See [Platform Support](Platform%20Support) for the package name per distribution or switch to `SQLite.Framework.Bundled`, which carries its own binary.

A related startup error saying that a SQLitePCLRaw provider was not set means you are on the `SQLite.Framework.Base` package, which leaves initialization to you. Call `SQLitePCL.Batteries_V2.Init()` once before creating a database.

## "no such function", "no such table"

`no such function` almost always means the SQLite on the device is older than the feature the query uses. With no version floor declared, calls fall through to the engine and this raw error is what comes back. Declare a floor with `UseMinimumSqliteVersion` and the same mistake becomes a clear error naming the feature and the version it needs, thrown before the query runs. See [Choosing a SQLite Version](Choosing%20a%20SQLite%20Version). With a floor declared you may instead see, at open time on a too-old device:

> The loaded SQLite version 3.x.y is below the configured minimum ...

That is the floor working as intended. Ship `SQLite.Framework.Bundled` or lower the floor.

`no such table` means the table was never created on this database file. Check that `CreateTable` or the [migration](Migrations) chain ran against the same path the query uses. A surprisingly common variant is two different relative paths resolving to two different files.

## "database is locked"

Within one `SQLiteDatabase` instance the framework serializes writes with its own lock, so in-process contention queues instead of erroring. A `SQLiteException` with `Result == Busy` therefore points at a second connection, another process, a second `SQLiteDatabase` on the same file or a tool like a DB browser holding the file.

Three levers:

* Set a busy timeout so SQLite waits instead of failing immediately. The framework does not set one by default. `db.Pragmas.BusyTimeout = 5000;`
* Turn on WAL mode with `UseWalMode()`, which lets readers and a writer coexist. See [Multi-threading](Multi-threading).
* Share one `SQLiteDatabase` instance per file instead of opening several.

## NotSupportedException from a query

The translator throws instead of guessing when a LINQ construct has no clean SQL mapping. The generic forms look like:

> Unsupported method: ...
> Cannot translate expression ...

The fix is to reshape the query or materialize early with `ToListAsync()` and finish in memory. Several terminal operators throw a message that names the rewrite, for example `LastAsync` suggests `OrderByDescending(...).FirstAsync(...)` and `MinByAsync` suggests `OrderBy(...).FirstOrDefaultAsync()`.

One rule explains where untranslatable code throws and where it silently works. In the final `Select` projection of an outer query, an untranslatable piece is computed in memory after the SQL runs. Anywhere else, in a `Where`, an `OrderBy`, a join key or any inner query, it must become SQL, so it throws. That is why the same expression can work in a projection and fail as a predicate. [Limitations](Limitations) lists the known cases.

## InvalidOperationException mentioning ReflectionFallbackDisabled

With `DisableReflectionFallback()` set, any query the [Source Generator](Source%20Generator) did not cover fails fast instead of quietly using reflection, for example:

> Select projection fell back to runtime reflection but ReflectionFallbackDisabled is set. ... Projection signature: ...

Checklist when you hit one:

* The `SQLite.Framework.SourceGenerator` package must be referenced by every project that runs queries, the generated class is internal and per-project.
* `UseGeneratedMaterializers()` must be called on the options builder.
* The projection may use a shape the generator does not cover, such as a private type or a private helper method. The message echoes the exact signature it looked for. Reshape it or mark that one query with `UseReflectionMaterializer()`.

## Native AOT and trimming failures

Publishing with `PublishAot` without the source generator leaves materialization on the reflection path, which the trimmer can break. The one explicit runtime guard reads:

> Materializing 'IGrouping<,>' ... requires runtime code generation. ... Use the SQLite.Framework source generator with UseGeneratedMaterializers or remove PublishAot.

The [Native AOT](Native%20AOT) page has the full setup, including the trimmer root descriptor for built-in column types and the `IL2026` suppression needed for anonymous-type projections.

## Migration errors

* > Cannot migrate table '...'. Column '...' is new and NOT NULL with no default, but the table has rows.

  The message names the three fixes. Give the column a default in `OnModelCreating`, fill it with `TableChanged(s => s.Set(...))` or make it nullable. See [Migrations](Migrations).
* > Version N is declared more than once.

  Each version number may be registered once. Also, versions must be 1 or higher.
* A run that fails rolls the database back to the version it started at, so after fixing the cause you just run migrate again.

## Schema drift

When reads return wrong shapes or writes hit unexpected constraints, compare the model against the live file:

```csharp
SQLiteModelValidationResult result = await db.Schema.ValidateModelAsync<Book>();
foreach (string issue in result.Issues)
{
    Console.WriteLine(issue);
}
```

The issues are human-readable sentences, missing or extra columns, type mismatches, key or nullability differences, missing indexes, foreign keys and triggers.

## Write errors

* > Cannot perform an update operation without a primary key, use ExecuteUpdate instead.

  `Update` and `Remove` locate the row by `[Key]`. Give the entity a key or use the predicate-based `ExecuteUpdate` / `ExecuteDelete`.
* Auto-increment misuse fails at `CreateTable` with a message naming the column and rule. SQLite only allows auto-increment on a single-column `INTEGER PRIMARY KEY` and never on a `WITHOUT ROWID` table.
* Constraint violations surface as `SQLiteException` with `Result == Constraint` and SQLite's text naming the constraint, such as `UNIQUE constraint failed: Books.Isbn`.
