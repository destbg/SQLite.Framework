# Recipes

Short, copy-pasteable patterns composed from features documented elsewhere. Each section links to the pages with the full story.

## Keyset pagination

Offset paging (`Skip`/`Take`) rescans everything it skips, so page 1000 is slow. Keyset paging remembers the last row of the previous page and continues after it, every page costs the same.

```csharp
List<Book> page = await db.Table<Book>()
    .Where(b => b.Id > lastSeenId)
    .OrderBy(b => b.Id)
    .Take(pageSize)
    .ToListAsync();

int lastId = page.Count > 0 ? page[^1].Id : lastSeenId;
```

For a non-unique sort column, add the key as a tiebreaker and expand the comparison:

```csharp
List<Book> page = await db.Table<Book>()
    .Where(b => b.Title.CompareTo(lastTitle) > 0
        || (b.Title == lastTitle && b.Id > lastSeenId))
    .OrderBy(b => b.Title)
    .ThenBy(b => b.Id)
    .Take(pageSize)
    .ToListAsync();
```

The expanded `||` form is the way to write a composite comparison, C# has no `(a, b) > (x, y)` operator to translate. An index on the sort columns makes the seek cheap, see [Schema](Schema).

## Optimistic concurrency

There is no built-in concurrency token. A plain `int` version column and a guarded `ExecuteUpdate` give you the same protection, the affected-row count is the check.

```csharp
public class Document
{
    [Key]
    public int Id { get; set; }
    public string Content { get; set; } = "";
    public int Version { get; set; }
}

int updated = await db.Table<Document>()
    .Where(d => d.Id == doc.Id && d.Version == doc.Version)
    .ExecuteUpdateAsync(s => s
        .Set(d => d.Content, doc.Content)
        .Set(d => d.Version, d => d.Version + 1));

if (updated == 0)
{
    throw new InvalidOperationException("The document was changed by someone else. Reload and retry.");
}
```

When another writer bumped `Version` first, the `Where` matches nothing and `updated` is 0.

## Hierarchies with a recursive CTE

Categories, org charts and threaded comments all reduce to a `ParentId` column plus a recursive [CTE](Common%20Table%20Expressions). All descendants of one node:

```csharp
SQLiteCte<Category> subtree = db.WithRecursive<Category>(self =>
    db.Table<Category>().Where(c => c.Id == rootId)
      .Concat(
          from c in db.Table<Category>()
          join p in self on c.ParentId equals p.Id
          select c));

List<Category> all = await (from c in subtree select c).ToListAsync();
```

The first operand seeds the recursion, the `Concat` adds each next level until no new rows appear. Walk upward instead (all ancestors) by seeding with the leaf and joining `p.ParentId equals c.Id`.

## Search-as-you-type with FTS5

Prefix matching plus BM25 ranking gives instant-feeling search over large text. Declare the FTS table once, see [Full Text Search](Full%20Text%20Search), then run each keystroke through a prefix query:

```csharp
List<ArticleSearch> hits = await db.Table<ArticleSearch>()
    .Where(a => SQLiteFTS5Functions.Match(a, f => f.Prefix(term)))
    .OrderBy(a => SQLiteFTS5Functions.Rank(a))
    .Take(10)
    .ToListAsync();
```

`Prefix(term)` matches tokens starting with the typed text and `Rank` orders by relevance, weighted by the `[FullTextIndexed(Weight = ...)]` declarations. Skip queries shorter than two or three characters, they match too much to be useful.

## Audit timestamps

The framework never writes timestamps behind your back, so pick the layer that suits you.

Hooks on the options builder are the simplest. They run for `Add` and `Update` on matching entities:

```csharp
SQLiteOptions options = new SQLiteOptionsBuilder("app.db")
    .OnAdd<Order>(o => o.CreatedAt = DateTime.UtcNow)
    .OnUpdate<Order>(o => o.UpdatedAt = DateTime.UtcNow)
    .Build();
```

An interface registration covers every entity in one line, `.OnAdd<IHasTimestamps>(e => e.CreatedAt = DateTime.UtcNow)`. Hooks do not run for `ExecuteUpdate`, stamp the column in the `Set` list there.

To also catch writes that bypass your app entirely, move the stamp into the database with a [trigger](Triggers) or write a shadow column on every save with `WithColumns`, see [CRUD Operations](CRUD%20Operations).
