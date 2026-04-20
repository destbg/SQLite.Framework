# Dependency Injection

The `SQLite.Framework.DependencyInjection` package registers `SQLiteDatabase` (or a subclass of it) into a `Microsoft.Extensions.DependencyInjection` service collection. The API looks a lot like Entity Framework Core's `AddDbContext` and `AddDbContextFactory`, so if you have used those you will feel at home.

## Installation

```bash
dotnet add package SQLite.Framework
dotnet add package SQLite.Framework.DependencyInjection
```

## Basic registration

Configure `SQLiteOptionsBuilder` in a callback. The options are built once when the database is first resolved.

```csharp
services.AddSQLiteDatabase(b =>
{
    b.DatabasePath = "app.db";
    b.IsWalMode = true;
});
```

Then inject `SQLiteDatabase` anywhere:

```csharp
public class BookService
{
    private readonly SQLiteDatabase db;

    public BookService(SQLiteDatabase db)
    {
        this.db = db;
    }

    public Task<List<Book>> GetAllAsync()
    {
        return db.Table<Book>().ToListAsync();
    }
}
```

## Lifetimes

The default lifetime is `Singleton`. One database instance is created the first time you resolve it and reused for the rest of the app. That's a good choice for most mobile apps.

If you want a different lifetime, pass it as the second argument:

```csharp
services.AddSQLiteDatabase(b => b.DatabasePath = dbPath, ServiceLifetime.Scoped);
```

`Transient` is almost never what you want. You would get a brand new connection for every resolve.

## Reading configuration from the service provider

If the database path (or anything else) comes from another registered service, use the overload that also takes the `IServiceProvider`:

```csharp
services.AddSQLiteDatabase((sp, b) =>
{
    IConfiguration config = sp.GetRequiredService<IConfiguration>();
    b.DatabasePath = config["Db:Path"]!;
    b.IsWalMode = config.GetValue("Db:Wal", true);
});
```

## Custom database subclass

Register a subclass of `SQLiteDatabase` the same way. The subclass needs a public constructor that takes `SQLiteOptions`, plus any other services you want DI to hand it:

```csharp
public class LibraryDatabase : SQLiteDatabase
{
    private readonly ILogger<LibraryDatabase> logger;

    public LibraryDatabase(SQLiteOptions options, ILogger<LibraryDatabase> logger)
        : base(options)
    {
        this.logger = logger;
    }
}

services.AddSQLiteDatabase<LibraryDatabase>(b => b.DatabasePath = "library.db");
```

`ActivatorUtilities` pulls the extra constructor arguments (`ILogger<T>` above) from the same service provider.

## Factory pattern

When a single scope needs more than one database instance (for example, a background worker that iterates items and wants a short lived database per item), register a factory instead:

```csharp
services.AddSQLiteDatabaseFactory<LibraryDatabase>(b => b.DatabasePath = "library.db");
```

Then inject `ISQLiteDatabaseFactory<LibraryDatabase>`:

```csharp
public class ImportWorker
{
    private readonly ISQLiteDatabaseFactory<LibraryDatabase> factory;

    public ImportWorker(ISQLiteDatabaseFactory<LibraryDatabase> factory)
    {
        this.factory = factory;
    }

    public async Task RunAsync(IEnumerable<string> files)
    {
        foreach (string file in files)
        {
            using LibraryDatabase db = factory.CreateDatabase();
            // ... use db ...
        }
    }
}
```

The factory itself is `Singleton` by default. Each `CreateDatabase()` call returns a fresh `LibraryDatabase`. The caller owns the instance and is responsible for disposing it.
