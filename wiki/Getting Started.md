# Getting Started

## Installation

Install the NuGet package:

```bash
dotnet add package SQLite.Framework
```

## Console App

Build a read-only `SQLiteOptions` with `SQLiteOptionsBuilder` and hand it to the `SQLiteDatabase` constructor. The database path lives on the options. The connection opens automatically on the first operation.

```csharp
SQLiteOptions options = new SQLiteOptionsBuilder("library.db").Build();
using SQLiteDatabase db = new(options);

var authors = db.Table<Author>();
var books = db.Table<Book>();

await authors.CreateTableAsync();
await books.CreateTableAsync();

await authors.AddAsync(new Author { Name = "Robert Martin", Country = "USA" });
await books.AddAsync(new Book { Title = "Clean Code", AuthorId = 1, Price = 29.99m });

var results = await books.Where(b => b.Price < 50).ToListAsync();
```

`SQLiteDatabase` implements `IDisposable`, so use it inside a `using` block or statement.

### Why a builder?

`SQLiteOptionsBuilder` is mutable and lets you chain `Use*` / `Add*` calls. Once you call `Build()`, the returned `SQLiteOptions` is fully immutable, this makes it safe to share through dependency injection and reuse across databases without worrying about a late change affecting live code paths.

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
        builder.Services.AddSQLiteDatabase(
            b => b.DatabasePath = dbPath,
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
        await _db.Table<Author>().CreateTableAsync();
        await _db.Table<Book>().CreateTableAsync();
    }

    public Task<List<Book>> GetBooksAsync()
    {
        return _db.Table<Book>().OrderBy(b => b.Title).ToListAsync();
    }
}
```

## Schema Setup

Call `CreateTableAsync()` once at startup for each model.

```csharp
await db.Table<Author>().CreateTableAsync();
await db.Table<Book>().CreateTableAsync();
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
await db.Table<Author>().CreateTableAsync();
await db.Table<Book>().CreateTableAsync();

if (db.UserVersion == 1)
{
    db.Execute("ALTER TABLE Books ADD COLUMN BookGenre TEXT");
    db.UserVersion = 2;
}
if (db.UserVersion == 2)
{
    db.Execute("ALTER TABLE Books ADD COLUMN BookInStock INTEGER NOT NULL DEFAULT 0");
    db.UserVersion = 3;
}
```

Each block runs only once. On the next launch `UserVersion` is already at the latest number, so the blocks are skipped.

There are the async versions:

```csharp
int version = await db.GetUserVersionAsync();
await db.SetUserVersionAsync(2);
```
