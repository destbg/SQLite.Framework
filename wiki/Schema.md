# Schema

The `db.Schema` property gives you DDL operations on the database: create and drop tables, add and remove indexes, alter columns, and inspect what is there.

## Why a separate class

In earlier versions, `CreateTable` and `DropTable` lived on `db.Table<T>()`. They are now on `db.Schema`. The old methods still work, but they are marked obsolete. New code should use the schema API.

```csharp
// New
db.Table<Book>().Schema.CreateTable(); // Uses fluent builder
// Or
db.Schema.CreateTable<Book>();

// Old (still works, gives an obsolete warning)
db.Table<Book>().CreateTable();
```

## Create and drop

```csharp
db.Table<Book>().Schema.CreateTable();
// Or
db.Schema.CreateTable<Book>();

db.Schema.DropTable<Book>();

// By table name when you do not have an entity class anymore
db.Schema.DropTable("OldBooks");
```

`CreateTable` is idempotent. It uses `CREATE TABLE IF NOT EXISTS` and creates any indexes you marked with `[Indexed]`.

## Indexes

```csharp
db.Schema.CreateIndex<Book>(b => b.Title);
db.Schema.CreateIndex<Book>(b => b.AuthorId, name: "IX_Book_Author", unique: true);
db.Schema.DropIndex("IX_Book_Author");
```

The default index name is `idx_{TableName}_{ColumnName}`.

## Fluent table builder

When you want to set up computed columns, CHECK constraints, or partial indexes alongside the table itself, use the fluent builder. Reach it with `db.Table<T>().Schema`. It records each option you chain, then issues all of the DDL when you call `CreateTable()`.

```csharp
db.Table<Book>().Schema
    .Computed(b => b.Total, b => b.Price * b.Quantity)
    .Computed(b => b.PriceWithTax, b => b.Price * 1.21m, stored: true)
    .Check(b => b.Price > 0, name: "CK_Price_Positive")
    .Check(b => b.Title.Length > 0)
    .Default(b => b.Rating, 0)
    .Default(b => b.Slug, () => SQLiteFunctions.SqliteVersion())
    .Index(b => b.Title)
    .Index(b => b.AuthorId, unique: true)
    .Index(b => b.CategoryId, filter: b => !b.Deleted)
    .Index(b => new { b.AuthorId, b.Genre }, name: "IX_AuthorGenre")
    .Strict()
    .CreateTable();
```

`Computed(target, sql, stored)` adds a generated column. The default is virtual (computed on every read). Pass `stored: true` to store the value on disk.

`Check(predicate, name)` adds a table-level CHECK constraint. The predicate is translated to SQL the same way `Where` clauses are.

`Default(column, ...)` sets the column's `DEFAULT` clause. Two overloads:

* `Default(column, value)` writes a literal.
* `Default(column, () => expression)` writes a translated SQL expression in parentheses.

`DateTime` columns are stored as ticks by default, so SQLite's `CURRENT_TIMESTAMP`/`datetime('now')` is not a good default for them. Set the value in C# before insert, or change the column to a different storage mode through [Storage Options](Storage%20Options).

For columns set this way, `Add` and `AddRange` omit the column from the INSERT when its CLR value equals `default(T)`. Same behavior as `[DefaultValue]` (see [Defining Models](Defining%20Models)).

`Index(column, name, unique, filter, collation, collations, direction, directions)` adds an index. Pass a single property for a single-column index, an anonymous object (`b => new { b.A, b.B }`) for a composite index, or any expression for an expression index. When `filter` is set, the result is a partial index (a `WHERE` clause on the index itself). Composite indexes cannot have a partial filter.

Use `collation` to apply one of the built-in collations (`NoCase`, `Rtrim`, `Binary`) to every column of the index. The default `SQLiteCollation.Inherit` emits no clause. For per-column collations on a composite index, pass `collations` as an array of the same length as the column list.

```csharp
.Index(b => b.Email, unique: true, collation: SQLiteCollation.NoCase)
.Index(b => new { b.LastName, b.FirstName },
    collations: [SQLiteCollation.NoCase, SQLiteCollation.NoCase])
```

Expression indexes accept any translatable expression, and a composite index can mix plain columns and expressions in the same slot list. The `name` argument is required when any slot is an expression because there is no stable default name for translated SQL. SQLite uses the indexed expression when a query contains the same expression in `WHERE`, `ORDER BY`, or a join.

