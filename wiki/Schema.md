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
    .Index(b => b.Title)
    .Index(b => b.AuthorId, unique: true)
    .Index(b => b.CategoryId, filter: b => !b.Deleted)
    .CreateTable();
```

`Computed(target, sql, stored)` adds a generated column. The default is virtual (computed on every read); pass `stored: true` to store the value on disk.

`Check(predicate, name)` adds a table-level CHECK constraint. The predicate is translated to SQL the same way `Where` clauses are.

`Index(column, name, unique, filter)` adds an index. When `filter` is set, the result is a partial index (a `WHERE` clause on the index itself).

Constants in computed, CHECK, and partial-index expressions are inlined as SQL literals because CREATE TABLE / CREATE INDEX cannot bind parameters. Only simple types (numbers, strings, bool) are supported as constants. For exotic types, use raw SQL through `db.Execute`.

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

`RenameColumn`, `DropColumn`, and `RenameTable` take SQLite column or table names directly.

## Async

Every method has an async wrapper that runs on a background thread:

```csharp
await db.Schema.CreateTableAsync<Book>();
await db.Schema.DropTableAsync<Book>();
await db.Schema.CreateIndexAsync<Book>(b => b.Title);
bool exists = await db.Schema.TableExistsAsync<Book>();
IReadOnlyList<SchemaColumnInfo> cols = await db.Schema.ListColumnsAsync<Book>();
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
