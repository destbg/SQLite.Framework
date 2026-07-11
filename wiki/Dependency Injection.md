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
services.AddSQLiteDatabase<AppDatabase>(b =>
{
    b.DatabasePath = "app.db";
    b.MinimumSqliteVersion = SQLiteMinimumVersion.V3_36;
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
services.AddSQLiteDatabase<AppDatabase>(b => b.DatabasePath = dbPath, ServiceLifetime.Scoped);
```

`Transient` is almost never what you want. You would get a brand new connection for every resolve.

## Reading configuration from the service provider

If the database path (or anything else) comes from another registered service, use the overload that also takes the `IServiceProvider`:

```csharp
services.AddSQLiteDatabase<AppDatabase>((sp, b) =>
{
    IConfiguration config = sp.GetRequiredService<IConfiguration>();
    b.DatabasePath = config["Db:Path"]!;
    b.IsWalMode = config.GetValue("Db:Wal", true);
});
```

## Custom database subclass

Register a subclass of `SQLiteDatabase` the same way. The subclass needs a public constructor that takes `SQLiteOptions`, plus any other services you want DI to hand it:

```csharp
public class AppDatabase : SQLiteDatabase
{
    private readonly ILogger<AppDatabase> logger;

    public AppDatabase(SQLiteOptions options, ILogger<AppDatabase> logger)
        : base(options)
    {
        this.logger = logger;
    }
}

services.AddSQLiteDatabase<AppDatabase>(b => b.DatabasePath = "library.db");
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

## Migrations on startup

Pass a `migrations` callback to declare the [migration](Migrations) chain on the registration. Every database instance the registration creates is migrated right after it is constructed, so the schema is ready before the first query.

```csharp
services.AddSQLiteDatabase<AppDatabase>(
    b => b.DatabasePath = "app.db",
    migrations: r => r
        .Add<M0001_InitialSchema>()
        .Add<M0002_AddBookGenre>());
```

The same parameter exists on `AddSQLiteDatabaseFactory`. Each `CreateDatabase()` call then returns a migrated instance.

The migration runs synchronously during the resolve. Versions that declare `RunAsync` or `RunBeforeAsync` callbacks cannot run here, so migrate those chains with `MigrateAsync`. When the migration fails, the database instance is disposed and the exception leaves the resolve.

### Injecting services into a migration class

A migration class added with `Add<T>()` on a database created through this package is built from the service provider, so its constructor can take services. You do not register the migration class itself, only its dependencies.

```csharp
public sealed class M0002_SeedCountries : ISQLiteMigration
{
    public static int Version => 2;

    private readonly ICountrySource countries;

    public M0002_SeedCountries(ICountrySource countries)
    {
        this.countries = countries;
    }

    public void Apply(SQLiteMigrationStep step)
    {
        step.TableChanged<Country>();
        foreach (Country country in countries.All())
        {
            step.Insert(country);
        }
    }
}
```

```csharp
services.AddSingleton<ICountrySource, StaticCountrySource>();
services.AddSQLiteDatabase<AppDatabase>(
    b => b.DatabasePath = "app.db",
    migrations: r => r
        .Add<M0001_InitialSchema>()
        .Add<M0002_SeedCountries>());
```
