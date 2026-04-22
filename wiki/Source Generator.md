# Source Generator

`SQLite.Framework.SourceGenerator` is an optional package that produces materializers for your entities and `Select` projections at build time. A materializer is the small piece of code that reads column values from a SQLite row and builds a .NET object out of them.

Without the source generator, SQLite.Framework walks the expression tree of every query at runtime and uses reflection to create the result objects. That works fine on normal .NET but it has two costs:

- **Startup and per-query cost**: every row goes through reflected constructor, property, and method calls.
- **AOT compatibility**: the expression tree methods the C# compiler generates for a `Select` (like `Expression.New` and `Expression.Bind`) are annotated with `[RequiresUnreferencedCode]`, which produces trimmer warnings under `PublishAot`. The trimmer can also strip types that are only reached through reflection.

The source generator solves both. It reads your code at build time and writes plain C# that creates the objects directly. Every public type or method the generator can see is referenced by name, so the trimmer keeps it and no reflection is needed for those.

Reflection is still used in two narrow cases: when a `Select` or entity target is a `private` or `internal` type that the generated code cannot name, and when a `Select` body calls a private method. In those cases the generator falls back to `MethodInfo.Invoke` / `Activator.CreateInstance` on types and members that are captured at query-build time. The word "reflection-free" would be too strong, but reflection is avoided for everything the generator can see. If you want zero reflection on the hot path, keep the types and methods that appear in your `Select` projections `public` or `internal` with `InternalsVisibleTo`.

## When to use it

Use the source generator when any of these apply:

- You ship with Native AOT (`PublishAot`). This is the main reason it exists.
- You want faster cold-start queries (for example in a mobile app or a short-lived CLI).
- You want to avoid reflection for every public type and method the generator can see, and keep the trimmer happy on every shape the generator covers.

If you do not need any of these, you can skip it. The runtime path still works.

## Installation

Install the package next to `SQLite.Framework`:

```bash
dotnet add package SQLite.Framework.SourceGenerator
```

It is a build-time only package. It does not add a runtime dependency to your app.

## Usage

Call `UseGeneratedMaterializers` on your `SQLiteOptionsBuilder`:

```csharp
using SQLite.Framework;
using SQLite.Framework.Generated;

SQLiteOptions options = new SQLiteOptionsBuilder("app.db")
    .UseGeneratedMaterializers()
    .Build();

using SQLiteDatabase db = new(options);
```

`UseGeneratedMaterializers` is an extension method written by the generator itself. It lives in the `SQLite.Framework.Generated` namespace, so add the `using` line shown above. It fills in the `EntityMaterializers` and `SelectMaterializers` dictionaries on the builder. After that, every query uses the generated code and falls back to the runtime path only for shapes the generator does not cover yet.

That is all the setup that is needed. Write LINQ queries the same way you do without the generator:

```csharp
var titles = await db.Table<Book>()
    .Where(b => b.Price < 30)
    .Select(b => new { b.Id, b.Title })
    .ToListAsync();
```

The anonymous type `{ Id, Title }` gets a materializer at build time.

## Fail fast when reflection would be used

If you want a hard guarantee that the source generator covers every query in production, call `DisableReflectionFallback` on the builder:

```csharp
SQLiteOptions options = new SQLiteOptionsBuilder("app.db")
    .UseGeneratedMaterializers()
    .DisableReflectionFallback()
    .Build();
```

With this set, any query that would otherwise use the runtime reflection path throws an `InvalidOperationException` at the moment the query runs. This covers both entity materialization (for example when `db.Table<T>()` hits a type the generator did not cover) and `Select` projections (shapes the generator skipped).

Run your test suite with this flag on to catch unsupported shapes before you ship. Leave it off during local experimentation when you want the runtime fallback to just work.

## One generator output per project

The generator runs in every project that references the package and produces one `SQLiteFrameworkGeneratedMaterializers` class per project. The class and its `UseGeneratedMaterializers` method are `internal`, so they are only visible inside the project that built them.

This means:

- If your solution has several projects that build LINQ queries (for example a Web API project, a background worker, and a shared data library that only exposes `IQueryable` helpers), **each project that calls `UseGeneratedMaterializers` needs its own reference to `SQLite.Framework.SourceGenerator`**. The generated class in project A cannot be called from project B.
- The generator only sees entities and `Select` projections that appear in the project it is building. A `Select` written in a different project will use the runtime path unless that other project also has the generator installed and also calls `UseGeneratedMaterializers` on its own builder.
- It is fine to call `UseGeneratedMaterializers` more than once on the same builder (for example once per library that contributes queries). Later calls replace entries in the dictionaries for the same signature, so the last registration wins.

If all your queries live in one project (the common case for small apps), install the package there and call `UseGeneratedMaterializers` once at startup. That is the whole setup.

## What the generator covers

The generator produces two kinds of materializers.

**Entity materializers** map a row to a table entity (the classes you register with `db.Table<T>()`). This covers simple properties, nullable types, custom converters, and the built-in types listed in [Data Types](Data%20Types.md).

**Select materializers** cover the body of a `Select` lambda. This includes:

- Anonymous types: `Select(b => new { b.Id, b.Title })`
- Object initialisers: `Select(b => new BookView { Id = b.Id, Title = b.Title })`
- Method calls on rows, including your own methods: `Select(b => FormatTitle(b))`
- Captured locals from the surrounding method: `Select(b => new { b.Id, Prefix = prefix + b.Title })`
- Joins and group joins written in query syntax.

A tiny number of shapes still fall back to the runtime path, such as projections into private nested types. If you want the build to fail fast when that happens, turn on `DisableReflectionFallback` on the options builder so the first query with an unsupported shape throws at runtime (see "Fail fast when reflection would be used" below).

## Combining with AOT

If you publish with `PublishAot=true`, the source generator is the recommended way to avoid reflection during queries. See [Native AOT](Native%20AOT.md) for the full AOT setup, including the trimmer descriptor and the `[UnconditionalSuppressMessage]` usage on methods that build expression trees directly.

## How it works

For each `db.Table<T>()` call and each `Select(...)` lambda in your code, the generator emits a method that reads the right columns from the row and creates the result object. At runtime, SQLite.Framework looks up the method in a dictionary keyed by the entity type (for entities) or by a canonical signature of the lambda body (for `Select` projections). If it finds one, it calls it. If it does not, it builds the materializer the normal way using reflection.

You do not need to know any of this to use the package. Just install it and call `UseGeneratedMaterializers`.
