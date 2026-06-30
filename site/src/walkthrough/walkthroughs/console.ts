import type { Walkthrough } from "./types";

export const consoleWalkthrough: Walkthrough = {
    slug: "console",
    title: "Console Walkthrough",
    subtitle: "Get a feel for the LINQ surface in a plain console app",
    steps: [
        {
            title: "What you will build",
            description:
                "A tiny console app that creates a SQLite database, seeds it, and runs a handful of LINQ queries. Pure C# with no UI in the way.",
        },
        {
            title: "Create the project",
            description:
                "A bare console template is enough. Add the framework package.",
            code: {
                language: "bash",
                text: `dotnet new console -n MyConsole
cd MyConsole
dotnet add package SQLite.Framework`,
            },
        },
        {
            title: "Define your entities",
            description:
                "Two related tables: Authors and Books. The attributes mark primary keys, auto-increment, and required columns.",
            code: {
                language: "csharp",
                filename: "Models.cs",
                text: `using System.ComponentModel.DataAnnotations;
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

    public required decimal Price { get; set; }

    public DateTime PublishedAt { get; set; }
}`,
            },
        },
        {
            title: "Open a database",
            description:
                "Build SQLiteOptions with the builder. The connection opens lazily on the first operation. Always wrap the database in a using block.",
            code: {
                language: "csharp",
                filename: "Program.cs",
                text: `using SQLite.Framework;

SQLiteOptions options = new SQLiteOptionsBuilder("library.db")
    .UseMinimumSqliteVersion(SQLiteMinimumVersion.V3_36)
    .Build();

using SQLiteDatabase db = new(options);`,
            },
        },
        {
            title: "Create the schema",
            description:
                "Call CreateTableAsync<T>() for each entity at startup. It is a no-op when the table already exists.",
            code: {
                language: "csharp",
                filename: "Program.cs",
                text: `await db.Schema.CreateTableAsync<Author>();
await db.Schema.CreateTableAsync<Book>();`,
            },
        },
        {
            title: "Seed some data",
            description:
                "AddAsync inserts a single row. AddRangeAsync inserts many in a single transaction. Both populate the auto-increment Id on the way back.",
            code: {
                language: "csharp",
                filename: "Program.cs",
                text: `var authors = db.Table<Author>();
var books = db.Table<Book>();

if (await authors.CountAsync() == 0)
{
    await authors.AddAsync(new Author { Name = "Robert Martin", Country = "USA" });
    await authors.AddAsync(new Author { Name = "Andrew Hunt", Country = "USA" });

    await books.AddRangeAsync(new[]
    {
        new Book { Title = "Clean Code", AuthorId = 1, Price = 29.99m, PublishedAt = new(2008, 8, 1) },
        new Book { Title = "The Pragmatic Programmer", AuthorId = 2, Price = 39.99m, PublishedAt = new(1999, 10, 30) },
    });
}`,
            },
        },
        {
            title: "Filter with Where",
            description:
                "LINQ in, SQL out. The framework translates the predicate to a parameterised WHERE clause.",
            code: {
                language: "csharp",
                filename: "Program.cs",
                text: `List<Book> affordable = await books
    .Where(b => b.Price < 30)
    .OrderBy(b => b.Title)
    .ToListAsync();

foreach (Book b in affordable)
{
    Console.WriteLine($"{b.Title} - {b.Price:C}");
}`,
            },
        },
        {
            title: "Join two tables",
            description:
                "Use either method syntax or query syntax. Both compile to the same SQL.",
            code: {
                language: "csharp",
                filename: "Program.cs",
                text: `var titlesWithAuthors = await (
    from b in books
    join a in authors on b.AuthorId equals a.Id
    where b.Price < 50
    orderby b.PublishedAt descending
    select new { b.Title, Author = a.Name, b.Price }
).ToListAsync();

foreach (var row in titlesWithAuthors)
{
    Console.WriteLine($"{row.Title} by {row.Author} - {row.Price:C}");
}`,
            },
        },
        {
            title: "Group and aggregate",
            description:
                "Group by author, project to an anonymous type with Count and Sum. SQLite runs the aggregation, the framework hands you the shaped rows.",
            code: {
                language: "csharp",
                filename: "Program.cs",
                text: `var perAuthor = await (
    from b in books
    join a in authors on b.AuthorId equals a.Id
    group b by a.Name into g
    orderby g.Sum(x => x.Price) descending
    select new
    {
        Author = g.Key,
        Titles = g.Count(),
        Revenue = g.Sum(x => x.Price),
    }
).ToListAsync();

foreach (var row in perAuthor)
{
    Console.WriteLine($"{row.Author}: {row.Titles} titles, {row.Revenue:C}");
}`,
            },
        },
        {
            title: "Bulk update and delete",
            description:
                "ExecuteUpdateAsync and ExecuteDeleteAsync mutate many rows in a single round trip without loading them first.",
            code: {
                language: "csharp",
                filename: "Program.cs",
                text: `int marked = await books
    .Where(b => b.PublishedAt < new DateTime(2010, 1, 1))
    .ExecuteUpdateAsync(s => s.Set(b => b.Price, b => b.Price * 0.9m));

Console.WriteLine($"Discounted {marked} books.");

int removed = await books
    .Where(b => b.Price > 100)
    .ExecuteDeleteAsync();

Console.WriteLine($"Removed {removed} expensive books.");`,
            },
        },
        {
            title: "Wrap things in a transaction",
            description:
                "The framework uses SQLite savepoints under the hood, so transactions nest cleanly.",
            code: {
                language: "csharp",
                filename: "Program.cs",
                text: `await using (var tx = await db.BeginTransactionAsync())
{
    await books.AddAsync(new Book
    {
        Title = "New Release",
        AuthorId = 1,
        Price = 24.99m,
        PublishedAt = DateTime.UtcNow,
    });

    await tx.CommitAsync();
}`,
            },
        },
        {
            title: "You are done",
            description:
                "You have a working database, seed data, joins, group-by, bulk operations, and transactions, all in one short Program.cs. From here you have the full LINQ surface to build on.",
        },
    ],
};
