# Attached Databases

`db.AttachDatabase` lets you open a second SQLite file on the same connection. After you attach it, you can read its tables with raw SQL.

## Attach a file

```csharp
await db.AttachDatabaseAsync("other.db", "aux");

List<Book> rows = await db.QueryAsync<Book>(
    "SELECT BookId AS Id, BookTitle AS Title, BookAuthorId AS AuthorId, BookPrice AS Price FROM aux.Books");
```

The first argument is the path to the SQLite file. The second is the name you give it on this connection. Use that name as the schema prefix in your SQL.

The schema name has to be a plain identifier. Letters, digits and underscores. The framework checks this and throws an `ArgumentException` if you pass something else.

## Typed queries against an attached file

Pass the schema name to `db.Table<T>(schema)` to read an attached table with the typed LINQ surface. The query emits the schema-qualified name `"aux"."Books"`, so you can join an attached table with a main table in one query.

```csharp
await db.AttachDatabaseAsync("other.db", "aux");

List<string> titles = (
    from a in db.Table<Author>()        // main
    join b in db.Table<Book>("aux")     // attached
        on a.Id equals b.AuthorId
    select b.Title
).ToList();
```

`db.Table<T>(schema)` returns a read-only table.

## Attach a whole context

If the attached file has its own `SQLiteDatabase`, attach the object instead of the path. The framework remembers the schema name, so tables read through that other database get the right prefix on their own.

```csharp
main.AttachDatabase(aux, "aux");        // aux is another SQLiteDatabase

List<string> titles = (
    from a in main.Table<Author>()      // runs on main
    join b in aux.Table<Book>()         // resolved as "aux"."Books"
        on a.Id equals b.AuthorId
    select b.Title
).ToList();
```

## Detach when you are done

```csharp
await db.DetachDatabaseAsync("aux");
```

After detach, queries against `aux.Books` will fail. A database attached as an object is also removed from the prefix lookup.

## Notes

- Writes go to the main database. Use raw SQL for writes to an attached file.
- The path can have an apostrophe in it. The framework escapes the path for you.