```csharp
.Index(b => b.Title.ToLower(), name: "IX_Book_TitleLower")
.Index(b => new { b.AuthorId, Lowered = b.Title.ToLower() },
    name: "IX_Book_AuthorAndTitleLower")
```

Use `direction` to store every slot of the index in descending order, or pass `directions` as an array for per-slot control on a composite index. The default `SQLiteIndexDirection.Inherit` emits no clause, `Ascending` emits `ASC`, and `Descending` emits `DESC`. Sorting a slot in `DESC` lets the planner skip the extra sort step for matching `ORDER BY x DESC` queries. The `[Indexed]` attribute has a `Direction` property with the same effect.

```csharp
.Index(b => b.PublishedAt, name: "IX_Book_PublishedDesc",
    direction: SQLiteIndexDirection.Descending)
.Index(b => new { b.AuthorId, b.PublishedAt },
    name: "IX_Book_AuthorAndPublishedMixed",
    directions: [SQLiteIndexDirection.Ascending, SQLiteIndexDirection.Descending])
```

`Strict()` marks the table as a SQLite STRICT table. Same effect as the `[StrictTable]` attribute (see [Defining Models](Defining%20Models)). Requires SQLite 3.37.0 or newer.

Constants in computed, CHECK, default, and partial-index expressions are inlined as SQL literals because CREATE TABLE / CREATE INDEX cannot bind parameters. Only simple types (numbers, strings, bool) are supported as constants. For exotic types, use raw SQL through `db.Execute`.

## Existence checks

```csharp
bool hasBooks = db.Schema.TableExists<Book>();
bool hasIndex = db.Schema.IndexExists("IX_Book_Author");
bool hasTitle = db.Schema.ColumnExists<Book>("BookTitle");
```

## Inspection

```csharp
IReadOnlyList<string> tables = db.Schema.ListTables();
IReadOnlyList<string> indexes = db.Schema.ListIndexes();
IReadOnlyList<string> bookIndexes = db.Schema.ListIndexes("Books");

IReadOnlyList<SchemaColumnInfo> columns = db.Schema.ListColumns<Book>();
foreach (SchemaColumnInfo col in columns)
{
    Console.WriteLine($"{col.Name} {col.Type} nullable={col.IsNullable} pk={col.IsPrimaryKey}");
}
```

## Altering tables

```csharp
db.Schema.AddColumn<Book>("Subtitle");     // adds the Subtitle column from the entity
db.Schema.RenameColumn<Book>("BookTitle", "Title");
db.Schema.DropColumn<Book>("BookTitle");
db.Schema.RenameTable<Book>("RenamedBooks");
```

`AddColumn` takes a property name on the entity. The framework reads the type, nullability, and primary-key flags from your model and emits the right `ALTER TABLE ADD COLUMN` SQL.

A property selector overload is also available:

```csharp
db.Schema.AddColumn<Book>(b => b.Subtitle);
```

Pass `defaultValue` to emit a `DEFAULT` clause. You need this when you add a `NOT NULL` column to a table that already has rows. SQLite then uses the default to backfill existing rows.

```csharp
db.Schema.AddColumn<Book>(b => b.Pages, defaultValue: 0);
db.Schema.AddColumn<Book>(b => b.Genre, defaultValue: "Unknown");
```

SQLite does not let you use parameters inside DDL statements like `ALTER TABLE`. The framework writes `defaultValue` straight into the SQL text. Only numbers, `bool`, and `string` are accepted. Single quotes inside strings are doubled, so a value with quotes in it cannot escape from the string and run other SQL.

A second overload takes a translated SQL expression:

```csharp
db.Schema.AddColumn<Book>(b => b.Rating, () => 7 * 6);
```

Requires SQLite 3.31.0 or newer. SQLite also rejects non-constant defaults on `ADD COLUMN` when the table already has rows. Use them only on empty tables.

`RenameColumn`, `DropColumn`, and `RenameTable` take SQLite column or table names directly.

## Views

`db.Schema.CreateView<T>(...)` creates a SQL view from a LINQ expression. The view name comes from the `[Table("...")]` attribute on the entity, the body is the SQL produced by translating the lambda.

