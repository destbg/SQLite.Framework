# Getting Started

## Installation

Install the NuGet package:

```bash
dotnet add package SQLite.Framework
```

## Console App

Create a `SQLiteDatabase` and pass a file path. The connection opens automatically on the first operation.

```csharp
using SQLiteDatabase db = new("library.db");

var authors = db.Table<Author>();
var books = db.Table<Book>();

await authors.CreateTableAsync();
await books.CreateTableAsync();

await authors.AddAsync(new Author { Name = "Robert Martin", Country = "USA" });
await books.AddAsync(new Book { Title = "Clean Code", AuthorId = 1, Price = 29.99m });

var results = await books.Where(b => b.Price < 50).ToListAsync();
```

`SQLiteDatabase` implements `IDisposable`, so use it inside a `using` block or statement.

## .NET MAUI App

Use `FileSystem.AppDataDirectory` to get a path that works on every platform.

Register `SQLiteDatabase` as a singleton in `MauiProgram.cs` so the DI container creates it once and shares it across your app.

```csharp
// MauiProgram.cs
public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();

        builder.UseMauiApp<App>();

        string dbPath = Path.Combine(FileSystem.AppDataDirectory, "library.db");
        builder.Services.AddSingleton(new SQLiteDatabase(dbPath));

        builder.Services.AddSingleton<LibraryPage>();
        builder.Services.AddSingleton<LibraryViewModel>();

        return builder.Build();
    }
}
```

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
