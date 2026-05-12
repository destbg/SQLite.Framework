using System.ComponentModel.DataAnnotations;
using SQLite.Framework.Attributes;
using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class ReturningTests
{
    [Fact]
    public void ExecuteDelete_Returning_FullEntity_ReturnsDeletedRows()
    {
        using TestDatabase db = new();
        db.Table<ReturningItem>().Schema.CreateTable();
        db.Table<ReturningItem>().Add(new ReturningItem { Id = 1, Name = "alpha", Price = 10.0 });
        db.Table<ReturningItem>().Add(new ReturningItem { Id = 2, Name = "beta", Price = 20.0 });
        db.Table<ReturningItem>().Add(new ReturningItem { Id = 3, Name = "gamma", Price = 30.0 });

        List<ReturningItem> deleted = db.Table<ReturningItem>()
            .Where(x => x.Price > 15)
            .Returning()
            .ExecuteDelete();

        Assert.Equal(2, deleted.Count);
        Assert.Contains(deleted, r => r.Id == 2 && r.Name == "beta");
        Assert.Contains(deleted, r => r.Id == 3 && r.Name == "gamma");

        List<ReturningItem> remaining = db.Table<ReturningItem>().ToList();
        Assert.Single(remaining);
        Assert.Equal(1, remaining[0].Id);
    }

    [Fact]
    public void ExecuteDelete_Returning_ScalarProjection_ReturnsValues()
    {
        using TestDatabase db = new();
        db.Table<ReturningItem>().Schema.CreateTable();
        db.Table<ReturningItem>().Add(new ReturningItem { Id = 1, Name = "alpha", Price = 10.0 });
        db.Table<ReturningItem>().Add(new ReturningItem { Id = 2, Name = "beta", Price = 20.0 });

        List<int> ids = db.Table<ReturningItem>()
            .Where(x => x.Price >= 10)
            .Returning(x => x.Id)
            .ExecuteDelete();

        Assert.Equal(2, ids.Count);
        Assert.Contains(1, ids);
        Assert.Contains(2, ids);
    }

    [Fact]
    public void ExecuteDelete_Returning_AnonymousProjection_ReturnsValues()
    {
        using TestDatabase db = new();
        db.Table<ReturningItem>().Schema.CreateTable();
        db.Table<ReturningItem>().Add(new ReturningItem { Id = 7, Name = "seven", Price = 70.0 });

        var rows = db.Table<ReturningItem>()
            .Where(x => x.Id == 7)
            .Returning(x => new { x.Id, x.Name })
            .ExecuteDelete();

        Assert.Single(rows);
        Assert.Equal(7, rows[0].Id);
        Assert.Equal("seven", rows[0].Name);
    }

    [Fact]
    public void ExecuteDelete_Returning_NoRowsMatch_ReturnsEmpty()
    {
        using TestDatabase db = new();
        db.Table<ReturningItem>().Schema.CreateTable();
        db.Table<ReturningItem>().Add(new ReturningItem { Id = 1, Name = "a", Price = 1 });

        List<ReturningItem> deleted = db.Table<ReturningItem>()
            .Where(x => x.Id == 999)
            .Returning()
            .ExecuteDelete();

        Assert.Empty(deleted);
    }

    [Fact]
    public void ExecuteUpdate_Returning_ReturnsUpdatedRows()
    {
        using TestDatabase db = new();
        db.Table<ReturningItem>().Schema.CreateTable();
        db.Table<ReturningItem>().Add(new ReturningItem { Id = 1, Name = "alpha", Price = 10.0 });
        db.Table<ReturningItem>().Add(new ReturningItem { Id = 2, Name = "beta", Price = 20.0 });

        List<ReturningItem> updated = db.Table<ReturningItem>()
            .Where(x => x.Price > 5)
            .Returning()
            .ExecuteUpdate(s => s.Set(x => x.Price, x => x.Price + 100));

        Assert.Equal(2, updated.Count);
        Assert.All(updated, r => Assert.True(r.Price >= 110));
    }

    [Fact]
    public void ExecuteUpdate_Returning_ScalarProjection_ReturnsNewValues()
    {
        using TestDatabase db = new();
        db.Table<ReturningItem>().Schema.CreateTable();
        db.Table<ReturningItem>().Add(new ReturningItem { Id = 1, Name = "alpha", Price = 10.0 });
        db.Table<ReturningItem>().Add(new ReturningItem { Id = 2, Name = "beta", Price = 20.0 });

        List<double> prices = db.Table<ReturningItem>()
            .Where(x => x.Id > 0)
            .Returning(x => x.Price)
            .ExecuteUpdate(s => s.Set(x => x.Price, x => x.Price * 2));

        Assert.Equal(2, prices.Count);
        Assert.Contains(20.0, prices);
        Assert.Contains(40.0, prices);
    }

    [Fact]
    public void Returning_RequiresFrameworkQueryable()
    {
        IQueryable<ReturningItem> source = new List<ReturningItem>().AsQueryable();

        Assert.Throws<InvalidOperationException>(() => source.Returning());
    }

    [Fact]
    public void TableReturning_Add_ReturnsInsertedRow()
    {
        using TestDatabase db = new();
        db.Table<ReturningItem>().Schema.CreateTable();

        ReturningItem? inserted = db.Table<ReturningItem>()
            .Returning()
            .Add(new ReturningItem { Id = 10, Name = "alpha", Price = 5 });

        Assert.NotNull(inserted);
        Assert.Equal(10, inserted!.Id);
        Assert.Equal("alpha", inserted.Name);
        Assert.Equal(5.0, inserted.Price);
    }

    [Fact]
    public void TableReturning_Add_WithAutoIncrement_ReturnsAssignedId()
    {
        using TestDatabase db = new();
        db.Table<ReturningAuto>().Schema.CreateTable();

        int id = db.Table<ReturningAuto>()
            .Returning(x => x.Id)
            .Add(new ReturningAuto { Name = "a" });

        Assert.True(id > 0);
    }

    [Fact]
    public void TableReturning_Add_IdentityProjection_BackfillsAutoIncrementOnEntity()
    {
        using TestDatabase db = new();
        db.Table<ReturningAuto>().Schema.CreateTable();

        ReturningAuto entity = new() { Name = "a" };
        ReturningAuto? returned = db.Table<ReturningAuto>()
            .Returning()
            .Add(entity);

        Assert.NotNull(returned);
        Assert.True(returned!.Id > 0);
        Assert.Equal(returned.Id, entity.Id);
    }

    [Fact]
    public void TableReturning_Add_WithTriggerSideEffect_RowIsObservedAndTriggerFired()
    {
        using TestDatabase db = new();
        db.Table<ReturningItem>().Schema.CreateTable();
        db.Table<ReturningChild>().Schema.CreateTable();
        db.Execute("""
            CREATE TRIGGER trg_returning_item_log AFTER INSERT ON ReturningItem
            FOR EACH ROW
            BEGIN
                INSERT INTO ReturningChild (Id, ParentId) VALUES (NEW.Id, NEW.Id);
            END;
            """);

        ReturningItem? inserted = db.Table<ReturningItem>()
            .Returning()
            .Add(new ReturningItem { Id = 42, Name = "trig", Price = 10 });

        Assert.NotNull(inserted);
        Assert.Equal(42, inserted!.Id);
        Assert.Equal("trig", inserted.Name);

        List<ReturningChild> logged = db.Table<ReturningChild>().ToList();
        Assert.Single(logged);
        Assert.Equal(42, logged[0].ParentId);
    }

    [Fact]
    public void TableReturning_AddRange_ReturnsAllRows()
    {
        using TestDatabase db = new();
        db.Table<ReturningItem>().Schema.CreateTable();

        ReturningItem[] items =
        [
            new() { Id = 1, Name = "a", Price = 1 },
            new() { Id = 2, Name = "b", Price = 2 },
            new() { Id = 3, Name = "c", Price = 3 },
        ];

        List<int> ids = db.Table<ReturningItem>()
            .Returning(x => x.Id)
            .AddRange(items);

        Assert.Equal([1, 2, 3], ids);
    }

    [Fact]
    public void TableReturning_Update_ReturnsPostUpdateRow()
    {
        using TestDatabase db = new();
        db.Table<ReturningItem>().Schema.CreateTable();
        db.Table<ReturningItem>().Add(new ReturningItem { Id = 7, Name = "old", Price = 1 });

        ReturningItem? updated = db.Table<ReturningItem>()
            .Returning()
            .Update(new ReturningItem { Id = 7, Name = "new", Price = 99 });

        Assert.NotNull(updated);
        Assert.Equal("new", updated!.Name);
        Assert.Equal(99.0, updated.Price);
    }

    [Fact]
    public void TableReturning_Update_NoMatch_ReturnsDefault()
    {
        using TestDatabase db = new();
        db.Table<ReturningItem>().Schema.CreateTable();

        ReturningItem? updated = db.Table<ReturningItem>()
            .Returning()
            .Update(new ReturningItem { Id = 404, Name = "ghost", Price = 0 });

        Assert.Null(updated);
    }

    [Fact]
    public void TableReturning_Remove_ReturnsDeletedRow()
    {
        using TestDatabase db = new();
        db.Table<ReturningItem>().Schema.CreateTable();
        db.Table<ReturningItem>().Add(new ReturningItem { Id = 1, Name = "x", Price = 5 });

        string? removedName = db.Table<ReturningItem>()
            .Returning(x => x.Name)
            .Remove(new ReturningItem { Id = 1, Name = "x", Price = 5 });

        Assert.Equal("x", removedName);
        Assert.Empty(db.Table<ReturningItem>().ToList());
    }

    [Fact]
    public void TableReturning_Remove_NoMatch_ReturnsDefault()
    {
        using TestDatabase db = new();
        db.Table<ReturningItem>().Schema.CreateTable();

        string? removed = db.Table<ReturningItem>()
            .Returning(x => x.Name)
            .Remove(new ReturningItem { Id = 999, Name = "missing", Price = 0 });

        Assert.Null(removed);
    }

    [Fact]
    public void TableReturning_AnonymousProjection_Works()
    {
        using TestDatabase db = new();
        db.Table<ReturningItem>().Schema.CreateTable();

        var inserted = db.Table<ReturningItem>()
            .Returning(x => new { x.Id, x.Name })
            .Add(new ReturningItem { Id = 5, Name = "anon", Price = 1 });

        Assert.NotNull(inserted);
        Assert.Equal(5, inserted!.Id);
        Assert.Equal("anon", inserted.Name);
    }

    [Fact]
    public void TableReturning_AddHook_CancelsAdd()
    {
        using TestDatabase db = new(o => o.OnAdd<ReturningItem>((_, item) => item.Price >= 0));
        db.Table<ReturningItem>().Schema.CreateTable();

        ReturningItem? inserted = db.Table<ReturningItem>()
            .Returning()
            .Add(new ReturningItem { Id = 1, Name = "bad", Price = -1 });

        Assert.Null(inserted);
        Assert.Empty(db.Table<ReturningItem>().ToList());
    }

    [Fact]
    public void TableReturning_Add_BeforeInsertTriggerIgnore_ReturnsDefault()
    {
        using TestDatabase db = new();
        db.Table<ReturningItem>().Schema.CreateTable();
        db.Execute("""
            CREATE TRIGGER trg_returning_ignore_negative BEFORE INSERT ON ReturningItem
            FOR EACH ROW
            WHEN NEW.Price < 0
            BEGIN
                SELECT RAISE(IGNORE);
            END;
            """);

        ReturningItem? inserted = db.Table<ReturningItem>()
            .Returning()
            .Add(new ReturningItem { Id = 1, Name = "neg", Price = -1 });

        Assert.Null(inserted);
        Assert.Empty(db.Table<ReturningItem>().ToList());
    }

    [Fact]
    public void TableReturning_AddRange_WithAutoIncrement_BackfillsEveryEntity()
    {
        using TestDatabase db = new();
        db.Table<ReturningAuto>().Schema.CreateTable();

        ReturningAuto[] items =
        [
            new() { Name = "a" },
            new() { Name = "b" },
            new() { Name = "c" },
        ];

        List<ReturningAuto> rows = db.Table<ReturningAuto>()
            .Returning()
            .AddRange(items);

        Assert.Equal(3, rows.Count);
        Assert.All(items, item => Assert.True(item.Id > 0));
        Assert.Equal(items.Select(i => i.Id).ToList(), rows.Select(r => r.Id).ToList());
    }

    [Fact]
    public void TableReturning_AddRange_WithoutTransaction_Works()
    {
        using TestDatabase db = new();
        db.Table<ReturningItem>().Schema.CreateTable();

        ReturningItem[] items =
        [
            new() { Id = 1, Name = "a", Price = 1 },
            new() { Id = 2, Name = "b", Price = 2 },
        ];

        List<int> ids = db.Table<ReturningItem>()
            .Returning(x => x.Id)
            .AddRange(items, runInTransaction: false);

        Assert.Equal([1, 2], ids);
    }

    [Fact]
    public void TableReturning_UpdateHook_CancelsUpdate()
    {
        using TestDatabase db = new(o => o.OnUpdate<ReturningItem>((_, item) => item.Price >= 0));
        db.Table<ReturningItem>().Schema.CreateTable();
        db.Table<ReturningItem>().Add(new ReturningItem { Id = 1, Name = "x", Price = 10 });

        ReturningItem? updated = db.Table<ReturningItem>()
            .Returning()
            .Update(new ReturningItem { Id = 1, Name = "x", Price = -5 });

        Assert.Null(updated);
    }

    [Fact]
    public void TableReturning_UpdateRange_ReturnsPostUpdateRows()
    {
        using TestDatabase db = new();
        db.Table<ReturningItem>().Schema.CreateTable();
        db.Table<ReturningItem>().Add(new ReturningItem { Id = 1, Name = "a", Price = 1 });
        db.Table<ReturningItem>().Add(new ReturningItem { Id = 2, Name = "b", Price = 2 });

        ReturningItem[] updates =
        [
            new() { Id = 1, Name = "A", Price = 11 },
            new() { Id = 2, Name = "B", Price = 22 },
        ];

        List<ReturningItem> rows = db.Table<ReturningItem>()
            .Returning()
            .UpdateRange(updates);

        Assert.Equal(2, rows.Count);
        Assert.Contains(rows, r => r.Id == 1 && r.Name == "A" && r.Price == 11);
        Assert.Contains(rows, r => r.Id == 2 && r.Name == "B" && r.Price == 22);
    }

    [Fact]
    public void TableReturning_RemoveHook_CancelsRemove()
    {
        using TestDatabase db = new(o => o.OnRemove<ReturningItem>((_, _) => false));
        db.Table<ReturningItem>().Schema.CreateTable();
        db.Table<ReturningItem>().Add(new ReturningItem { Id = 1, Name = "x", Price = 1 });

        ReturningItem? removed = db.Table<ReturningItem>()
            .Returning()
            .Remove(new ReturningItem { Id = 1, Name = "x", Price = 1 });

        Assert.Null(removed);
        Assert.Single(db.Table<ReturningItem>().ToList());
    }

    [Fact]
    public void TableReturning_RemoveRange_ReturnsDeletedRows()
    {
        using TestDatabase db = new();
        db.Table<ReturningItem>().Schema.CreateTable();
        db.Table<ReturningItem>().Add(new ReturningItem { Id = 1, Name = "a", Price = 1 });
        db.Table<ReturningItem>().Add(new ReturningItem { Id = 2, Name = "b", Price = 2 });
        db.Table<ReturningItem>().Add(new ReturningItem { Id = 3, Name = "c", Price = 3 });

        ReturningItem[] toRemove =
        [
            new() { Id = 1, Name = "a", Price = 1 },
            new() { Id = 3, Name = "c", Price = 3 },
        ];

        List<int> removedIds = db.Table<ReturningItem>()
            .Returning(x => x.Id)
            .RemoveRange(toRemove, runInTransaction: false);

        Assert.Equal([1, 3], removedIds);
        Assert.Single(db.Table<ReturningItem>().ToList());
    }

    [Fact]
    public void TableReturning_Returning_ProjectionWithLiteral_BindsRpPrefixedParam()
    {
        using TestDatabase db = new();
        db.Table<ReturningItem>().Schema.CreateTable();

        int adjusted = db.Table<ReturningItem>()
            .Returning(x => x.Id + 100)
            .Add(new ReturningItem { Id = 7, Name = "lit", Price = 1 });

        Assert.Equal(107, adjusted);
    }

    [Fact]
    public void TableReturning_Add_ExplicitAutoIncrementKeysPreserved_IncludesIdInColumns()
    {
        using TestDatabase db = new(o => o.PreserveExplicitAutoIncrementKeys());
        db.Table<ReturningAuto>().Schema.CreateTable();

        ReturningAuto entity = new() { Name = "preset" };
        ReturningAuto? inserted = db.Table<ReturningAuto>()
            .Returning()
            .Add(entity);

        Assert.NotNull(inserted);
        Assert.True(inserted!.Id > 0);
        Assert.Equal(inserted.Id, entity.Id);
    }

    [Fact]
    public async Task TableReturning_AddAsync_ReturnsInsertedRow()
    {
        using TestDatabase db = new();
        db.Table<ReturningItem>().Schema.CreateTable();

        ReturningItem? inserted = await db.Table<ReturningItem>()
            .Returning()
            .AddAsync(new ReturningItem { Id = 1, Name = "async", Price = 1 });

        Assert.NotNull(inserted);
        Assert.Equal(1, inserted!.Id);
    }

    [Fact]
    public async Task TableReturning_AddRangeAsync_ReturnsInsertedRows()
    {
        using TestDatabase db = new();
        db.Table<ReturningItem>().Schema.CreateTable();

        List<ReturningItem> rows = await db.Table<ReturningItem>()
            .Returning()
            .AddRangeAsync([
                new() { Id = 1, Name = "a", Price = 1 },
                new() { Id = 2, Name = "b", Price = 2 },
            ]);

        Assert.Equal(2, rows.Count);
    }

    [Fact]
    public async Task TableReturning_UpdateAsync_ReturnsPostUpdateRow()
    {
        using TestDatabase db = new();
        db.Table<ReturningItem>().Schema.CreateTable();
        db.Table<ReturningItem>().Add(new ReturningItem { Id = 1, Name = "old", Price = 1 });

        ReturningItem? updated = await db.Table<ReturningItem>()
            .Returning()
            .UpdateAsync(new ReturningItem { Id = 1, Name = "new", Price = 99 });

        Assert.NotNull(updated);
        Assert.Equal("new", updated!.Name);
    }

    [Fact]
    public async Task TableReturning_UpdateRangeAsync_ReturnsPostUpdateRows()
    {
        using TestDatabase db = new();
        db.Table<ReturningItem>().Schema.CreateTable();
        db.Table<ReturningItem>().Add(new ReturningItem { Id = 1, Name = "a", Price = 1 });

        List<ReturningItem> rows = await db.Table<ReturningItem>()
            .Returning()
            .UpdateRangeAsync([new ReturningItem { Id = 1, Name = "A", Price = 11 }]);

        Assert.Single(rows);
        Assert.Equal("A", rows[0].Name);
    }

    [Fact]
    public async Task TableReturning_RemoveAsync_ReturnsDeletedRow()
    {
        using TestDatabase db = new();
        db.Table<ReturningItem>().Schema.CreateTable();
        db.Table<ReturningItem>().Add(new ReturningItem { Id = 1, Name = "x", Price = 1 });

        ReturningItem? removed = await db.Table<ReturningItem>()
            .Returning()
            .RemoveAsync(new ReturningItem { Id = 1, Name = "x", Price = 1 });

        Assert.NotNull(removed);
        Assert.Equal(1, removed!.Id);
    }

    [Fact]
    public async Task TableReturning_RemoveRangeAsync_ReturnsDeletedRows()
    {
        using TestDatabase db = new();
        db.Table<ReturningItem>().Schema.CreateTable();
        db.Table<ReturningItem>().Add(new ReturningItem { Id = 1, Name = "a", Price = 1 });
        db.Table<ReturningItem>().Add(new ReturningItem { Id = 2, Name = "b", Price = 2 });

        List<ReturningItem> rows = await db.Table<ReturningItem>()
            .Returning()
            .RemoveRangeAsync([
                new() { Id = 1, Name = "a", Price = 1 },
                new() { Id = 2, Name = "b", Price = 2 },
            ]);

        Assert.Equal(2, rows.Count);
    }

    [Fact]
    public async Task QueryableReturning_ExecuteDeleteAsync_ReturnsDeletedRows()
    {
        using TestDatabase db = new();
        db.Table<ReturningItem>().Schema.CreateTable();
        db.Table<ReturningItem>().Add(new ReturningItem { Id = 1, Name = "a", Price = 1 });

        List<int> ids = await db.Table<ReturningItem>()
            .Where(x => x.Id == 1)
            .Returning(x => x.Id)
            .ExecuteDeleteAsync();

        Assert.Equal([1], ids);
    }

    [Fact]
    public async Task QueryableReturning_ExecuteUpdateAsync_ReturnsUpdatedRows()
    {
        using TestDatabase db = new();
        db.Table<ReturningItem>().Schema.CreateTable();
        db.Table<ReturningItem>().Add(new ReturningItem { Id = 1, Name = "a", Price = 1 });

        List<double> prices = await db.Table<ReturningItem>()
            .Where(x => x.Id == 1)
            .Returning(x => x.Price)
            .ExecuteUpdateAsync(s => s.Set(x => x.Price, x => x.Price + 10));

        Assert.Equal([11.0], prices);
    }

#if !SQLITECIPHER
    [Fact]
    public void TableReturning_Add_JsonbColumn_AppliesColumnSqlExpression()
    {
        using TestDatabase db = new(b => b.AddJsonbContext(PersonRootJsonContext.Default));
        db.Table<PersonEntity>().Schema.CreateTable();

        PersonEntity? inserted = db.Table<PersonEntity>()
            .Returning()
            .Add(new PersonEntity
            {
                Id = 1,
                Person = new Person { Name = "Alice", Home = new Address { Street = "1 Oak", City = "Shelbyville" } },
            });

        Assert.NotNull(inserted);
        Assert.Equal("Alice", inserted!.Person.Name);
        Assert.Equal("Shelbyville", inserted.Person.Home.City);
    }
#endif
}

public class ReturningAuto
{
    [Key, AutoIncrement]
    public int Id { get; set; }

    public required string Name { get; set; }
}

public class ReturningChild
{
    [Key]
    public int Id { get; set; }

    public int ParentId { get; set; }
}

public class ReturningItem
{
    [Key]
    public int Id { get; set; }

    public required string Name { get; set; }

    public double Price { get; set; }
}
