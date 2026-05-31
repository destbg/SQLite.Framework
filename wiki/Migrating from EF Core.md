# Migrating from Entity Framework Core

This page is for people who already use Entity Framework Core and want to move to `SQLite.Framework`. It points out the parts that look the same, the parts that look different, and the small changes you need to make in your code.

`SQLite.Framework` is built only for SQLite. It does not try to be a full ORM like EF Core. It has no automatic change tracker, unit of work, lazy loading, or navigation properties. It does give you lightweight stand-ins for the heavier parts. Write hooks handle before-save logic, and `Migrate` with the user-version pragma handles schema versioning. It just lets you use LINQ over SQLite tables and run CRUD operations.

## DbContext Becomes a SQLiteDatabase Subclass

EF Core has you write a class that inherits from `DbContext` and exposes a `DbSet<T>` for every table. `SQLite.Framework` works the same way. You write a class that inherits from `SQLiteDatabase` and exposes a `SQLiteTable<T>` for every table. The base class gives you a `Table<T>()` method that returns the typed table, the same way `DbContext` gives you `Set<T>()`.

```csharp
// EF Core
public class AppDbContext : DbContext
{
    public DbSet<Book> Books => Set<Book>();
    protected override void OnConfiguring(DbContextOptionsBuilder b) =>
        b.UseSqlite("Data Source=app.db");
}

// SQLite.Framework
public class AppDatabase : SQLiteDatabase
{
    public AppDatabase(SQLiteOptions options) : base(options) { }

    public SQLiteTable<Book> Books => Table<Book>();
    public SQLiteTable<Author> Authors => Table<Author>();
}
```

You build the options once and pass them in:

```csharp
SQLiteOptions options = new SQLiteOptionsBuilder("app.db").Build();
using AppDatabase db = new(options);
```

The subclass form is the recommended pattern. It puts every table in one place and matches what EF Core users expect. You can also use `SQLiteDatabase` directly for a quick script, in which case you call `db.Table<Book>()` instead of `db.Books`.

You can register the database in dependency injection. See [Dependency Injection](Dependency%20Injection) for the helper that wires it up.

## Defining Models

EF Core lets you describe a model in two places: attributes (`DataAnnotations`) on the class, and the Fluent API in `OnModelCreating`. In `SQLite.Framework`, the column-level metadata (primary key, column name, table name, what to skip) lives on the class as attributes. There is no `OnModelCreating`.

The attribute names are the same as the ones EF Core understands. `[Key]` marks the primary key, `[Table]` and `[Column]` rename the table and column, `[NotMapped]` skips a property. The only new attribute is `[AutoIncrement]`, which tells SQLite to assign the primary key for you.

```csharp
[Table("Books")]
public class Book
{
    [Key]
    [AutoIncrement]
    public int Id { get; set; }

    [Column("BookTitle")]
    public required string Title { get; set; }

    public int AuthorId { get; set; }
    public decimal Price { get; set; }
}
```

See [Defining Models](Defining%20Models) for the full list.

## No SaveChanges

In EF Core you change tracked entities and call `SaveChanges` to flush them. In `SQLite.Framework` every CRUD method writes to the database right away. There is no tracker and there is no batching layer. If you want to batch, you can still wrap the writes in a transaction.

```csharp
// EF Core
db.Books.Add(book);
book.Price = 19.99m;
await db.SaveChangesAsync();

// SQLite.Framework
await db.Books.AddAsync(book);
book.Price = 19.99m;
await db.Books.UpdateAsync(book);
```

The methods are similar to EF Core's `DbSet` methods, only you call them on the property you exposed on your `SQLiteDatabase` subclass. See [CRUD Operations](CRUD%20Operations) for the full list.

## AutoIncrement Keys: Match EF Core's Behavior

By default, EF Core's `Add` looks at the primary key. If the value is the type default (zero for an `int`), EF Core asks the database to assign one. If the value is set to anything else, EF Core uses that value when inserting.

`SQLite.Framework`'s default is different. By default, `Add` always lets SQLite assign the key, even if you set a value on the entity first. Your value gets overwritten.

If you want the EF Core behavior, turn on the option:

```csharp
SQLiteOptions options = new SQLiteOptionsBuilder("app.db")
    .PreserveExplicitAutoIncrementKeys()
    .Build();
```

With the option on:

- `book.Id == 0` (the default) -> SQLite assigns a new id, the same as before.
- `book.Id == 5` -> the row is inserted with id 5. If a row with id 5 already exists, SQLite throws a uniqueness error.

This option only changes `Add` and `AddRange`. `AddOrUpdate` already respects the value you set on the primary key, with or without the option.

## Querying with LINQ

LINQ works the same way as in EF Core for the common cases. `Where`, `OrderBy`, `Select`, `Take`, `Skip`, `Count`, `Any`, `All`, `First`, `Single`, joins, group by, and projections are all translated to SQL.

```csharp
// EF Core
var titles = await db.Books
    .Where(b => b.Price < 30)
    .OrderBy(b => b.Title)
    .Select(b => b.Title)
    .ToListAsync();

// SQLite.Framework
List<string> titles = await db.Books
    .Where(b => b.Price < 30)
    .OrderBy(b => b.Title)
    .Select(b => b.Title)
    .ToListAsync();
```

