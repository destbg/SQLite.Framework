# Full Text Search

SQLite has a built-in full-text search engine called [FTS5](https://www.sqlite.org/fts5.html). The framework wraps it so you can declare an FTS table as a normal class, query it with LINQ, and get ranked results.

## Requirements

FTS5 needs **SQLite 3.9.0 or newer**. On mobile that means:

- **iOS 10 or newer.**
- **Android 7 Nougat or newer (API level 24).**

In a MAUI or multi-targeted csproj, set the minimum platform version per target so the .NET platform compatibility analyzer (CA1416) stops warning:

```xml
<PropertyGroup>
    <SupportedOSPlatformVersion Condition="'$(TargetPlatformIdentifier)' == 'android'">24.0</SupportedOSPlatformVersion>
    <SupportedOSPlatformVersion Condition="'$(TargetPlatformIdentifier)' == 'ios'">10.0</SupportedOSPlatformVersion>
</PropertyGroup>
```

If you target older platforms, install [`SQLite.Framework.Bundled`](Home) instead. It ships its own recent SQLite and skips the OS version check entirely.

> The `trigram` tokenizer needs SQLite 3.34 or newer. The other tokenizers (`unicode61`, `porter`, `ascii`, custom) work on every supported SQLite version.

## Defining an FTS table

An FTS table is a class with `[FullTextSearch]`. Each searchable column is a property with `[FullTextIndexed]`. The implicit `rowid` column maps to a property marked `[FullTextRowId]`.

```csharp
using SQLite.Framework.Attributes;
using SQLite.Framework.Enums;

[FullTextSearch(
    ContentMode = FtsContentMode.External,
    ContentTable = typeof(Article),
    AutoSync = FtsAutoSync.Triggers)]
public class ArticleSearch
{
    [FullTextRowId]
    public int Id { get; set; }

    [FullTextIndexed(Weight = 10.0)]
    public required string Title { get; set; }

    [FullTextIndexed]
    public required string Body { get; set; }
}
```

`Weight` controls the BM25 score: matches in `Title` count ten times more than matches in `Body`.

Create the table the normal way:

```csharp
db.Schema.CreateTable<Article>();
db.Schema.CreateTable<ArticleSearch>();
```

## Content modes

`ContentMode` decides where the indexed values come from.

| Mode | When to use |
|---|---|
| `Internal` (default) | The FTS table stores the values. You insert into the FTS table directly. |
| `External` | The FTS table reads values from a normal table. Set `ContentTable = typeof(...)`. The source's `[Key]` property is the row id link. |
| `Contentless` | Index only, no row storage. You can search but not read the values back. |

For `External`, the framework reuses the source table's `[Key]` for `content_rowid`. If you need a different column, set `ContentRowIdColumn = nameof(Article.Slug)` on the attribute.

## Auto-sync triggers

With `External`, every write to the source table needs a matching write to the FTS table. The framework can wire that up for you. Set `AutoSync = FtsAutoSync.Triggers` on the attribute and `Schema.CreateTable<T>()` will create the standard FTS5 sync triggers (insert, update, delete) on the source table.

The default is `FtsAutoSync.Manual`, where you write to the FTS table yourself.

## Tokenizers

Pick a tokenizer with one attribute on the class. If you do not pick one, FTS5 uses `unicode61` with default settings.

```csharp
[Unicode61Tokenizer(RemoveDiacritics = Unicode61Diacritics.RemoveAll)]
public class ArticleSearch { ... }

[PorterTokenizer(Base = PorterBaseTokenizer.Unicode61)]
public class StemmedSearch { ... }

[TrigramTokenizer(CaseSensitive = false)]
public class CodeSearch { ... }

[AsciiTokenizer]
public class FastAsciiSearch { ... }

[CustomTokenizer("my_tokenizer", "arg1", "arg2")]
public class CustomSearch { ... }
```

| Tokenizer | What it does |
|---|---|
| `Unicode61Tokenizer` | The default. Splits on Unicode word boundaries. Folds case. Optionally strips diacritics. |
| `PorterTokenizer` | Wraps another tokenizer with the Porter English stemmer. "running" matches "ran" and "runs". |
| `TrigramTokenizer` | Indexes 3-character substrings, so "sqli" matches "sqlite". Good for code or substring search. Larger index. |
| `AsciiTokenizer` | Faster than `unicode61` but only handles ASCII. |
| `CustomTokenizer` | Use a tokenizer you registered through SQLite's C API. |

## Searching

Match is a marker method that lives on `SQLiteFunctions`. It works inside `Where`.

### String form

```csharp
List<ArticleSearch> hits = db.Table<ArticleSearch>()
    .Where(a => SQLiteFunctions.Match(a, "native AND aot"))
    .ToList();
```

The string is the raw FTS5 query, see the [FTS5 query syntax](https://www.sqlite.org/fts5.html#full_text_query_syntax) docs.

### Builder form

If you do not want to write FTS5 syntax by hand, pass a lambda. The lambda receives a builder `f` with `Term`, `Phrase`, `Prefix`, `Near`, and `Column` methods, combined with the standard C# operators `&&`, `||`, and `!`.

```csharp
.Where(a => SQLiteFunctions.Match(a, f => f.Term("native") && f.Term("aot")))

.Where(a => SQLiteFunctions.Match(a, f => f.Phrase("native aot")))

.Where(a => SQLiteFunctions.Match(a, f => f.Prefix("nativ")))

.Where(a => SQLiteFunctions.Match(a, f => f.Near(2, "ahead", "time")))
```

### Searching one column

Pass a property reference instead of the entity. The translator emits a column-scoped match.

```csharp
.Where(a => SQLiteFunctions.Match(a.Title, "native"))

.Where(a => SQLiteFunctions.Match(a.Title, f => f.Prefix("nativ")))
```

To mix a column scope with other terms, use `f.Column` inside the builder lambda:

```csharp
.Where(a => SQLiteFunctions.Match(a,
    f => f.Column(a.Title, f.Prefix("aot")) || f.Term("trim")))
```

## Ranking

`SQLiteFunctions.Rank(entity)` returns the BM25 score of the row. Use it inside `OrderBy`. The per-column weights from `[FullTextIndexed(Weight = ...)]` are applied automatically.

```csharp
db.Table<ArticleSearch>()
    .Where(a => SQLiteFunctions.Match(a, "native"))
    .OrderBy(a => SQLiteFunctions.Rank(a))
    .Take(20)
    .ToList();
```

## Snippets and highlights

Project a column with the matching tokens wrapped in markers:

```csharp
var hits = db.Table<ArticleSearch>()
    .Where(a => SQLiteFunctions.Match(a, "native"))
    .OrderBy(a => SQLiteFunctions.Rank(a))
    .Select(a => new
    {
        a.Id,
        Title = SQLiteFunctions.Highlight(a, a.Title, "<b>", "</b>"),
        Body = SQLiteFunctions.Snippet(a, a.Body, "<b>", "</b>", "...", 32),
    })
    .ToList();
```

`Highlight` wraps every matching token. `Snippet` returns a short window of text around the matches, with the `ellipsis` marker on either side when the snippet is truncated.

## Joining the FTS table to its source

When `ContentMode = External`, the FTS rowid is the same as the source table's primary key, so a normal LINQ join works:

```csharp
var hits = (
    from s in db.Table<ArticleSearch>()
    join a in db.Table<Article>() on s.Id equals a.Id
    where SQLiteFunctions.Match(s, "native aot")
    orderby SQLiteFunctions.Rank(s)
    select new { a.Id, a.Title, a.PublishedAt })
    .Take(20)
    .ToList();
```

## Multiple FTS tables on the same source

You can point several FTS classes at the same source. Each one is independent and has its own tokenizer config, columns, and triggers.

```csharp
[FullTextSearch(ContentMode = FtsContentMode.External, ContentTable = typeof(Article), AutoSync = FtsAutoSync.Triggers)]
public class ArticleSearchUnicode { ... }

[FullTextSearch(ContentMode = FtsContentMode.External, ContentTable = typeof(Article), AutoSync = FtsAutoSync.Triggers)]
[TrigramTokenizer]
public class ArticleSearchTrigram { ... }
```

Both can be queried in the same LINQ expression and joined together.