```csharp
db.Schema.CreateView<BookSummary>(() =>
    from b in db.Table<Book>()
    where b.Price > 0
    select new BookSummary { Id = b.Id, Title = b.Title, Price = b.Price });

bool exists = db.Schema.ViewExists<BookSummary>();
IReadOnlyList<string> views = db.Schema.ListViews();

db.Schema.DropView<BookSummary>();
db.Schema.DropView("vBookSummary");
```

The DDL uses `CREATE VIEW IF NOT EXISTS`, so calling `CreateView` twice is safe. Pair the view with `db.ReadOnlyTable<T>()` to query it.

SQLite does not allow placeholders inside view bodies, so any constants in the lambda are inlined as SQL literals when the view is created. Only simple types (numbers, strings, bool) work as inlined constants. For exotic types use raw SQL through `db.Execute`.

## Triggers

`db.Schema.CreateTrigger<T>(...)` creates a trigger on the table for `T`. The body and the optional `WHEN` predicate are raw SQL strings. Use `OLD` and `NEW` to refer to the row.

```csharp
db.Schema.CreateTrigger<Book>(
    name: "trg_book_history",
    timing: SQLiteTriggerTiming.After,
    @event: SQLiteTriggerEvent.Update,
    body: "INSERT INTO BookHistory(BookId, OldPrice, NewPrice) VALUES (NEW.Id, OLD.BookPrice, NEW.BookPrice)",
    when: "OLD.BookPrice <> NEW.BookPrice");

db.Schema.DropTrigger("trg_book_history");
```

`SQLiteTriggerTiming` is `Before`, `After`, or `InsteadOf`. `SQLiteTriggerEvent` is `Insert`, `Update`, or `Delete`. `InsteadOf` only works on views. The trigger runs once per row by default, pass `forEachRow: false` to run once per statement.

There is also a typed overload that builds the body from LINQ instead of a SQL string. Use the builder's `Old` and `New` rows, and add `Update`, `Insert`, or `Delete` statements. Columns and the `When` guard are checked at compile time.

```csharp
db.Schema.CreateTrigger<Book>("trg_book_history", SQLiteTriggerTiming.After, SQLiteTriggerEvent.Update, t => t
    .When(() => t.Old.Price != t.New.Price)
    .Insert(db.Table<BookHistory>(), s => s
        .Set(h => h.BookId, _ => t.New.Id)
        .Set(h => h.OldPrice, _ => t.Old.Price)
        .Set(h => h.NewPrice, _ => t.New.Price)));
```

## Async

Every method has an async wrapper that runs on a background thread:

```csharp
await db.Schema.CreateTableAsync<Book>();
await db.Schema.DropTableAsync<Book>();
await db.Schema.CreateIndexAsync<Book>(b => b.Title);
bool exists = await db.Schema.TableExistsAsync<Book>();
IReadOnlyList<SchemaColumnInfo> cols = await db.Schema.ListColumnsAsync<Book>();
await db.Schema.CreateViewAsync<BookSummary>(() => from b in db.Table<Book>() select new BookSummary { ... });
await db.Schema.CreateTriggerAsync<Book>("trg_x", SQLiteTriggerTiming.After, SQLiteTriggerEvent.Insert, "...");
```

## Customizing schema generation

To change how DDL is generated, inherit from `SQLiteSchema` and register the subclass:

```csharp
public sealed class MySchema : SQLiteSchema
{
    public MySchema(SQLiteDatabase database) : base(database) { }

    protected override IEnumerable<string> BuildTriggerSql(FtsTableInfo fts, TableMapping mapping)
    {
        // your own FTS5 trigger SQL
    }
}

builder.UseSchema(db => new MySchema(db));
```

The protected `virtual` methods on `SQLiteSchema` cover the FTS5 trigger generation paths. Override them when you want custom trigger shapes or naming conventions.

## Custom table types

`SQLiteDatabase.Table<T>()` is virtual. To return a custom `SQLiteTable<T>` subclass for a specific entity, override `Table<T>()` on your own `SQLiteDatabase` subclass:

```csharp
public class AppDatabase : SQLiteDatabase
{
    public AppDatabase(SQLiteOptions options) : base(options) { }

    public override SQLiteTable<T> Table<T>()
    {
        if (typeof(T) == typeof(Book))
        {
            return (SQLiteTable<T>)(object)new BookTable(this, TableMapping<Book>());
        }
        return base.Table<T>();
    }
}
```
