# Existing Databases

This page is about pointing the framework at a database you did not create, a file from an older app, another library or another team. The goal is to describe what is already there, not to change it.

## Map the names you find

Table and column names rarely match C# conventions. Map them with attributes or in `OnModelCreating`, whichever you prefer.

```csharp
[Table("tbl_books")]
public class Book
{
    [Key]
    [Column("book_id")]
    public int Id { get; set; }

    [Column("book_title")]
    public string Title { get; set; } = "";
}
```

The builder equivalents are `ToTable`, `HasColumnName`, `HasKey` and friends, see [Defining Models](Defining%20Models).

Columns that exist in the file but not on your entity are simply ignored by queries, the framework only selects mapped columns. To keep such a column across a migration rebuild without giving it a CLR property, declare it as a shadow column with `Column(...)` in `OnModelCreating`, see [Schema](Schema).

## Match the value formats

The framework's defaults assume it wrote the data itself. A foreign database usually needs some [Storage Options](Storage%20Options) adjusted to match what the original writer did:

* Dates stored as ISO text read back fine, but to write the same shape use `UseDateTimeStorage(DateTimeStorageMode.TextFormatted, "...")`. Databases from sqlite-net-pcl may need `TextTicks`, see [Migrating from sqlite-net-pcl](Migrating%20from%20sqlite-net-pcl). Dates stored as unix seconds have no matching mode, map them as `long` and convert in code. The [Dates and Times](Dates%20and%20Times) page covers all of this.
* Enums stored as names need `UseEnumStorage(EnumStorageMode.Text)`.
* Exact decimals stored as strings need `UseDecimalStorage(DecimalStorageMode.Text)`.
* For any representation the storage options cannot express, write a [Custom Converter](Custom%20Converters), which gives you full control over the bind and read of one CLR type.

One case to know about, Guids. The framework stores and compares Guids as lowercase text. A Guid another tool wrote in uppercase reads back as the correct .NET `Guid`, but an equality filter against it does not match, because the comparison happens on the stored text. Normalize the casing in the data once or filter on a lowercased copy.

## Validate before you trust it

`ValidateModel` compares your mapping against the live file and returns every difference as a readable sentence. Run it at startup in development or in a test, so you find drift before your users do.

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

It checks columns, types, keys, nullability, indexes, foreign keys and declared triggers. Extra columns in the file are reported too, which is exactly what you want to hear about when reverse-engineering a schema.

## Read-only access

When the file belongs to another application, make the intent explicit:

* `db.ReadOnlyTable<T>()` exposes the full query surface with no write methods.
* Opening the file with read-only flags stops writes at the SQLite level, `UseOpenFlags(SQLiteOpenFlags.ReadOnly)`.
* Attaching the file to your own database with `AttachDatabase` gives read-only typed queries across both, see [Attached Databases](Attached%20Databases).

## Be careful with schema APIs

`CreateTable` is safe, it does nothing when the table exists. The [migration](Migrations) reconcile is not a tool for databases you do not own. `TableChanged` makes the table match your model, which includes dropping columns your model does not declare. On a foreign database, prefer reading what is there. If you must evolve it, declare every existing column, as a property or a shadow column, before reconciling.
