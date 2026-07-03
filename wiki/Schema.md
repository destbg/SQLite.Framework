# Schema

The `db.Schema` property gives you DDL operations on the database. Create and drop tables, add and remove indexes, alter columns and inspect what is there.

## Why a separate class

In earlier versions, `CreateTable` and `DropTable` lived on `db.Table<T>()`. They are now on `db.Schema`. The old methods still work, but they are marked obsolete. New code should use the schema API.

```csharp
// New
await db.Table<Book>().Schema.CreateTableAsync(); // table action handle
// Or
await db.Schema.CreateTableAsync<Book>();

// Old (still works, gives an obsolete warning)
await db.Table<Book>().CreateTableAsync();
```

## Create and drop

```csharp
await db.Table<Book>().Schema.CreateTableAsync();
// Or
await db.Schema.CreateTableAsync<Book>();

await db.Schema.DropTableAsync<Book>();

// By table name when you do not have an entity class anymore
await db.Schema.DropTableAsync("OldBooks");
```

`CreateTable` is idempotent. It uses `CREATE TABLE IF NOT EXISTS` and creates any indexes you marked with `[Indexed]`.

## Indexes

```csharp
await db.Schema.CreateIndexAsync<Book>(b => b.Title);
await db.Schema.CreateIndexAsync<Book>(b => b.AuthorId, name: "IX_Book_Author", unique: true);
await db.Schema.DropIndexAsync("IX_Book_Author");
```

The default index name is `idx_{TableName}_{ColumnName}`.

## Defining the model

Computed columns, CHECK constraints, indexes, foreign keys, defaults, triggers and the rest of the schema are declared once, in one place. Override `OnModelCreating` on your `SQLiteDatabase` subclass and configure each entity with `builder.Entity<T>()`. The framework calls `OnModelCreating` a single time, before any table is used, so create, migrate and validate all read the same definition.

```csharp
public class AppDatabase : SQLiteDatabase
{
    public AppDatabase(SQLiteOptions options) : base(options) { }

    protected override void OnModelCreating(SQLiteModelBuilder builder)
    {
        builder.Entity<Book>()
            .ToTable("Books")
            .HasKey(b => b.Id)
            .AutoIncrement(b => b.Id)
            .HasColumnName(b => b.Title, "title")
            .HasColumnType(b => b.Price, SQLiteColumnType.Real)
            .IsRequired(b => b.Title)
            .Ignore(b => b.Scratch)
            .Computed(b => b.Total, b => b.Price * b.Quantity)
            .Computed(b => b.PriceWithTax, b => b.Price * 1.21m, stored: true)
            .Check(b => b.Price > 0, name: "CK_Price_Positive")
            .Default(b => b.Rating, 0)
            .Index(b => b.AuthorId)
            .Index(b => new { b.AuthorId, b.Genre }, name: "IX_AuthorGenre")
            .ForeignKey<Author>(b => b.AuthorId, onDelete: SQLiteForeignKeyAction.Cascade)
            .Column("RowVersion", SQLiteColumnType.Integer, nullable: false, defaultSql: "0")
            .Strict()
            .Trigger("trg_Book_Audit", SQLiteTriggerTiming.After, SQLiteTriggerEvent.Insert, t => t
                .Insert(Table<AuditLog>(), set => set.Set(a => a.BookId, _ => t.New.Id)));
    }
}
```

Everything the mapping attributes can do is available here too, so you can keep entities as plain classes and put all the schema in `OnModelCreating` or mix attributes and the builder.

Table-level configuration:

* `ToTable(name)` sets the table name, same as the `[Table]` attribute.
* `HasKey(b => b.Id)` or `HasKey(b => new { b.A, b.B })` sets the primary key, same as `[Key]`. It replaces any key already declared on the columns.
* `WithoutRowId()` marks the table WITHOUT ROWID, same as `[WithoutRowId]`.
* `Strict()` marks the table as a SQLite STRICT table, same as `[StrictTable]`. Requires SQLite 3.37.0 or newer.

Column-level configuration:

* `HasColumnName(b => b.Title, "title")` sets the column name, same as `[Column]`.
* `HasColumnType(b => b.Price, SQLiteColumnType.Real)` overrides the storage type.
* `IsRequired(b => b.Title)` makes the column NOT NULL, same as `[Required]`. Pass `required: false` to allow NULL.
* `AutoIncrement(b => b.Id)` marks an auto-incrementing primary key, same as `[AutoIncrement]`.
* `Ignore(b => b.Scratch)` drops the property from the model, same as `[NotMapped]`.

`Computed(target, sql, stored)` adds a generated column. The default is virtual (computed on every read). Pass `stored: true` to store the value on disk.

`Check(predicate, name)` adds a table-level CHECK constraint. The predicate is translated to SQL the same way `Where` clauses are.