For more complex queries, see [Querying](Querying), [Joins](Joins), [Subqueries](Subqueries), [Grouping and Aggregates](Grouping%20and%20Aggregates), and [Window Functions](Window%20Functions).

## No Navigation Properties or Include

EF Core lets you define navigation properties (a `Book` has an `Author`, a `User` has a list of `Orders`), and load them with `Include`. `SQLite.Framework` does not do this.

```csharp
// EF Core
var withAuthor = await db.Books
    .Include(b => b.Author)
    .ToListAsync();

// SQLite.Framework
var withAuthor = await db.Books
    .Join(db.Authors,
        b => b.AuthorId,
        a => a.Id,
        (b, a) => new { Book = b, Author = a })
    .ToListAsync();
```

This means a bit more code for one query, but it also means you always know what SQL gets sent and you cannot accidentally pull a whole graph by mistake. `SQLite.Framework` focuses on making sure the query you write gets translated as closely as possible to SQL to avoid potential slowdowns from bad optimizations. See [Joins](Joins) for the full set of join shapes.

## No Lazy Loading

In EF Core, you can set things up so that simply reading a navigation property on an entity causes a SQL query to run in the background. For example, if you load an `Author` and then read `author.Books`, EF Core notices the read and quietly fetches the books from the database. You did not write a query, the property access triggered one. The data is loaded only when you ask for it, hence "lazy".

`SQLite.Framework` does not do this. There are no navigation properties on your models in the first place, so nothing could trigger a hidden query.

## Transactions

EF Core uses `db.Database.BeginTransaction()` and you commit it at the end. `SQLite.Framework` looks similar, only the call sits on the database directly.

```csharp
// EF Core
using var tx = await db.Database.BeginTransactionAsync();
db.Books.Add(book);
await db.SaveChangesAsync();
await tx.CommitAsync();

// SQLite.Framework
await using SQLiteTransaction tx = await db.BeginTransactionAsync();
await db.Books.AddAsync(book);
await tx.CommitAsync();
```

If you forget to call `Commit`, the transaction is rolled back when it is disposed. See [Transactions](Transactions) for more details.

## Migrations

There is **no** migration-file system like EF Core's, with generated up and down scripts and a `__EFMigrationsHistory` table. Instead the model is the source of truth, and `Migrate` reconciles the live database with it. It creates a missing table, rebuilds a table that has drifted while keeping its rows, and reconciles indexes and triggers. Track the schema version with the SQLite user-version pragma and apply what is missing on startup.

```csharp
if (db.Pragmas.UserVersion < 1)
{
    await db.Table<Book>().Schema.MigrateAsync();
    db.Pragmas.UserVersion = 1;
}
```

You can also manage the schema by hand with `db.Schema.CreateTable<T>()` and the other methods on `Schema`.

```csharp
await db.Schema.CreateTableAsync<Book>();
await db.Schema.CreateIndexAsync<Book>(b => b.AuthorId);
```

For things you cannot express with attributes, like indexes, computed columns, partial indexes, and CHECK constraints, override `OnModelCreating` on your `SQLiteDatabase` subclass and configure each entity with `builder.Entity<T>()`. This works just like EF Core's `OnModelCreating`.

```csharp
protected override void OnModelCreating(SQLiteModelBuilder builder)
{
    builder.Entity<Book>()
        .Index(b => b.AuthorId)
        .Index(b => b.Title, unique: true)
        .Check(b => b.Price >= 0, name: "ck_book_price_non_negative");
}
```

Then create the table with `db.Schema.CreateTable<Book>()`.

`Migrate` handles most drift automatically, including column type changes and added or removed columns. For one-off changes you can also use the helper methods on `Schema`. See [Schema](Schema) for `Migrate`, the per-table actions, and `ValidateModel`.

## Async Support

EF Core has async versions of every method (`ToListAsync`, `FirstAsync`, `AddAsync`, and so on). `SQLite.Framework` adds the same methods through extension methods. The names match. You only need to add the right `using` to make them show up.

```csharp
using SQLite.Framework.Extensions;
```

The async methods run the work on a thread pool. SQLite itself is synchronous under the hood, so you do not get more parallelism out of going async, but you do not block the calling thread either. See [Multi-threading](Multi-threading) for how to run several writes at the same time.

## Concurrency, Identity Resolution, and Change Tracking

EF Core has all of these built in. `SQLite.Framework` has none of them.

- There is no `[Timestamp]` or row version. If you need optimistic concurrency, add a `Version` column and check it in your `Update` SQL.
- There is no identity map. Two queries that return the same row give you two different objects. Comparing them by reference does not work, you have to compare by primary key.
- There is no change tracker. The library does not know which properties you changed. `Update` writes every column. Use `ExecuteUpdate` for more control over which columns get updated.

For the before-write logic people often reach for the change tracker, such as audit timestamps or derived values, use write hooks (`OnAdd`/`OnUpdate`), which can also fill columns that have no CLR property. See [Hooks](CRUD%20Operations#hooks).

For most apps that use SQLite, this is fine. If you depend on the rest of these features, plan for a bit of extra code.
