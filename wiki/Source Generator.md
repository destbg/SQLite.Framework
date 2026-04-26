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

**Entity materializers** map a row to a .NET class. The generator scans every `db.Table<T>()`, `db.Query<T>`, `db.FromSql<T>`, `db.With<T>`, `.Cast<T>()`, `.OfType<T>()`, and `command.ExecuteQuery<T>()` call to find target types. It also scans `Select` and `SelectMany` projection result types, and the types produced by `select` clauses in query syntax. Nested private and `file sealed` classes work through a reflection-based materializer that is still registered per type, so the runtime never falls back.

**Select materializers** cover the body of a `Select`, `SelectMany`, `Join`, or `GroupBy` key selector. This includes:

- Anonymous types: `Select(b => new { b.Id, b.Title })`
- Object initialisers: `Select(b => new BookView { Id = b.Id, Title = b.Title })`
- Object initialisers with nested entity construction: `Select(b => new BookDto { Id = b.Id, Author = new AuthorDto { ... } })`
- Method calls on rows, including your own methods: `Select(b => FormatTitle(b))`
- Captured locals from the surrounding method: `Select(b => new { b.Id, Prefix = prefix + b.Title })`
- Joins and group joins written in query syntax.
- Anonymous types returned from chains, with correct member names preserved.

Shapes that still fall back to the runtime path include anonymous types whose members use a type with a custom converter (for example a user-defined `struct` bound through `AddTypeConverter`). Turn on `DisableReflectionFallback` to make the first such query throw instead of silently using reflection.

## Generic helpers

The generator follows generic methods and generic classes whose body wraps `ExecuteQuery<T>` or a `Select` projection. For each concrete instantiation it sees somewhere in the project, it emits one materializer keyed by the closed type. Two patterns are covered:

**A generic class wrapping `ExecuteQuery<T>`:**

```csharp
public class Repo<T>
{
    private readonly SQLiteDatabase db;
    public Repo(SQLiteDatabase db) => this.db = db;

    public List<T> Get(string sql)
        => db.CreateCommand(sql, []).ExecuteQuery<T>().ToList();
}

// Elsewhere in the same project:
List<Book> books = new Repo<Book>(db).Get("SELECT * FROM Books");
List<Author> authors = new Repo<Author>(db).Get("SELECT * FROM Authors");
```

The generator records `new Repo<Book>()` and `new Repo<Author>()`, then walks `Repo<T>.Get`'s body, sees `ExecuteQuery<T>` with an open `T`, and emits an entity materializer for `Book` and one for `Author`. Adding `new Repo<Customer>()` later automatically gets a `Customer` materializer the next time the generator runs.

**A generic method projecting through `Select`:**

```csharp
private static TResult ProjectFirst<T, TResult>(IQueryable<T> query)
    where T : INomenclature
    where TResult : NomenclatureDtoBase, new()
    => query.Select(f => new TResult { Id = f.Id, Name = f.Name }).First();

// Callers:
DtoA aDto = ProjectFirst<NomenclatureA, DtoA>(db.Table<NomenclatureA>());
DtoB bDto = ProjectFirst<NomenclatureB, DtoB>(db.Table<NomenclatureB>());
```

The generator records each closed call (`<NomenclatureA, DtoA>`, `<NomenclatureB, DtoB>`), substitutes the type parameters into the lambda body, and emits one Select materializer per concrete `TResult`.

What the generator can follow:

- Both class-level (`Repo<T>`) and method-level (`Run<T>`) type parameters, plus the cross product when both are generic.
- Transitive cases where one generic helper calls another, as long as the chain stays inside the same project.
- Constraint-based member access (`f.Id` where `f : T, T : INomenclature`). The generator emits the same `Convert(f, INomenclature)` shape the runtime expression tree builds for the closed call.

What is out of scope:

- Cross-assembly helpers. If `Repo<T>` lives in a referenced library, the generator only sees its compiled signature, not its body, and cannot tell that it calls `ExecuteQuery<T>`. Move the helper into the project that runs the generator, or pre-call `ExecuteQuery<ConcreteType>()` directly.
- Helpers with no concrete callsite in the same project. The generator has nothing to substitute; the runtime path is used.
- Reflection inside the helper body (`Activator.CreateInstance(typeof(TResult))` and similar). The generator only follows real `new TResult { ... }` syntax.

## Combining with AOT

If you publish with `PublishAot=true`, the source generator is the recommended way to avoid reflection during queries. See [Native AOT](Native%20AOT.md) for the full AOT setup, including the trimmer descriptor and the `[UnconditionalSuppressMessage]` usage on methods that build expression trees directly.

## How it works

For each `db.Table<T>()` call and each `Select(...)` lambda in your code, the generator emits a method that reads the right columns from the row and creates the result object. At runtime, SQLite.Framework looks up the method in a dictionary keyed by the entity type (for entities) or by a canonical signature of the lambda body (for `Select` projections). If it finds one, it calls it. If it does not, it builds the materializer the normal way using reflection.

For generic helpers, the generator additionally builds an index of every closed type-argument tuple it sees at any callsite of every generic method and generic class in the project. When a helper's body uses an open type parameter as the projection or `ExecuteQuery<T>` argument, the generator substitutes each tuple from the index and emits one materializer per concrete substitution.

You do not need to know any of this to use the package. Just install it and call `UseGeneratedMaterializers`.
