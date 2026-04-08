# JSON and JSONB

The `SQLite.Framework.JsonB` add-in package adds two things: type converters that store .NET objects as JSON inside a SQLite column, and method translators that let you call SQLite's built-in JSON functions from LINQ queries.

Install it alongside whichever core package you use:

```
dotnet add package SQLite.Framework
dotnet add package SQLite.Framework.JsonB
```

The `SQLite.Framework.JsonB` package does not pull in a provider automatically. You must install one of the four core packages (`SQLite.Framework`, `SQLite.Framework.Bundled`, `SQLite.Framework.Cipher`, or `SQLite.Framework.Base`) separately. This lets you swap providers without any conflict.

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
[JsonSerializable(typeof(List<string>))]
[JsonSerializable(typeof(List<Address>))]
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

// collections work the same way
db.StorageOptions.TypeConverters[typeof(List<string>)] =
    new SQLiteJsonConverter<List<string>>(AppJsonContext.Default.ListString);
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

## Collection methods

When you store a `List<T>` or `T[]` as JSON, `AddJson()` also registers translators for many standard LINQ, `List<T>`, and `Array` methods. These are translated to SQL using `json_each()` and other SQLite JSON functions. Everything runs on the database, not in memory.

### Supported LINQ methods (Enumerable)

**Scalar results (no predicate):**

| Method | What it does |
|---|---|
| `Any()` | True if the array is not empty |
| `Count()` | Number of elements |
| `First()` / `FirstOrDefault()` | First element |
| `Last()` / `LastOrDefault()` | Last element |
| `Single()` / `SingleOrDefault()` | The only element, or null if there is not exactly one |
| `ElementAt(i)` | Element at the given index |
| `Min()` / `Max()` | Smallest or largest element |
| `Sum()` / `Average()` | Sum or average of numeric elements |

**Scalar results (with predicate):**

| Method | What it does |
|---|---|
| `Any(x => ...)` | True if any element matches |
| `All(x => ...)` | True if every element matches |
| `Count(x => ...)` | Number of matching elements |
| `First(x => ...)` / `FirstOrDefault(x => ...)` | First matching element |
| `Last(x => ...)` / `LastOrDefault(x => ...)` | Last matching element |
| `Single(x => ...)` / `SingleOrDefault(x => ...)` | The only matching element |

**Aggregate with selector:**

| Method | What it does |
|---|---|
| `Min(x => x.Prop)` | Smallest value of a property |
| `Max(x => x.Prop)` | Largest value of a property |
| `Sum(x => x.Prop)` | Sum of a numeric property |
| `Average(x => x.Prop)` | Average of a numeric property |

**Collection results:**

| Method | What it does |
|---|---|
| `Where(x => ...)` | Filter elements |
| `Select(x => ...)` | Project each element |
| `SelectMany(x => x.Items)` | Flatten nested collections |
| `OrderBy(x => ...)` | Sort ascending |
| `OrderByDescending(x => ...)` | Sort descending |
| `ThenBy(x => ...)` | Secondary sort ascending (after OrderBy) |
| `ThenByDescending(x => ...)` | Secondary sort descending (after OrderBy) |
| `GroupBy(x => ...)` | Group elements by a key |
| `Distinct()` | Remove duplicates |
| `Reverse()` | Reverse the order |
| `Skip(n)` | Skip the first n elements |
| `Take(n)` | Take the first n elements |
| `Concat(other)` | Combine two collections |
| `Union(other)` | Combine two collections, removing duplicates |
| `Intersect(other)` | Keep only elements that appear in both |
| `Except(other)` | Remove elements that appear in the other |

### Supported List\<T\> methods

| Method | What it does |
|---|---|
| `Contains(item)` | True if the list contains the item |
| `IndexOf(item)` | Index of the first occurrence, or -1 |
| `LastIndexOf(item)` | Index of the last occurrence, or -1 |
| `GetRange(index, count)` | A sub-list starting at the given index |
| `Exists(x => ...)` | True if any element matches the predicate |
| `Find(x => ...)` | First element matching the predicate |
| `FindAll(x => ...)` | All elements matching the predicate |
| `FindIndex(x => ...)` | Index of the first match, or -1 |
| `FindLast(x => ...)` | Last element matching the predicate |
| `FindLastIndex(x => ...)` | Index of the last match, or -1 |
| `TrueForAll(x => ...)` | True if every element matches |

### Supported Array methods

| Method | What it does |
|---|---|
| `Array.IndexOf(arr, item)` | Index of the first occurrence, or -1 |
| `Array.LastIndexOf(arr, item)` | Index of the last occurrence, or -1 |
| `Array.Exists(arr, x => ...)` | True if any element matches |
| `Array.Find(arr, x => ...)` | First matching element |
| `Array.FindAll(arr, x => ...)` | All matching elements |
| `Array.FindIndex(arr, x => ...)` | Index of the first match, or -1 |
| `Array.FindLast(arr, x => ...)` | Last matching element |
| `Array.FindLastIndex(arr, x => ...)` | Index of the last match, or -1 |
| `Array.TrueForAll(arr, x => ...)` | True if every element matches |
| `Array.ConvertAll(arr, x => ...)` | Project each element |