`ForeignKey<TParent>(column, ...)` declares a foreign key, same as `[ReferencesTable]`. Pass a target selector for non-key targets or composite keys. See [Defining Models](Defining%20Models) for the full foreign key options.

`Default(column, ...)` sets the column's `DEFAULT` clause. Two overloads:

* `Default(column, value)` writes a literal.
* `Default(column, () => expression)` writes a translated SQL expression in parentheses.

`DateTime` columns are stored as ticks by default, so SQLite's `CURRENT_TIMESTAMP`/`datetime('now')` is not a good default for them. Set the value in C# before insert or change the column to a different storage mode through [Storage Options](Storage%20Options).

For columns set this way, `Add` and `AddRange` omit the column from the INSERT when its CLR value equals `default(T)`. Same behavior as `[DefaultValue]` (see [Defining Models](Defining%20Models)).

`Index(column, name, unique, filter, collation, collations, direction, directions)` adds an index. Pass a single property for a single-column index, an anonymous object (`b => new { b.A, b.B }`) for a composite index or any expression for an expression index. When `filter` is set, the result is a partial index (a `WHERE` clause on the index itself). Composite indexes cannot have a partial filter.

Use `collation` to apply one of the built-in collations (`NoCase`, `Rtrim`, `Binary`) to every column of the index. The default `SQLiteCollation.Inherit` emits no clause. For per-column collations on a composite index, pass `collations` as an array of the same length as the column list.

```csharp
.Index(b => b.Email, unique: true, collation: SQLiteCollation.NoCase)
.Index(b => new { b.LastName, b.FirstName },
    collations: [SQLiteCollation.NoCase, SQLiteCollation.NoCase])
```

Expression indexes accept any translatable expression and a composite index can mix plain columns and expressions in the same slot list. The `name` argument is required when any slot is an expression because there is no stable default name for translated SQL. SQLite uses the indexed expression when a query contains the same expression in `WHERE`, `ORDER BY` or a join.

```csharp
.Index(b => b.Title.ToLower(), name: "IX_Book_TitleLower")
.Index(b => new { b.AuthorId, Lowered = b.Title.ToLower() },
    name: "IX_Book_AuthorAndTitleLower")
```

Use `direction` to store every slot of the index in descending order or pass `directions` as an array for per-slot control on a composite index. The default `SQLiteIndexDirection.Inherit` emits no clause, `Ascending` emits `ASC` and `Descending` emits `DESC`. Sorting a slot in `DESC` lets the planner skip the extra sort step for matching `ORDER BY x DESC` queries. The `[Indexed]` attribute has a `Direction` property with the same effect.

`Column(name, type, nullable, defaultSql)` adds a column that has no CLR property. The framework creates it and keeps it across a migrate rebuild, but never reads or writes it on its own. To read it in a query or write it on a save, reference it with `SQLiteColumn.Of<T>(row, "Name")`:

```csharp
// Read in a query (Where, Select, OrderBy, GroupBy):
var recent = await db.Table<Book>()
    .Where(b => SQLiteColumn.Of<long>(b, "UpdatedAt") > cutoff)
    .OrderByDescending(b => SQLiteColumn.Of<long>(b, "UpdatedAt"))
    .ToListAsync();

// Write on Add or Update (see WithColumns in CRUD Operations):
await db.Table<Book>()
    .WithColumns(c => c.Set(b => SQLiteColumn.Of<long>(b, "UpdatedAt"), _ => SQLiteFunctions.UnixEpoch()))
    .UpdateAsync(book);
```

`Trigger(name, timing, event, build)` declares a trigger whose body is built from typed LINQ statements. The trigger becomes part of the model, so create and migrate manage it. Reference target tables through the database's own `Table<TTarget>()`, which is in scope inside `OnModelCreating`. See the [Triggers](Triggers) page.

Constants in computed, CHECK, default and partial-index expressions are inlined as SQL literals because CREATE TABLE / CREATE INDEX cannot bind parameters. Only simple types (numbers, strings, bool) are supported as constants. For exotic types, use raw SQL through `db.Execute`.

### Running the schema actions

Configuration lives only in `OnModelCreating`. To act on a table, reach its action handle with `db.Schema.Table<T>()` or `db.Table<T>().Schema`, which expose `CreateTable()` and `ValidateModel()`. `db.Schema.CreateTable<T>()` is the same as `db.Schema.Table<T>().CreateTable()`. To bring a live database up to the model, use the migration runner `db.Schema.Migrations()` described on the [Migrations](Migrations) page.

```csharp
await db.Schema.CreateTableAsync<Book>();          // create with all declared indexes and triggers
await db.Table<Book>().Schema.ValidateModelAsync();
```

Earlier versions configured the schema at the call site, like `db.Schema.Table<Book>().Index(...).CreateTable()`. That is gone. Move those calls into `OnModelCreating` and call the action by itself.

