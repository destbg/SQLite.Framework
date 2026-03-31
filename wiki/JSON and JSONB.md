# JSON and JSONB

The `SQLite.Framework.JsonB` add-in package adds two things: type converters that store .NET objects as JSON inside a SQLite column, and method translators that let you call SQLite's built-in JSON functions from LINQ queries.

Install it alongside whichever core package you use:

```
dotnet add package SQLite.Framework.JsonB
```

---

## Storing objects as JSON

When you have a .NET type that does not map to a simple SQLite column, you can serialize it to JSON and store the result in the database. The package provides two converters for this.

### SQLiteJsonConverter\<T\> - TEXT storage

`SQLiteJsonConverter<T>` serializes the value to a JSON string and stores it in a TEXT column. It is the simplest option and works with any SQLite tooling that can read text.

### SQLiteJsonbConverter\<T\> - JSONB storage

`SQLiteJsonbConverter<T>` stores the value in a BLOB column using SQLite's built-in `jsonb()` and `json()` functions. JSONB is more compact than text and lets SQLite parse it without scanning for quotes or escape sequences, which can make JSON function calls faster.

> **Platform compatibility.** JSONB support was added in [SQLite 3.45.0](https://sqlite.org/jsonb.html). As of Android 15 (API level 35) the bundled SQLite is 3.44.3, so JSONB requires Android 16 (API level 36) or later. iOS 16 and earlier ship with SQLite 3.39.5 or older, so no listed iOS version includes JSONB support out of the box.
>
> If you are targeting mobile devices and need JSONB, use `SQLite.Framework.Bundled` or `SQLite.Framework.Cipher` instead of the default `SQLite.Framework` package. Both ship their own SQLite binary and can be updated independently of the OS. With those packages you can use `SQLiteJsonbConverter<T>` on any supported OS version. If you must use the default package on older devices, use `SQLiteJsonConverter<T>` for plain TEXT storage instead.

Both converters take a `JsonTypeInfo<T>` from a source-generated `JsonSerializerContext`, which keeps them compatible with Native AOT and trimming.

### Setup

Create a `JsonSerializerContext` that includes all types you want to store as JSON:

```csharp
[JsonSerializable(typeof(Address))]
public partial class AppJsonContext : JsonSerializerContext;
```

Register the converter in `StorageOptions` before you use the table:

```csharp
// TEXT column
db.StorageOptions.TypeConverters[typeof(Address)] =
    new SQLiteJsonConverter<Address>(AppJsonContext.Default.Address);

// or for JSONB binary BLOB column
db.StorageOptions.TypeConverters[typeof(Address)] =
    new SQLiteJsonbConverter<Address>(AppJsonContext.Default.Address);
```

After that, any model with an `Address` property is handled automatically:

```csharp
public class Contact
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public Address HomeAddress { get; set; } = new();
}

public class Address
{
    public string Street { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
}
```

Reading and writing work the same as any other column:

```csharp
await db.Table<Contact>().AddAsync(new Contact
{
    Name = "Alice",
    HomeAddress = new Address { Street = "1 Main St", City = "Springfield" }
});

Contact alice = await db.Table<Contact>().FirstAsync(c => c.Name == "Alice");
Console.WriteLine(alice.HomeAddress.City); // Springfield
```

---

## JSON functions in queries

SQLite has a set of built-in JSON functions such as `json_extract`, `json_set`, and `json_valid`. The package exposes these through the `SQLiteJsonFunctions` static class and registers translators for them when you call `AddJson()`.

### Setup

```csharp
db.StorageOptions.AddJson();
```

`AddJson` registers translators for every method on `SQLiteJsonFunctions`. Call it once when setting up the database.

### Available functions

| Method | SQL produced |
|---|---|
| `Extract<T>(json, path)` | `json_extract(json, path)` |
| `Set(json, path, value)` | `json_set(json, path, value)` |
| `Insert(json, path, value)` | `json_insert(json, path, value)` |
| `Replace(json, path, value)` | `json_replace(json, path, value)` |
| `Remove(json, path)` | `json_remove(json, path)` |
| `Type(json, path)` | `json_type(json, path)` |
| `Valid(json)` | `json_valid(json)` |
| `Patch(json, patch)` | `json_patch(json, patch)` |
| `ArrayLength(json)` | `json_array_length(json)` |
| `ArrayLength(json, path)` | `json_array_length(json, path)` |
| `Minify(json)` | `json(json)` |
| `ToJsonb(json)` | `jsonb(json)` |
| `ExtractJsonb<T>(json, path)` | `jsonb_extract(json, path)` |

These methods throw `InvalidOperationException` at runtime. They are only valid inside a LINQ expression tree, where they are translated to SQL before execution.

### Filtering on a JSON field

```csharp
var errors = await db.Table<Log>()
    .Where(l => SQLiteJsonFunctions.Extract<string>(l.Data, "$.level") == "error")
    .ToListAsync();
```

### Projecting a JSON value

```csharp
var levels = await db.Table<Log>()
    .Select(l => SQLiteJsonFunctions.Extract<string>(l.Data, "$.level"))
    .ToListAsync();
```

### Checking whether a column contains valid JSON

```csharp
var valid = await db.Table<Log>()
    .Where(l => SQLiteJsonFunctions.Valid(l.Data))
    .ToListAsync();
```

---

## Native AOT

`SQLiteJsonConverter<T>` and `SQLiteJsonbConverter<T>` both use `JsonTypeInfo<T>` for serialization, so they are fully compatible with Native AOT and trimming. The `AddJson()` method carries a `[DynamicDependency]` attribute that tells the trimmer to keep all public methods on `SQLiteJsonFunctions`, so those marker methods are never removed from the output.

You do not need to do anything extra beyond providing a source-generated `JsonSerializerContext` as shown above.
