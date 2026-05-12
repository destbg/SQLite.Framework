# Defining Models

A model is a plain C# class. Each public property maps to a column. The attributes come from `System.ComponentModel.DataAnnotations`, `System.ComponentModel.DataAnnotations.Schema`, and `SQLite.Framework.Attributes`.

## Primary Key

Use `[Key]` to mark the primary key. Add `[AutoIncrement]` to let SQLite assign the value automatically.

```csharp
using System.ComponentModel.DataAnnotations;
using SQLite.Framework.Attributes;

public class Book
{
    [Key]
    [AutoIncrement]
    public int Id { get; set; }

    public required string Title { get; set; }
}
```

Without `[AutoIncrement]`, you are responsible for setting the key to an unique value before inserting.

## Custom Table Name

By default the table name matches the class name. Use `[Table]` to change it.

```csharp
using System.ComponentModel.DataAnnotations.Schema;

[Table("Books")]
public class Book
{
    [Key]
    [AutoIncrement]
    public int Id { get; set; }
}
```

## Custom Column Name

By default the column name matches the property name. Use `[Column]` to change it.

```csharp
[Column("BookTitle")]
public required string Title { get; set; }
```

## NOT NULL Columns

Use `[Required]` to mark a column as NOT NULL.

```csharp
[Required]
public required string Title { get; set; }
```

Nullable reference types (`string?`, `int?`) map to nullable columns automatically.

## Excluding Properties

Use `[NotMapped]` to keep a property out of the database entirely.

```csharp
using System.ComponentModel.DataAnnotations.Schema;

[NotMapped]
public string DisplayLabel => $"{Title} (${Price})";
```

## Indexes

Use `[Indexed]` to create an index on a column when `CreateTable` runs.

```csharp
using SQLite.Framework.Attributes;

[Indexed]
public int AuthorId { get; set; }
```

Use `IsUnique = true` for a unique index:

```csharp
[Indexed(IsUnique = true)]
public string Isbn { get; set; }
```

Give the index a specific name:

```csharp
[Indexed(Name = "IX_Book_AuthorId")]
public int AuthorId { get; set; }
```

To create a composite index across multiple columns, use the same name and set the `Order`:

```csharp
[Indexed("IX_Book_AuthorGenre", 0)]
public int AuthorId { get; set; }

[Indexed("IX_Book_AuthorGenre", 1)]
public string? Genre { get; set; }
```

## Foreign Keys

Mark the foreign key column with `[ReferencesTable(typeof(Parent))]`. The framework emits an inline `REFERENCES "Parent"("Id")` clause and infers the parent's primary key.

```csharp
public class Book
{
    [Key]
    public int Id { get; set; }
    public required string Title { get; set; }

    [ReferencesTable(typeof(Author))]
    public int AuthorId { get; set; }
}
```

Set `OnDelete`, `OnUpdate`, or `Deferred` for richer behavior. `SetNull` requires the column to be nullable. The framework checks this at model load.

```csharp
[ReferencesTable(typeof(Author), OnDelete = SQLiteForeignKeyAction.Cascade)]
public int AuthorId { get; set; }

[ReferencesTable(typeof(Author), OnDelete = SQLiteForeignKeyAction.SetNull)]
public int? AuthorId { get; set; }
```

Pass a column name to target a non-primary-key column.

```csharp
[ReferencesTable(typeof(Country), nameof(Country.Code))]
public string CountryCode { get; set; }
```

The framework also reads `[System.ComponentModel.DataAnnotations.Schema.ForeignKey("Author")]`. It takes `Name` as the target class name and infers the primary key with `NoAction` defaults. Prefer `[ReferencesTable]` when you need an action other than `NoAction`, deferred enforcement, or refactor-safe `typeof` targeting. The two attributes cannot be combined on the same property.

For composite keys use the fluent builder.

```csharp
db.Schema.Table<OrderLine>()
    .ForeignKey<Order>(
        l => new { l.OrderId, l.OrderVersion },
        o => new { o.Id, o.Version },
        onDelete: SQLiteForeignKeyAction.Cascade)
    .CreateTable();
```

The single-column overload also lives on the builder for runtime decisions.

```csharp
db.Schema.Table<Book>()
    .ForeignKey<Author>(b => b.AuthorId, onDelete: SQLiteForeignKeyAction.Cascade)
    .CreateTable();
```

Enforcement is on by default. The framework runs `PRAGMA foreign_keys = ON` on every connection open. Pass `UseForeignKeys(false)` to the builder to opt out, or flip it at runtime with `db.Pragmas.ForeignKeys = false`.

## WITHOUT ROWID

Use `[WithoutRowId]` on the class to create a WITHOUT ROWID table. This can improve performance for tables where every lookup goes through the primary key. The primary key must not be `[AutoIncrement]` when using this.

```csharp
using SQLite.Framework.Attributes;

[WithoutRowId]
public class BookTag
{
    [Key]
    public required string Tag { get; set; }
}
```

See the [SQLite docs](https://sqlite.org/withoutrowid.html) for details on when this helps.

## Full Example

```csharp
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Attributes;

[Table("Books")]
public class Book
{
    [Key]
    [AutoIncrement]
    [Column("BookId")]
    public int Id { get; set; }

    [Column("BookTitle")]
    [Required]
    public required string Title { get; set; }

    [Column("BookAuthorId")]
    [Required]
    [Indexed(Name = "IX_Book_AuthorId")]
    public required int AuthorId { get; set; }

    [Column("BookPrice")]
    [Required]
    public required decimal Price { get; set; }

    [Column("BookGenre")]
    public string? Genre { get; set; }

    [Column("BookPublishedAt")]
    public DateTime PublishedAt { get; set; }

    [Column("BookInStock")]
    public bool InStock { get; set; }

    [NotMapped]
    public string Label => $"{Title} by author {AuthorId}";
}
```