## Existence checks

```csharp
bool hasBooks = await db.Schema.TableExistsAsync<Book>();
bool hasIndex = await db.Schema.IndexExistsAsync("IX_Book_Author");
bool hasTitle = await db.Schema.ColumnExistsAsync<Book>("BookTitle");
```

## Inspection

```csharp
IReadOnlyList<string> tables = db.Schema.ListTables();
IReadOnlyList<string> indexes = db.Schema.ListIndexes();
IReadOnlyList<string> bookIndexes = db.Schema.ListIndexes("Books");

IReadOnlyList<SchemaColumnInfo> columns = await db.Schema.ListColumnsAsync<Book>();
foreach (SchemaColumnInfo col in columns)
{
    Console.WriteLine($"{col.Name} {col.Type} nullable={col.IsNullable} pk={col.IsPrimaryKey}");
}
```

## Validate the model

`ValidateModel<T>()` compares the model against the live database and returns the issues found. It is also reachable on the table action handle as `db.Schema.Table<T>().ValidateModel()` and `db.Table<T>().Schema.ValidateModel()`.

```csharp
SQLiteModelValidationResult result = await db.Schema.ValidateModelAsync<Book>();
if (!result.IsValid)
{
    foreach (string issue in result.Issues)
    {
        Console.WriteLine(issue);
    }
}
```

It checks columns (missing, extra, type, primary key, nullability), declared indexes, foreign keys and declared triggers. Columns declared with `Column(...)` that have no CLR property are expected, not flagged as extra. Virtual tables (FTS5, R-Tree) only have their existence checked.

## Migrate

Migrations bring a live database up to the current model. They are versioned, run in one transaction and record their progress in `PRAGMA user_version`, so a version that already ran is skipped on the next run.

```csharp
await db.Schema.Migrations()
    .Version(1, m => m.CreateTable<Book>().CreateTable<Author>())
    .Version(2, m => m.TableChanged<Author>())
    .MigrateAsync();
```

The [Migrations](Migrations) page covers the whole system. How a run is ordered, what `TableChanged` reconciles, filling new columns, renames, drops, data steps and previewing a run with `Plan()`. For one-off column changes outside a migration, see the next section.

## Altering tables

```csharp
await db.Schema.AddColumnAsync<Book>("Subtitle");     // adds the Subtitle column from the entity
await db.Schema.RenameColumnAsync<Book>("BookTitle", "Title");
await db.Schema.DropColumnAsync<Book>("BookTitle");
await db.Schema.RenameTableAsync<Book>("RenamedBooks");
```

`AddColumn` takes a property name on the entity. The framework reads the type, nullability and primary-key flags from your model and emits the right `ALTER TABLE ADD COLUMN` SQL.

A property selector overload is also available:

```csharp
await db.Schema.AddColumnAsync<Book>(b => b.Subtitle);
```

Pass `defaultValue` to emit a `DEFAULT` clause. You need this when you add a `NOT NULL` column to a table that already has rows. SQLite then uses the default to backfill existing rows.

```csharp
await db.Schema.AddColumnAsync<Book>(b => b.Pages, defaultValue: 0);
await db.Schema.AddColumnAsync<Book>(b => b.Genre, defaultValue: "Unknown");
```

SQLite does not let you use parameters inside DDL statements like `ALTER TABLE`. The framework writes `defaultValue` straight into the SQL text. Only numbers, `bool` and `string` are accepted. Single quotes inside strings are doubled, so a value with quotes in it cannot escape from the string and run other SQL.

SQLite requires a column added with a foreign key to default to NULL. Adding a foreign key column with a non-null `defaultValue` or a default expression throws. Add it with a null default or recreate the table with the new schema instead.

A second overload takes a translated SQL expression:

```csharp
await db.Schema.AddColumnAsync<Book>(b => b.Rating, () => 7 * 6);
```

Requires SQLite 3.31.0 or newer. SQLite also rejects non-constant defaults on `ADD COLUMN` when the table already has rows. Use them only on empty tables.

`RenameColumn`, `DropColumn` and `RenameTable` take SQLite column or table names directly.

## Views

`db.Schema.CreateView<T>(...)` creates a SQL view from a LINQ expression and `db.ReadOnlyTable<T>()` queries it. See the [Views](Views) page for read models, changing a view's definition and writable views.

## Triggers

`db.Schema.CreateTrigger<T>(...)` creates a trigger with a raw SQL or typed LINQ body. `Trigger(...)` in `OnModelCreating` declares one on the model so create and migrate manage it. See the [Triggers](Triggers) page for the builder API and use cases like audit logs and denormalized counters.

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

The protected `virtual` methods on `SQLiteSchema` cover the FTS5 trigger generation paths. Override them to change the trigger shapes or naming conventions.

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
