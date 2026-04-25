# Attached Databases

`db.AttachDatabase` lets you open a second SQLite file on the same connection. After you attach it, you can read its tables with raw SQL.

## Attach a file

```csharp
db.AttachDatabase("other.db", "aux");

List<Book> rows = db.Query<Book>(
    "SELECT BookId AS Id, BookTitle AS Title, BookAuthorId AS AuthorId, BookPrice AS Price FROM aux.Books");
```

The first argument is the path to the SQLite file. The second is the name you give it on this connection. Use that name as the schema prefix in your SQL.

The schema name has to be a plain identifier. Letters, digits, and underscores. The framework checks this and throws an `ArgumentException` if you pass something else.

## Detach when you are done

```csharp
db.DetachDatabase("aux");
```

After detach, queries against `aux.Books` will fail.

## Notes

- Use the typed `db.Table<T>()` API only on the main database. Attached tables are read with raw SQL through `db.Query<T>(...)`, `db.QueryFirst<T>(...)`, and friends.
- The path can have an apostrophe in it. The framework escapes the path for you.
- `AttachDatabaseAsync` and `DetachDatabaseAsync` run the same calls on a background thread.
