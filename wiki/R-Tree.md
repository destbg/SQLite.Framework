# R-Tree

R-Tree is SQLite's built-in spatial index. It stores bounding boxes in 1 to 5 dimensions and answers range queries with a logarithmic seek instead of a full scan. The framework lets you map a class to an R-Tree virtual table and use the same `Table<T>` surface as for normal tables.

R-Tree requires SQLite 3.24.0 or newer to use auxiliary columns. The base module is available since SQLite 3.6.

## Defining an R-Tree entity

Mark the class with `[RTreeIndex]`. The class needs:

* Exactly one `[Key]` property of type `int` or `long`. This is the R-Tree rowid.
* One or more pairs of `[RTreeMin]` and `[RTreeMax]` properties grouped by a dimension name.
* Zero or more `[RTreeAuxiliary]` properties. The framework emits these with the SQLite `+` prefix, which means they ride along with the row but do not participate in the spatial index.

```csharp
using SQLite.Framework.Attributes;
using System.ComponentModel.DataAnnotations;

[RTreeIndex]
public class Region
{
    [Key] public int Id { get; set; }

    [RTreeMin("X")] public float MinX { get; set; }
    [RTreeMax("X")] public float MaxX { get; set; }
    [RTreeMin("Y")] public float MinY { get; set; }
    [RTreeMax("Y")] public float MaxY { get; set; }

    [RTreeAuxiliary] public string? Label { get; set; }
}
```

The framework emits:

```sql
CREATE VIRTUAL TABLE "Region" USING rtree(Id, MinX, MaxX, MinY, MaxY, +Label)
```

`CreateTable` is idempotent, the same as for a normal table.

## Storage variants

Pass `SQLiteRTreeStorage.Int32` to the attribute to use the `rtree_i32` module. Coordinates are stored as 32-bit signed integers. Only `int` properties are accepted as min and max columns under this mode.

```csharp
[RTreeIndex(SQLiteRTreeStorage.Int32)]
public class GridCell
{
    [Key] public int Id { get; set; }
    [RTreeMin("X")] public int MinX { get; set; }
    [RTreeMax("X")] public int MaxX { get; set; }
    [RTreeMin("Y")] public int MinY { get; set; }
    [RTreeMax("Y")] public int MaxY { get; set; }
}
```

The default `SQLiteRTreeStorage.Float` uses `rtree` and accepts `float`, `double`, or `int` properties. SQLite always stores REAL as 8 bytes, so picking `float` and `double` is purely a CLR-side choice.

## Querying

R-Tree tables support the same LINQ surface as normal tables. The query planner will use the spatial index when the `Where` clause restricts every dimension with `min <= ? AND max >= ?` (or `BETWEEN`).

```csharp
List<Region> hits = db.Table<Region>()
    .Where(r => r.MinX <= 5 && r.MaxX >= 5 && r.MinY <= 5 && r.MaxY >= 5)
    .ToList();
```

You can also filter by an auxiliary column. The framework emits a regular `WHERE` clause for those, which SQLite applies after the R-Tree lookup.

```csharp
List<Region> hits = db.Table<Region>()
    .Where(r => r.MinX <= 5 && r.MaxX >= 5 && r.Label == "downtown")
    .ToList();
```

## Inserting, updating, removing

`Add`, `AddRange`, `Update`, `UpdateRange`, `Remove`, and `RemoveRange` work just like with a normal table. The framework binds the rowid, the bounding-box columns, and any auxiliary columns from the entity.

```csharp
db.Table<Region>().Add(new Region
{
    Id = 1,
    MinX = 0, MaxX = 10,
    MinY = 0, MaxY = 10,
    Label = "downtown",
});

Region row = db.Table<Region>().First();
row.MaxX = 50;
row.MaxY = 50;
db.Table<Region>().Update(row);

db.Table<Region>().Remove(row);
```

## Limits

* SQLite allows 1 to 5 dimensions. The framework throws at mapping time when there are more than 5.
* Every `[RTreeMin]` must have a matching `[RTreeMax]` with the same dimension name, and vice versa.
* Coordinate columns must be `float`, `double`, or `int` for `Float` storage. Only `int` is accepted under `Int32` storage.
* The rowid column must be `int` or `long`.
* The same class cannot be both `[RTreeIndex]` and `[FullTextSearch]`.
