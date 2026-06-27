# Schema

The `db.Schema` property gives you DDL operations on the database: create and drop tables, add and remove indexes, alter columns, and inspect what is there.

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

Computed columns, CHECK constraints, indexes, foreign keys, defaults, triggers, and the rest of the schema are declared once, in one place. Override `OnModelCreating` on your `SQLiteDatabase` subclass and configure each entity with `builder.Entity<T>()`. The framework calls `OnModelCreating` a single time, before any table is used, so create, migrate, and validate all read the same definition.

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

Everything the mapping attributes can do is available here too, so you can keep entities as plain classes and put all the schema in `OnModelCreating`, or mix attributes and the builder.

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

`Trigger(name, timing, event, build)` declares a trigger whose body is built from typed LINQ statements. The trigger becomes part of the model, so create and migrate manage it. Reference target tables through the database's own `Table<TTarget>()`, which is in scope inside `OnModelCreating`. See [Triggers](#triggers) below.

Constants in computed, CHECK, default, and partial-index expressions are inlined as SQL literals because CREATE TABLE / CREATE INDEX cannot bind parameters. Only simple types (numbers, strings, bool) are supported as constants. For exotic types, use raw SQL through `db.Execute`.

### Running the schema actions

Configuration lives only in `OnModelCreating`. To act on a table, reach its action handle with `db.Schema.Table<T>()` or `db.Table<T>().Schema`, which expose `CreateTable()` and `ValidateModel()`. `db.Schema.CreateTable<T>()` is the same as `db.Schema.Table<T>().CreateTable()`. To bring a live database up to the model, use the migration runner `db.Schema.Migrations()` described under [Migrate](#migrate).

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

It checks columns (missing, extra, type, primary key, nullability), declared indexes, foreign keys, and declared triggers. Columns declared with `Column(...)` that have no CLR property are expected, not flagged as extra. Virtual tables (FTS5, R-Tree) only have their existence checked.

## Migrate

Migrations are versioned. Reach the runner with `db.Schema.Migrations()`, declare each schema version, then apply it. The runner brings the database up to the current model and records the version it reached in `PRAGMA user_version`, so a version that already ran is skipped on the next run.

```csharp
await db.Schema.Migrations()
    .Version(1, m => m.TableChanged<Book>())
    .Version(2, m => m.TableChanged<Author>())
    .MigrateAsync();
```

`TableChanged<T>()` reconciles the table for `T` to the current model. What it does:

* Creates the table when it does not exist.
* Adds new columns in place, and drops columns the model no longer has. When a change cannot be made in place, it rebuilds the table the way SQLite recommends. It creates a new table from the model, copies the rows, drops the old table, and renames the new one. Pass `rebuild: true` to always rebuild, which works on any SQLite version.
* Preserves the rows for every column the model keeps. A removed column loses its data, a new column gets NULL or its default, and a type change keeps the values.
* Creates or recreates declared indexes and triggers, and drops indexes that are no longer declared. Triggers that are not declared on the model are left alone.

A whole run happens in one transaction. If a step fails, the run rolls back to the version it started at, and the next run retries from there. FTS5 and R-Tree tables are only ensured to exist.

Migrations always move toward the current model. There is no path back to an older version, and no way to stop below the highest declared version.

### See what a migration would do

`Plan()` reads the version recorded in the database and reports what a migrate would run, without changing anything.

```csharp
SQLiteMigrationPlan plan = await db.Schema.Migrations()
    .Version(1, m => m.TableChanged<Book>())
    .PlanAsync();

if (!plan.IsUpToDate)
{
    foreach (string step in plan.Operations)
    {
        Console.WriteLine(step);
    }
}
```

### Filling new columns

A new `NOT NULL` column with no default cannot be filled by copying old rows. If the table has rows, the run stops with a clear error that names the column. You have three ways to fix it. Give the column a default in `OnModelCreating`, make it nullable, or pass values to `TableChanged`.

`TableChanged<T>(s => s.Set(...))` fills or overrides columns during the reconcile. Each value is read from the old row. The runner unions the fills from every pending version before it reconciles, so a column added in a later version does not make an earlier version stop.

```csharp
await db.Schema.Migrations()
    .Version(1, m => m.TableChanged<Book>(s => s
        .Set(b => b.Status, "active")          // constant for every row
        .Set(b => b.Slug, b => b.Title)))      // expression over the old row
    .MigrateAsync();
```

The expression form is translated to SQL and runs over the old row, the same way CHECK and computed columns are. To read or write a column that has no CLR property, use `SQLiteColumn.Of<T>(row, "Name")`:

```csharp
await db.Schema.Migrations()
    .Version(1, m => m.TableChanged<Book>(s => s
        .Set(b => SQLiteColumn.Of<string>(b, "Slug"), b => b.Title)))
    .MigrateAsync();
```

A column you do not set is copied across unchanged when it still exists.

### Renames, drops, and data steps

A reconcile cannot tell a rename from a drop plus an add, so rename a column with an explicit step. Renames are applied before the reconcile, so the data is kept.

```csharp
await db.Schema.Migrations()
    .Version(1, m => m
        .RenameColumn<Book>("BookTitle", "Title")
        .TableChanged<Book>())
    .MigrateAsync();
```

A step can also drop a column with `DropColumn`, drop a table with `DropTable`, or run raw SQL with `Sql` for a data fix. Within one run the order is fixed. Renames run first, then one reconcile per table, then drops and raw SQL. So a raw SQL data step reads the final shape of the table. To move data out of a column you are removing, keep the old column on the model while you copy it, then remove it in a later version.

The runner reconciles structure and runs the data steps you declare. For one-off column changes outside a migration, see the next section.

## Altering tables

```csharp
await db.Schema.AddColumnAsync<Book>("Subtitle");     // adds the Subtitle column from the entity
await db.Schema.RenameColumnAsync<Book>("BookTitle", "Title");
await db.Schema.DropColumnAsync<Book>("BookTitle");
await db.Schema.RenameTableAsync<Book>("RenamedBooks");
```

`AddColumn` takes a property name on the entity. The framework reads the type, nullability, and primary-key flags from your model and emits the right `ALTER TABLE ADD COLUMN` SQL.

A property selector overload is also available:

```csharp
await db.Schema.AddColumnAsync<Book>(b => b.Subtitle);
```

Pass `defaultValue` to emit a `DEFAULT` clause. You need this when you add a `NOT NULL` column to a table that already has rows. SQLite then uses the default to backfill existing rows.

```csharp
await db.Schema.AddColumnAsync<Book>(b => b.Pages, defaultValue: 0);
await db.Schema.AddColumnAsync<Book>(b => b.Genre, defaultValue: "Unknown");
```

SQLite does not let you use parameters inside DDL statements like `ALTER TABLE`. The framework writes `defaultValue` straight into the SQL text. Only numbers, `bool`, and `string` are accepted. Single quotes inside strings are doubled, so a value with quotes in it cannot escape from the string and run other SQL.

SQLite requires a column added with a foreign key to default to NULL. Adding a foreign key column with a non-null `defaultValue` or a default expression throws. Add it with a null default, or recreate the table with the new schema instead.

A second overload takes a translated SQL expression:

```csharp
await db.Schema.AddColumnAsync<Book>(b => b.Rating, () => 7 * 6);
```

Requires SQLite 3.31.0 or newer. SQLite also rejects non-constant defaults on `ADD COLUMN` when the table already has rows. Use them only on empty tables.

`RenameColumn`, `DropColumn`, and `RenameTable` take SQLite column or table names directly.

## Views

`db.Schema.CreateView<T>(...)` creates a SQL view from a LINQ expression. The view name comes from the `[Table("...")]` attribute on the entity, the body is the SQL produced by translating the lambda.

```csharp
await db.Schema.CreateViewAsync<BookSummary>(() =>
    from b in db.Table<Book>()
    where b.Price > 0
    select new BookSummary { Id = b.Id, Title = b.Title, Price = b.Price });

bool exists = await db.Schema.ViewExistsAsync<BookSummary>();
IReadOnlyList<string> views = db.Schema.ListViews();

await db.Schema.DropViewAsync<BookSummary>();
await db.Schema.DropViewAsync("vBookSummary");
```

The DDL uses `CREATE VIEW IF NOT EXISTS`, so calling `CreateView` twice is safe. Pair the view with `db.ReadOnlyTable<T>()` to query it.

SQLite does not allow placeholders inside view bodies, so any constants in the lambda are inlined as SQL literals when the view is created. Only simple types (numbers, strings, bool) work as inlined constants. For exotic types use raw SQL through `db.Execute`.

## Triggers

`db.Schema.CreateTrigger<T>(...)` creates a trigger on the table for `T`. The body and the optional `WHEN` predicate are raw SQL strings. Use `OLD` and `NEW` to refer to the row.

```csharp
await db.Schema.CreateTriggerAsync<Book>(
    name: "trg_book_history",
    timing: SQLiteTriggerTiming.After,
    @event: SQLiteTriggerEvent.Update,
    body: "INSERT INTO BookHistory(BookId, OldPrice, NewPrice) VALUES (NEW.Id, OLD.BookPrice, NEW.BookPrice)",
    when: "OLD.BookPrice <> NEW.BookPrice");

await db.Schema.DropTriggerAsync("trg_book_history");
```

`SQLiteTriggerTiming` is `Before`, `After`, or `InsteadOf`. `SQLiteTriggerEvent` is `Insert`, `Update`, or `Delete`. `InsteadOf` only works on views. SQLite runs every trigger once per row, so the body can use `NEW` and `OLD` to reference the changed row.

There is also a typed overload that builds the body from LINQ instead of a SQL string. Use the builder's `Old` and `New` rows, and add `Update`, `Insert`, or `Delete` statements. Columns and the `When` guard are checked at compile time.

```csharp
await db.Schema.CreateTriggerAsync<Book>("trg_book_history", SQLiteTriggerTiming.After, SQLiteTriggerEvent.Update, t => t
    .When(() => t.Old.Price != t.New.Price)
    .Insert(db.Table<BookHistory>(), s => s
        .Set(h => h.BookId, _ => t.New.Id)
        .Set(h => h.OldPrice, _ => t.Old.Price)
        .Set(h => h.NewPrice, _ => t.New.Price)));
```

`CreateTrigger` creates the trigger right away and is not tracked by the model. To make a trigger part of the model, declare it with `Trigger(...)` in `OnModelCreating` (see [Defining the model](#defining-the-model)). Model triggers are created by `CreateTable`, and a `TableChanged` migration creates them when missing and recreates them when their body changes. Inside `OnModelCreating` reach the target table through the database's own `Table<TTarget>()`.

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
