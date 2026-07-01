# Custom Converters

`SQLiteOptionsBuilder` lets you register custom type converters and custom method translators before calling `Build()`. Type converters control how a .NET type is stored in and read from the database. Method translators let you map a .NET method call to a SQL function when building queries.

---

## Type converters

A type converter implements `ISQLiteTypeConverter` and tells the library which SQLite column type to use and how to convert values in each direction.

```csharp
public interface ISQLiteTypeConverter
{
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

Register the converter on the builder and hand the built options to the database:

```csharp
SQLiteOptions options = new SQLiteOptionsBuilder("shop.db")
    .AddTypeConverter<Money>(new MoneyConverter())
    .Build();

using var db = new SQLiteDatabase(options);
```

After that, any model with a `Money` property is handled automatically:

```csharp
public class Product
{
    [Key]
    [AutoIncrement]
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public Money Price { get; set; } = new(0, "USD");
}
```

CRUD and queries that project or filter on a `Money` column work without any extra setup.
The converter is called transparently when binding parameters and when reading rows back.

> Filtering on a custom-type column with a LINQ `Where` requires a `MethodTranslator` for the comparison logic or using `FromSql` to write the SQL by hand.

---

## Method translators

A method translator maps a .NET method to a SQL expression.
It covers SQL functions that have no .NET match, such as the JSON functions built into SQLite.

Add a translator on the builder by calling `AddMethodTranslator`. Pass the `MethodInfo` as the key and a `SQLiteMemberTranslator` delegate as the value:

```csharp
public delegate Expression SQLiteMemberTranslator(SQLiteCallerContext callerContext);
```

The delegate gets a `SQLiteCallerContext` for the call. It returns an `Expression` (most often a `SQLiteExpression`) that takes the place of the original method call in the query.

`SQLiteCallerContext` gives you:

- `Node`: the original `MethodCallExpression` being translated.
- `Visit(Expression)`: translate a child expression. Call this for each argument so you can use its translated form.
- `Counters.NextIdentifier()` and `Counters.NextParamName()`: give you a new identifier or a new `@p`-prefixed parameter name that is unique inside the query.

Build the result with one of the `SQLiteExpression` factory methods:

- `SQLiteExpression.Leaf(type, id, sql, parameters)`: a plain SQL string with no child expressions.
- `SQLiteExpression.Wrap(type, id, before, child, after, parameters)`: one child slot.
- `SQLiteExpression.Binary(type, id, before, a, mid, b, after, parameters)`: two child slots.
- `SQLiteExpression.Trinary(type, id, before, a, mid1, b, mid2, c, after, parameters)`: three child slots.
- `SQLiteExpression.Variadic(type, id, before, children, sep, after, parameters)`: a list of children joined by `sep`.

The last `parameters` argument is the full list of parameters for the new expression. Each time you visit a child, take its `Parameters` and add them to your list, so the database can bind them when the query runs.

### Example: SQLite JSON functions

Add stub methods that throw at run time. The query engine replaces them with SQL. If you call them by mistake from normal code, the error makes it clear what went wrong:

```csharp
public static class JsonFunctions
{
    public static string JsonExtract(string json, string path)
    {
        throw new InvalidOperationException("This method can only be used inside a LINQ query.");
    }

    public static string JsonObject(string key, object? value)
    {
        throw new InvalidOperationException("This method can only be used inside a LINQ query.");
    }
}
```

Add a translator for each one:

```csharp
using SQLite.Framework.Internals.Models; // SQLiteExpression lives here

SQLiteOptions options = new SQLiteOptionsBuilder("data.db")
    .AddMethodTranslator(
        typeof(JsonFunctions).GetMethod(nameof(JsonFunctions.JsonExtract))!,
        ctx =>
        {
            MethodCallExpression call = (MethodCallExpression)ctx.Node;
            SQLiteExpression json = (SQLiteExpression)ctx.Visit(call.Arguments[0]);
            SQLiteExpression path = (SQLiteExpression)ctx.Visit(call.Arguments[1]);
            return SQLiteExpression.Binary(
                call.Type, ctx.Counters.NextIdentifier(),
                "json_extract(", json, ", ", path, ")",
                Combine(json.Parameters, path.Parameters));
        })
    .AddMethodTranslator(
        typeof(JsonFunctions).GetMethod(nameof(JsonFunctions.JsonObject))!,
        ctx =>
        {
            MethodCallExpression call = (MethodCallExpression)ctx.Node;
            SQLiteExpression key = (SQLiteExpression)ctx.Visit(call.Arguments[0]);
            SQLiteExpression value = (SQLiteExpression)ctx.Visit(call.Arguments[1]);
            return SQLiteExpression.Binary(
                call.Type, ctx.Counters.NextIdentifier(),
                "json_object(", key, ", ", value, ")",
                Combine(key.Parameters, value.Parameters));
        })
    .Build();

using var db = new SQLiteDatabase(options);

static SQLiteParameter[]? Combine(SQLiteParameter[]? a, SQLiteParameter[]? b)
{
    if (a == null) return b;
    if (b == null) return a;
    return [.. a, .. b];
}
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
WHERE json_extract(l0."Data", '$.level') = 'error'
```

### Example: instance method on a custom type

If your type has methods that should translate to SQL functions, add them the same way. For instance methods, `MethodCallExpression.Object` holds the object the method is called on. Visit it the same way you visit each argument:

```csharp
public class Point
{
    public double X { get; set; }
    public double Y { get; set; }

    public double DistanceTo(Point other)
    {
        throw new InvalidOperationException("This method can only be used inside a LINQ query.");
    }
}
```

```csharp
options.MemberTranslators[typeof(Point).GetMethod(nameof(Point.DistanceTo))!] = ctx =>
{
    MethodCallExpression call = (MethodCallExpression)ctx.Node;
    SQLiteExpression self = (SQLiteExpression)ctx.Visit(call.Object!);
    SQLiteExpression other = (SQLiteExpression)ctx.Visit(call.Arguments[0]);
    string s = self.ToString();
    string o = other.ToString();
    string sql =
        $"sqrt(" +
        $"((json_extract({s}, '$.X') - json_extract({o}, '$.X')) * (json_extract({s}, '$.X') - json_extract({o}, '$.X'))) + " +
        $"((json_extract({s}, '$.Y') - json_extract({o}, '$.Y')) * (json_extract({s}, '$.Y') - json_extract({o}, '$.Y')))" +
        $")";
    return SQLiteExpression.Leaf(call.Type, ctx.Counters.NextIdentifier(), sql,
        Combine(self.Parameters, other.Parameters));
};
```

> When you only need the SQL text of a child, call `expr.ToString()`. You get back the SQL text, but the parameters stay on the child. You still need to add those parameters to your own result list.
