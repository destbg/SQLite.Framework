# Testing

The framework needs no test doubles. SQLite itself is fast enough to run in every test, so tests exercise the real query translation, the real schema and the real data instead of a mock that agrees with whatever you assert. This page shows the patterns, they match how the framework's own test suite (over 25000 tests) is built.

## An in-memory database per test

Pass `:memory:` as the path and each `SQLiteDatabase` instance is its own private, empty database.

```csharp
[Fact]
public async Task CheapBooksAreFound()
{
    SQLiteOptions options = new SQLiteOptionsBuilder(":memory:").Build();
    using SQLiteDatabase db = new(options);
    await db.Schema.CreateTableAsync<Book>();

    await db.Table<Book>().AddAsync(new Book { Title = "Clean Code", Price = 29.99m });
    await db.Table<Book>().AddAsync(new Book { Title = "DDD", Price = 54.99m });

    List<Book> cheap = await db.Table<Book>().Where(b => b.Price < 50).ToListAsync();

    Assert.Single(cheap);
    Assert.Equal("Clean Code", cheap[0].Title);
}
```

One rule makes this work. The connection opens on the first operation and stays open until the instance is disposed, so all setup and all queries must go through the same `SQLiteDatabase` instance. Disposing it destroys the in-memory data. One instance per test in a `using` is exactly right.

Because every test owns a private database, tests are isolated from each other and safe to run in parallel with no extra locking or collections.

A small base class keeps the boilerplate out of the tests:

```csharp
public class TestDatabase : SQLiteDatabase
{
    public TestDatabase(Action<SQLiteOptionsBuilder>? configure = null)
        : base(BuildOptions(configure))
    {
    }

    private static SQLiteOptions BuildOptions(Action<SQLiteOptionsBuilder>? configure)
    {
        SQLiteOptionsBuilder builder = new(":memory:");
        configure?.Invoke(builder);
        return builder.Build();
    }
}
```

## When you need a real file

WAL mode needs a real file, `:memory:` cannot run it. The same goes for tests around [Backup](Backup), attach or anything that inspects the file itself. Use a unique name per test and clean up the sidecar files too:

```csharp
string path = $"wal_test_{Guid.NewGuid():N}.db3";

// after the test
File.Delete(path);
File.Delete(path + "-wal");
File.Delete(path + "-shm");
```

The `Guid` in the name is what lets file-backed tests run in parallel.

## Schema and migrations in test setup

For most tests, creating the tables the test touches is enough. `CreateTable` is idempotent and cheap.

To test the [migration](Migrations) chain itself, run it against a file-backed database and assert both the data and the recorded version:

```csharp
[Fact]
public void MigrateFillsTheNewColumn()
{
    string path = $"migrate_test_{Guid.NewGuid():N}.db3";
    using SQLiteDatabase db = new(new SQLiteOptionsBuilder(path).Build());
    db.Execute("CREATE TABLE \"Books\" (\"Id\" INTEGER PRIMARY KEY)");
    db.Execute("INSERT INTO \"Books\" (\"Id\") VALUES (1)");

    db.Schema.Migrations()
        .Version(1, m => m.TableChanged<Book>(s => s.Set(b => b.Status, "active")))
        .Migrate();

    Assert.Equal("active", db.Table<Book>().Single().Status);
    Assert.Equal(1, db.Pragmas.UserVersion);
}
```

Creating the old shape with raw SQL, as above, is the honest way to test an upgrade. It pins what the database really looked like before the migration. `Plan()` is also useful in tests, it reports what a migrate would do without changing anything.

## Dependency injection in tests

`AddSQLiteDatabase` takes a lifetime, so a test can build a real `ServiceProvider` the same way the app does and swap the path:

```csharp
ServiceCollection services = new();
services.AddSQLiteDatabase<AppDatabase>(b => b.DatabasePath = ":memory:");

using ServiceProvider provider = services.BuildServiceProvider();
AppDatabase db = provider.GetRequiredService<AppDatabase>();
```

One caveat with `:memory:`. Every `SQLiteDatabase` instance is a separate database, so only the default `Singleton` lifetime gives all resolvers the same data. With `Scoped` each scope gets a fresh empty database and with `Transient` every resolve does, which is sometimes exactly what a test wants and sometimes a confusing bug. Pick deliberately.

## Faking time

The framework never reads the clock. There is no internal `DateTime.UtcNow` and no hidden timestamp column, so time in your data always comes from your code. Faking time is then a plain application concern. Inject a `TimeProvider` (or your own clock interface) into the code that stamps entities and give the test a fixed one:

```csharp
public class OrderService(SQLiteDatabase db, TimeProvider clock)
{
    public Task PlaceAsync(Order order)
    {
        order.CreatedAt = clock.GetUtcNow().UtcDateTime;
        return db.Table<Order>().AddAsync(order);
    }
}
```

The exception is SQL-side time. `SQLiteDateFunctions.Datetime()` and `SQLiteFunctions.UnixEpoch()` run inside the SQLite engine at query time and cannot be faked from .NET. Prefer passing a C# timestamp into the query when the moment matters to a test.

The same reasoning applies to `Guid.NewGuid()`. Created in your code before the query, it is a fixed parameter. Written inside a query expression, it translates to SQLite's `RANDOM` functions and produces a new value per row, which no test can pin down.
