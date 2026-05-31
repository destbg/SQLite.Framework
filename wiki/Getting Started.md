# Getting Started

## Installation

Install the NuGet package:

```bash
dotnet add package SQLite.Framework
```

## Console App

Build a read-only `SQLiteOptions` with `SQLiteOptionsBuilder` and hand it to the `SQLiteDatabase` constructor. The database path lives on the options. The connection opens automatically on the first operation.

```csharp
SQLiteOptions options = new SQLiteOptionsBuilder("library.db")
    .UseMinimumSqliteVersion(SQLiteMinimumVersion.V3_35)
    .Build();
using SQLiteDatabase db = new(options);

await db.Schema.CreateTableAsync<Author>();
await db.Schema.CreateTableAsync<Book>();

var authors = db.Table<Author>();
var books = db.Table<Book>();

await authors.AddAsync(new Author { Name = "Robert Martin", Country = "USA" });
await books.AddAsync(new Book { Title = "Clean Code", AuthorId = 1, Price = 29.99m });

var results = await books.Where(b => b.Price < 50).ToListAsync();
```

`SQLiteDatabase` implements `IDisposable`, so use it inside a `using` block or statement.

### Why a builder?

`SQLiteOptionsBuilder` is mutable and lets you chain `Use*` / `Add*` calls. Once you call `Build()`, the returned `SQLiteOptions` is fully immutable, this makes it safe to share through dependency injection and reuse across databases without worrying about a late change affecting live code paths.

### Declaring a minimum SQLite version

The system SQLite on Android and iOS varies by OS version, so a method that compiles against the framework may still fail at runtime if the device's SQLite is too old. Declare the floor your app commits to:

```csharp
SQLiteOptions options = new SQLiteOptionsBuilder("app.db")
    .UseMinimumSqliteVersion(SQLiteMinimumVersion.V3_35)
    .Build();
```

Behavior:

- At connection-open time the framework reads sqlite's version and throws if the loaded SQLite is below the floor.
- A method that needs a newer SQLite version than the floor throws an exception.

The enum is available on `SQLite.Framework` (where the loaded SQLite comes from the OS and the version varies by device) and on `SQLite.Framework.Base` (where you bring your own provider). The `SQLite.Framework.Bundled` and `SQLite.Framework.Cipher` packages ship a SQLite with a known version, so the enum only has `Unspecified` in those.

## .NET MAUI App

Use `FileSystem.AppDataDirectory` to get a path that works on every platform.

Add the optional [`SQLite.Framework.DependencyInjection`](Dependency%20Injection) package and call `AddSQLiteDatabase` in `MauiProgram.cs`. The DI container creates the database once (`ServiceLifetime.Singleton` a good choice for mobile apps) and shares it across your app.

```csharp
// MauiProgram.cs
public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();

        builder.UseMauiApp<App>();

        string dbPath = Path.Combine(FileSystem.AppDataDirectory, "library.db");
        builder.Services.AddSQLiteDatabase<AppDatabase>(
            b =>
            {
                b.DatabasePath = dbPath;
                b.MinimumSqliteVersion = SQLiteMinimumVersion.V3_35;
            },
            ServiceLifetime.Singleton);

        builder.Services.AddSingleton<LibraryPage>();
        builder.Services.AddSingleton<LibraryViewModel>();

        return builder.Build();
    }
}
```

See the [Dependency Injection](Dependency%20Injection) page for more overloads, including subclassed databases and factory-style registration.

Then accept it through the constructor in your ViewModel or page:

```csharp
public class LibraryViewModel
{
    private readonly SQLiteDatabase _db;

    public LibraryViewModel(SQLiteDatabase db)
    {
        _db = db;
    }

    public async Task InitializeAsync()
    {
        await _db.Schema.CreateTableAsync<Author>();
        await _db.Schema.CreateTableAsync<Book>();
    }

    public Task<List<Book>> GetBooksAsync()
    {
        return _db.Table<Book>().OrderBy(b => b.Title).ToListAsync();
    }
}
```

## Schema Setup

Call `CreateTableAsync<T>()` once at startup for each model.

```csharp
await db.Schema.CreateTableAsync<Author>();
await db.Schema.CreateTableAsync<Book>();
```

The models used throughout this wiki:

```csharp
using System.ComponentModel.DataAnnotations;
using SQLite.Framework.Attributes;

public class Author
{
    [Key]
    [AutoIncrement]
    public int Id { get; set; }

    [Required]
    public required string Name { get; set; }

    public string? Country { get; set; }
}

public class Book
{
    [Key]
    [AutoIncrement]
    public int Id { get; set; }

    [Required]
    public required string Title { get; set; }

    public int AuthorId { get; set; }

    [Required]
    public required decimal Price { get; set; }

    public DateTime PublishedAt { get; set; }

    public string? Genre { get; set; }

    public bool InStock { get; set; }
}
```

See [Defining Models](Defining%20Models) for the full list of attributes and options.

## Schema Migrations

SQLite stores a 32-bit integer in the database file header called the user version. It starts at zero and you control it entirely. Use it to track which migrations have already run so your app can apply only what is missing on each launch.

```csharp
await db.Schema.CreateTableAsync<Author>();
await db.Schema.CreateTableAsync<Book>();

if (db.Pragmas.UserVersion == 1)
{
    await db.ExecuteAsync("ALTER TABLE Books ADD COLUMN BookGenre TEXT");
    db.Pragmas.UserVersion = 2;
}
if (db.Pragmas.UserVersion == 2)
{
    await db.ExecuteAsync("ALTER TABLE Books ADD COLUMN BookInStock INTEGER NOT NULL DEFAULT 0");
    db.Pragmas.UserVersion = 3;
}
```

Each block runs only once. On the next launch `UserVersion` is already at the latest number, so the blocks are skipped.