### Examples

```csharp
// simple list queries
bool hasTag = db.Table<Product>()
    .Where(p => p.Tags.Contains("electronics"))
    .Any();

int tagCount = db.Table<Product>()
    .Select(p => p.Tags.Count())
    .First();

// predicate on simple types
List<Product> filtered = db.Table<Product>()
    .Where(p => p.Tags.Any(t => t.StartsWith("elec")))
    .ToList();

// predicate on complex types
List<Order> orders = db.Table<Order>()
    .Where(o => o.Items.Any(i => i.Price > 100 && i.Category == "Books"))
    .ToList();

// nested property access works too
bool hasLocal = db.Table<Company>()
    .Select(c => c.Offices.Any(o => o.Address.City == "Springfield"))
    .First();

// aggregate with selector
decimal maxPrice = db.Table<Order>()
    .Select(o => o.Items.Max(i => i.Price))
    .First();

// collection results
List<string> sorted = db.Table<Product>()
    .Select(p => p.Tags.OrderBy(t => t).Take(3))
    .First();

// chaining works, methods are combined into a single SQL subquery
string firstSorted = db.Table<Product>()
    .Select(p => p.Tags.OrderBy(t => t).First())
    .First();

// secondary sorting with ThenBy
string result = db.Table<Order>()
    .Select(o => o.Items
        .OrderBy(i => i.Category)
        .ThenByDescending(i => i.Price)
        .First().Name)
    .First();

// multiple chained operations become one query
int count = db.Table<Product>()
    .Select(p => p.Tags
        .Where(t => t.Length > 3)
        .OrderBy(t => t)
        .Count())
    .First();

// flatten nested collections with SelectMany
List<string> allTags = db.Table<Company>()
    .Select(c => c.Departments.SelectMany(d => d.Tags))
    .First();

// group by and count
int distinctGroups = db.Table<Product>()
    .Select(p => p.Tags.GroupBy(t => t).Count())
    .First();
```

### Property access on JSON columns

When you access a property on a JSON-stored object, `AddJson()` translates it to `json_extract`. This works in `Where`, `Select`, `OrderBy`, and anywhere else you use a property:

```csharp
// property access on a single JSON object
string city = db.Table<Contact>()
    .Select(c => c.HomeAddress.City)
    .First();
// SQL: SELECT json_extract(t0.HomeAddress, '$.City') ...

// property access on the result of a collection method
string street = db.Table<Order>()
    .Select(o => o.Items.First(i => i.Price > 50).Name)
    .First();
```

### Method chaining

When you chain two or more methods on a JSON collection, they are combined into a single SQL subquery instead of nesting multiple subqueries. For example, `.Where(...).OrderBy(...).Take(n)` produces one `SELECT ... FROM json_each(...) WHERE ... ORDER BY ... LIMIT n` query.

This also means that combinations like `.Where(...).Count()`, `.OrderBy(...).ThenBy(...).First()`, and `.GroupBy(...).Count()` all work and produce clean SQL.

### What is not supported

These patterns are not translated to SQL and will either fall back to client-side evaluation or throw an error:

- **Predicate overloads with start index or count.** `FindIndex(int startIndex, Predicate<T>)` and similar overloads that take a start index are not supported. Only the single-predicate overloads work.
- **`OrderBy` / `OrderByDescending` as the final result in a Select.** The C# return type is `IOrderedEnumerable<T>`, which cannot be deserialized back to `List<T>`. Chain another method after it instead, like `.First()` or `.Take(n)`.
- **`List<T>.Reverse()` in a Select.** The C# compiler picks the void instance method over the LINQ extension. Use `Enumerable.Reverse(list)` with the static call syntax instead.
- **`Zip`.** This is not supported.
- **`GroupBy` with result selector.** `GroupBy(x => x.Key, (key, group) => ...)` with a result selector is not supported yet. You can use `GroupBy(x => x.Key).Count()` and similar aggregations.
- **Predicate methods that return complex objects directly.** `Find(x => ...)` and `First(x => ...)` return the raw JSON value from the database. If you access a property on the result (like `.Street`), it works. If you try to return the whole object, you get the JSON string, not the deserialized object.

---

## Native AOT

`SQLiteJsonConverter<T>` and `SQLiteJsonbConverter<T>` both use `JsonTypeInfo<T>` for serialization, so they are fully compatible with Native AOT and trimming. The `AddJson()` method carries a `[DynamicDependency]` attribute that tells the trimmer to keep all public methods on `SQLiteJsonFunctions` and `Enumerable`, so those methods are never removed from the output.

You do not need to do anything extra beyond providing a source-generated `JsonSerializerContext` as shown above.
