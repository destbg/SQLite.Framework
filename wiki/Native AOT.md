# Native AOT

Native AOT compiles your application into a standalone native binary ahead of time, rather than running through the .NET runtime at startup. The result is faster startup, lower memory use, and no JIT overhead. SQLite.Framework supports Native AOT publishing with a small amount of setup.

## Project setup

Enable AOT in your `.csproj` file:

```xml
<PropertyGroup>
    <PublishAot>true</PublishAot>
    <IsTrimmable>true</IsTrimmable>
</PropertyGroup>
```

## Preserve built-in types

The AOT trimmer removes code it thinks is unused. SQLite.Framework uses reflection to read and write columns for built-in types like `int`, `string`, `DateTime`, and so on. You need to tell the trimmer to keep all of that code.

Add a `TrimmerRootDescriptor.xml` file to your project and reference it from the `.csproj`:

```xml
<ItemGroup>
    <TrimmerRootDescriptor Include="TrimmerRootDescriptor.xml" />
</ItemGroup>
```

The descriptor should preserve every type that can appear as a column value:

```xml
<linker>
  <assembly fullname="System.Private.CoreLib">
    <type fullname="System.Boolean" preserve="all" />
    <type fullname="System.Byte" preserve="all" />
    <type fullname="System.SByte" preserve="all" />
    <type fullname="System.Char" preserve="all" />
    <type fullname="System.Int16" preserve="all" />
    <type fullname="System.UInt16" preserve="all" />
    <type fullname="System.Int32" preserve="all" />
    <type fullname="System.UInt32" preserve="all" />
    <type fullname="System.Int64" preserve="all" />
    <type fullname="System.UInt64" preserve="all" />
    <type fullname="System.Single" preserve="all" />
    <type fullname="System.Double" preserve="all" />
    <type fullname="System.Decimal" preserve="all" />
    <type fullname="System.String" preserve="all" />
    <type fullname="System.DateTime" preserve="all" />
    <type fullname="System.DateTimeOffset" preserve="all" />
    <type fullname="System.DateOnly" preserve="all" />
    <type fullname="System.TimeOnly" preserve="all" />
    <type fullname="System.TimeSpan" preserve="all" />
    <type fullname="System.Guid" preserve="all" />
  </assembly>
</linker>
```

This covers all the types listed in [Data Types](Data%20Types.md). If you only use a subset of them you can trim this list down, but keeping all of them is safe and simple.

## LINQ queries with anonymous types

When you write a query that projects into an anonymous type or uses object initialisers inside a `select`, the C# compiler generates calls to `Expression.New` and `Expression.Bind`. These methods are annotated with `[RequiresUnreferencedCode]`, which will produce AOT warnings at publish time.

For example:

```csharp
var result = db.Table<Product>()
    .Select(p => new { p.Id, p.Name })
    .ToList();
```

Because the types involved are directly referenced in your code, the trimmer will not actually remove them, so the warning is safe to suppress. Add `[UnconditionalSuppressMessage]` to any method that contains queries like this:

```csharp
using System.Diagnostics.CodeAnalysis;

[UnconditionalSuppressMessage("AOT", "IL2026",
    Justification = "All types used in expression trees are referenced directly and will not be trimmed.")]
private static void MyQueryMethod(SQLiteDatabase db)
{
    var result = db.Table<Product>()
        .Select(p => new { p.Id, p.Name })
        .ToList();
}
```

You only need this attribute on methods that project into anonymous types or use object initialisers inside a `select`. Queries that return entity types directly do not need it.
