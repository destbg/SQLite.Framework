# Common Table Expressions

Common Table Expressions (CTEs) let you create a subquery and refer to it later in the same statement. SQLite supports two forms: non-recursive (`WITH`) and recursive (`WITH RECURSIVE`).

## Non-recursive CTE

Use `With` to define a CTE from any LINQ query:

```csharp
SQLiteCte<Book> expensiveBooks = db.With(() =>
    db.Table<Book>().Where(b => b.Price > 30));

List<Book> results = (from b in expensiveBooks select b).ToList();
```

Generated SQL:

```sql
WITH cte0 AS (
    SELECT b1.BookId AS "Id",
       b1.BookTitle AS "Title",
       b1.BookAuthorId AS "AuthorId",
       b1.BookPrice AS "Price"
    FROM "Books" AS b1
    WHERE b1.BookPrice > @p0
)
SELECT b0.Id AS "Id",
       b0.Title AS "Title",
       b0.AuthorId AS "AuthorId",
       b0.Price AS "Price"
FROM cte0 AS b0
```

The CTE body can be any queryable expression including `Where`, `Select`, `OrderBy`, joins, and set operations.

## Using a CTE in joins

A CTE can be used on either side of a join:

```csharp
SQLiteCte<Book> cheapBooks = db.With(() =>
    db.Table<Book>().Where(b => b.Price < 20));

var results = (
    from b in cheapBooks
    join a in db.Table<Author>() on b.AuthorId equals a.Id
    select new { b.Title, AuthorName = a.Name }
).ToList();
```

## Values

`Values` wraps a single row of data into a queryable, useful as the seed row in a recursive CTE:

```csharp
IQueryable<Seed> seed = db.Values(new Seed { X = 0, Label = "start" });
```

This generates `(SELECT @p0 AS "X", @p1 AS "Label") AS s0` as the FROM clause.

## Recursive CTEs

Use `WithRecursive` when the query needs to reference itself. The lambda parameter is the self-reference.

### Counting

The simplest recursive CTE counts from 1 to 10:

```csharp
SQLiteCte<Cnt> counter = db.WithRecursive<Cnt>(self =>
    db.Values(new Cnt { X = 1 })
      .Concat(from c in self where c.X < 10 select new Cnt { X = c.X + 1 }));

List<Cnt> rows = (from c in counter select c).ToList();
// rows = [1, 2, 3, 4, 5, 6, 7, 8, 9, 10]
```

Generated SQL:

```sql
WITH RECURSIVE cte0 AS (
    SELECT c1."X" AS "X"
    FROM (SELECT @p1 AS "X") AS c1
    UNION ALL
        SELECT (c2.X + @p3) AS "X"
    FROM cte0 AS c2
    WHERE c2.X < @p2
)
SELECT c0.X AS "X"
FROM cte0 AS c0
ORDER BY c0.X ASC
```

### Fibonacci sequence

Generate Fibonacci numbers up to 100:

```csharp
class Fib { public int A { get; set; } public int B { get; set; } }

SQLiteCte<Fib> fib = db.WithRecursive<Fib>(self =>
    db.Values(new Fib { A = 0, B = 1 })
      .Concat(from f in self where f.B < 100 select new Fib { A = f.B, B = f.A + f.B }));

List<Fib> results = (from f in fib select f).ToList();
// results[0] = { A = 0, B = 1 }
// results[11] = { A = 89, B = 144 }
```

Generated SQL:

```sql
WITH RECURSIVE cte0 AS (
    SELECT f1."A" AS "A",
       f1."B" AS "B"
    FROM (SELECT @p2 AS "A", @p3 AS "B") AS f1
    UNION ALL
        SELECT f2.B AS "A",
       (f2.A + f2.B) AS "B"
    FROM cte0 AS f2
    WHERE f2.B < @p4
)
SELECT f0.A AS "A",
       f0.B AS "B"
FROM cte0 AS f0
```

### Org chart BFS

Walk a reporting hierarchy and assign a depth level to each person. This uses two CTEs, the first wraps the table, the second is the recursive traversal:

```csharp
class Org { public required string Name { get; set; } public string? Boss { get; set; } }
class OrgLevel { public required string Name { get; set; } public int Level { get; set; } }

SQLiteCte<Org> org = db.With(() => db.Table<Org>());
SQLiteCte<OrgLevel> hierarchy = db.WithRecursive<OrgLevel>(self =>
    (from o in org where o.Boss == null select new OrgLevel { Name = o.Name, Level = 1 })
    .Concat(from o in org
            join p in self on o.Boss equals p.Name
            select new OrgLevel { Name = o.Name, Level = p.Level + 1 }));

List<OrgLevel> result = (from n in hierarchy orderby n.Level, n.Name select n).ToList();
```

Generated SQL:

```sql
WITH RECURSIVE cte0 AS (
        SELECT o2.Name AS "Name",
       o2.Boss AS "Boss"
        FROM "Org" AS o2
),
cte1 AS (
    SELECT o1.Name AS "Name",
       @p1 AS "Level"
    FROM cte0 AS o1
    WHERE o1.Boss IS NULL
    UNION ALL
        SELECT o3.Name AS "Name",
       (o4.Level + @p2) AS "Level"
    FROM cte0 AS o3
    JOIN cte1 AS o4 ON o3.Boss = o4.Name
)
SELECT o0.Name AS "Name",
       o0.Level AS "Level"
FROM cte1 AS o0
ORDER BY o0.Level ASC, o0.Name ASC
```

### Works-for-Alice (UNION deduplication)

Find everyone who reports to Alice, directly or indirectly, without duplicates:

```csharp
class WorksFor { public required string Name { get; set; } }

SQLiteCte<Org> org = db.With(() => db.Table<Org>());
SQLiteCte<WorksFor> worksFor = db.WithRecursive<WorksFor>(self =>
    (from o in org where o.Name == "Alice" select new WorksFor { Name = o.Name })
    .Union(from o in org
           join w in self on o.Boss equals w.Name
           select new WorksFor { Name = o.Name }));

List<WorksFor> result = (from w in worksFor select w).ToList();
```

Generated SQL:

```sql
WITH RECURSIVE cte0 AS (
        SELECT o2.Name AS "Name",
       o2.Boss AS "Boss"
        FROM "Org" AS o2
),
cte1 AS (
    SELECT o1.Name AS "Name"
    FROM cte0 AS o1
    WHERE o1.Name = @p0
    UNION
        SELECT o3.Name AS "Name"
    FROM cte0 AS o3
    JOIN cte1 AS w1 ON o3.Boss = w1.Name
)
SELECT w0.Name AS "Name"
FROM cte1 AS w0
```

### Sudoku solver

This example is taken directly from the [SQLite WITH documentation](https://sqlite.org/lang_with.html). It solves a Sudoku puzzle entirely in SQL using three CTEs: one for the input grid, one to enumerate digits 1–9, and one recursive CTE that fills in blanks one at a time using `NOT EXISTS` to check row, column, and box constraints.

The puzzle string is 81 characters where `.` marks an empty cell.

```csharp
class SudInput  { public string Sud { get; set; } = ""; }
class SudDigit  { public string Z   { get; set; } = ""; public int Lp  { get; set; } }
class SudX      { public string S   { get; set; } = ""; public int Ind { get; set; } }

const string puzzle = "53..7....6..195....98....6.8...6...34..8.3..17...2...6.6....28....419..5....8..79";

SQLiteCte<SudInput> input = db.With(() => db.Values(new SudInput { Sud = puzzle }));

SQLiteCte<SudDigit> digits = db.WithRecursive<SudDigit>(self =>
    db.Values(new SudDigit { Z = "1", Lp = 1 })
      .Concat(from d in self where d.Lp < 9
              select new SudDigit { Z = (d.Lp + 1).ToString(), Lp = d.Lp + 1 }));

SQLiteCte<SudX> x = db.WithRecursive<SudX>(self =>
    (from i in input select new SudX { S = i.Sud, Ind = i.Sud.IndexOf('.') + 1 })
    .Concat(
        from xr in self
        from z in digits
        where xr.Ind > 0
           && !(from lp in digits
                where z.Z == xr.S.Substring(((xr.Ind - 1) / 9) * 9 + lp.Lp - 1, 1)
                   || z.Z == xr.S.Substring((xr.Ind - 1) % 9 + (lp.Lp - 1) * 9, 1)
                   || z.Z == xr.S.Substring(((xr.Ind - 1) / 3) % 3 * 3 + ((xr.Ind - 1) / 27) * 27 + lp.Lp + ((lp.Lp - 1) / 3) * 6 - 1, 1)
                select lp).Any()
        select new SudX {
            S   = xr.S.Substring(0, xr.Ind - 1) + z.Z + xr.S.Substring(xr.Ind),
            Ind = (xr.S.Substring(0, xr.Ind - 1) + z.Z + xr.S.Substring(xr.Ind)).IndexOf('.') + 1
        }
    ));

string solution = (from xr in x where xr.Ind == 0 select xr.S).First();
// solution = "534678912672195348198342567859761423426853791713924856961537284287419635345286179"
```

The key LINQ features used:
- `from xr in self from z in digits` → `CROSS JOIN` for trying each digit at each blank
- `!(from lp in digits where ... select lp).Any()` → `NOT EXISTS (SELECT 1 FROM ... WHERE ...)`
- `xr.S.Substring(offset, 1)` → `SUBSTR(s, offset + 1, 1)`
- `xr.S.IndexOf('.')` → `INSTR(s, '.') - 1`
- `(d.Lp + 1).ToString()` → `CAST((lp + 1) AS TEXT)`

### Tree traversal

Traverse a parent-child hierarchy starting from root nodes:

```csharp
SQLiteCte<Category> tree = db.WithRecursive<Category>(self =>
    db.Table<Category>().Where(c => c.ParentId == null)
      .Concat(
          from c in db.Table<Category>()
          join p in self on c.ParentId equals p.Id
          select c));

List<Category> allNodes = (from c in tree select c).ToList();
```
