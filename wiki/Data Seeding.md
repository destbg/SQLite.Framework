# Data Seeding

Seeding puts rows into a fresh database, reference data like currencies and categories or demo content for first run. The framework has no dedicated seeding API because two existing pieces cover it, a guarded insert at startup and the migration chain. Pick by how the data should behave later.

## Insert when empty

The simplest seed. After the schema exists, check and fill:

```csharp
await db.Schema.CreateTableAsync<Category>();

if (!await db.Table<Category>().AnyAsync())
{
    await db.Table<Category>().AddRangeAsync(
    [
        new Category { Name = "Fiction" },
        new Category { Name = "Science" },
        new Category { Name = "History" },
    ]);
}
```

`AddRange` wraps the inserts in one transaction by default. This pattern never touches a database that already has data, so users keep their edits. That is also its limit, it cannot add the new category you introduce in version two. Use it for demo content and starting points the user owns afterwards.

## Idempotent seed with fixed keys

Reference data the app owns and may extend in later releases wants an upsert instead of a guard. Give each row a fixed key and run the seed on every startup:

```csharp
await db.Table<Country>().AddOrUpdateRangeAsync(
[
    new Country { Code = "BG", Name = "Bulgaria" },
    new Country { Code = "DE", Name = "Germany" },
]);
```

New rows appear, existing rows are refreshed to the shipped values, user data in other tables is untouched. `AddOrUpdate` replaces the whole row on conflict. When user-editable columns must survive, use `Upsert` with a `DoUpdate` that lists only the columns you own, see [CRUD Operations](CRUD%20Operations).

## Seeding inside a migration

When seed data is part of a schema version, put it in the [migration](Migrations) chain with the typed `Insert` step. It runs exactly once per database, inside the migration's transaction and after the tables of that run are reconciled.

```csharp
await db.Schema.Migrations()
    .Version(1, m => m
        .CreateTable<Category>()
        .Insert(
            new Category { Name = "Fiction" },
            new Category { Name = "Science" }))
    .Version(2, m => m
        .Insert(new Category { Name = "History" }))
    .MigrateAsync();
```

The rows go through the same write pipeline as `Add`, so storage modes, converters, write hooks and auto-increment key write-back all apply. A failed insert rolls the whole run back, like every other step.

Seed rows follow the same rule as every migration change. Never add them to a version that has shipped, because databases that passed that version will not run it again. Declare the next version instead, like version 2 above. See [Migrations](Migrations).

For a data fix that is not an insert, an UPDATE over old rows for example, use the raw `Sql` step, see [Migrations](Migrations).

## Seed data from a file

Larger datasets read better as an asset than as code. Ship a JSON file, parse it, insert with `AddRange`:

```csharp
if (!await db.Table<Book>().AnyAsync())
{
    await using Stream stream = File.OpenRead(seedPath);
    List<Book> books = await JsonSerializer.DeserializeAsync<List<Book>>(stream) ?? [];
    await db.Table<Book>().AddRangeAsync(books);
}
```

The Avalonia sample in the repository ships `SeedData.json` and a small seed service built exactly this way, see [Samples](Samples).

## What seeding is not

A column `DEFAULT` is not seeding, it fills a column on rows inserted later, see [Schema](Schema). And test fixtures are not seeding either, tests build their own data per test, see [Testing](Testing).
