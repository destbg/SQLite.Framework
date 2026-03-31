# Custom Converters

`SQLiteStorageOptions` lets you register custom type converters and custom method translators.
Type converters control how a .NET type is stored in and read from the database.
Method translators let you map a .NET method call to a SQL function when building queries.

---

## Type converters

A type converter implements `ISQLiteTypeConverter` and tells the library three things: the .NET type it handles, which SQLite column type to use, and how to convert values in each direction.

```csharp
public interface ISQLiteTypeConverter
{
    Type Type { get; }
    SQLiteColumnType ColumnType { get; }
    object? ToDatabase(object? value);
    object? FromDatabase(object? value);
}
```

### Example: storing a custom value object as TEXT

Say you have a `Money` type that wraps a decimal amount and a currency code:

```csharp
public record Money(decimal Amount, string Currency);
```

You can store it as a single TEXT column in the format `"9.99 USD"`:

```csharp
public class MoneyConverter : ISQLiteTypeConverter
{
    public Type Type => typeof(Money);
    public SQLiteColumnType ColumnType => SQLiteColumnType.Text;

    public object? ToDatabase(object? value)
    {
        if (value is not Money money) return null;
        return $"{money.Amount} {money.Currency}";
    }

    public object? FromDatabase(object? value)
    {
        if (value is not string s) return null;
        int space = s.LastIndexOf(' ');
        return new Money(decimal.Parse(s[..space]), s[(space + 1)..]);
    }
}
```

Register the converter when creating the database:

```csharp
var options = new SQLiteStorageOptions();
options.TypeConverters[typeof(Money)] = new MoneyConverter();

using var db = new SQLiteDatabase("shop.db", options);
```

After that, any model with a `Money` property is handled automatically:

```csharp
public class Product
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public Money Price { get; set; } = new(0, "USD");
}
```

CRUD and queries that project or filter on a `Money` column work without any extra setup.
The converter is called transparently when binding parameters and when reading rows back.

> Filtering on a custom-type column with a LINQ `Where` requires a `MethodTranslator` for the comparison logic, or using `FromSql` to write the SQL by hand.

---

## Method translators

A method translator maps a .NET method to a SQL expression.
This is useful for SQL functions that have no .NET equivalent, such as the JSON functions built into SQLite.

Register translators in `MethodTranslators` using the `MethodInfo` as the key
and a `SQLiteMethodTranslator` delegate as the value:

```csharp
public delegate string SQLiteMethodTranslator(string? instanceSql, string[] argumentsSql);
```

- `instanceSql` is the SQL for the object the method is called on. It is `null` for static methods.
- `argumentsSql` is an array containing the SQL for each argument.

The delegate returns a SQL string that replaces the method call in the query.

### Example: SQLite JSON functions

Define marker methods that throw at runtime so you get a clear error if they are called outside a query:

```csharp
public static class JsonFunctions
{
    public static string JsonExtract(string json, string path)
        => throw new InvalidOperationException("This method can only be used inside a LINQ query.");

    public static string JsonObject(string key, object? value)
        => throw new InvalidOperationException("This method can only be used inside a LINQ query.");
}
```

Register translators for each one:

```csharp
var options = new SQLiteStorageOptions();

options.MethodTranslators[typeof(JsonFunctions).GetMethod(nameof(JsonFunctions.JsonExtract))!] =
    (_, args) => $"json_extract({args[0]}, {args[1]})";

options.MethodTranslators[typeof(JsonFunctions).GetMethod(nameof(JsonFunctions.JsonObject))!] =
    (_, args) => $"json_object({args[0]}, {args[1]})";

using var db = new SQLiteDatabase("data.db", options);
```

Now you can use those methods inside LINQ queries and they will be translated to SQL:

```csharp
var results = await db.Table<Log>()
    .Where(l => JsonFunctions.JsonExtract(l.Data, "$.level") == "error")
    .ToListAsync();
```

This produces SQL like:

```sql
SELECT * FROM "Log" AS l0
WHERE json_extract(l0.Data, '$.level') = 'error'
```

### Example: instance method on a custom type

If your type has methods that should translate to SQL functions, register them the same way.
`instanceSql` will contain the SQL for the object the method is called on:

```csharp
public class Point
{
    public double X { get; set; }
    public double Y { get; set; }

    public double DistanceTo(Point other)
        => throw new InvalidOperationException("This method can only be used inside a LINQ query.");
}
```

```csharp
options.MethodTranslators[typeof(Point).GetMethod(nameof(Point.DistanceTo))!] =
    (instance, args) => $"sqrt(((json_extract({instance}, '$.X') - json_extract({args[0]}, '$.X')) * (json_extract({instance}, '$.X') - json_extract({args[0]}, '$.X'))) + ((json_extract({instance}, '$.Y') - json_extract({args[0]}, '$.Y')) * (json_extract({instance}, '$.Y') - json_extract({args[0]}, '$.Y'))))";
```

> The `instance` parameter gives you the SQL column reference for the object. Use it to build the full SQL expression.
