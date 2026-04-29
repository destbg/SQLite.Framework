using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class EntityHookTests
{
    [Fact]
    public void OnAdd_ActionHook_FiresAndMutatesEntity()
    {
        DateTime stamp = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        using TestDatabase db = new(b => b.OnAdd<AuditedEntity>(e => e.CreatedAt = stamp));
        db.Table<AuditedEntity>().Schema.CreateTable();

        AuditedEntity row = new() { Name = "x" };
        int affected = db.Table<AuditedEntity>().Add(row);

        Assert.Equal(1, affected);
        Assert.Equal(stamp, row.CreatedAt);
        Assert.Equal(stamp, db.Table<AuditedEntity>().Single().CreatedAt);
    }

    [Fact]
    public void OnAdd_FuncHookReturnsFalse_SkipsInsertReturnsZero()
    {
        using TestDatabase db = new(b => b.OnAdd<AuditedEntity>((_, _) => false));
        db.Table<AuditedEntity>().Schema.CreateTable();

        int affected = db.Table<AuditedEntity>().Add(new AuditedEntity { Name = "x" });

        Assert.Equal(0, affected);
        Assert.Empty(db.Table<AuditedEntity>().ToList());
    }

    [Fact]
    public void OnAdd_MultipleHooks_RunInRegistrationOrder()
    {
        List<string> order = [];
        using TestDatabase db = new(b => b
            .OnAdd<AuditedEntity>(e => order.Add($"first:{e.Name}"))
            .OnAdd<AuditedEntity>(e => order.Add($"second:{e.Name}")));
        db.Table<AuditedEntity>().Schema.CreateTable();

        db.Table<AuditedEntity>().Add(new AuditedEntity { Name = "a" });

        Assert.Equal(["first:a", "second:a"], order);
    }

    [Fact]
    public void OnAdd_OneHookCancels_SkipsRemainingHooksAndDefault()
    {
        List<string> order = [];
        using TestDatabase db = new(b => b
            .OnAdd<AuditedEntity>((_, _) =>
            {
                order.Add("first");
                return false;
            })
            .OnAdd<AuditedEntity>((_, _) =>
            {
                order.Add("second");
                return true;
            }));
        db.Table<AuditedEntity>().Schema.CreateTable();

        int affected = db.Table<AuditedEntity>().Add(new AuditedEntity { Name = "a" });

        Assert.Equal(0, affected);
        Assert.Equal(["first"], order);
        Assert.Empty(db.Table<AuditedEntity>().ToList());
    }

    [Fact]
    public void OnUpdate_ActionHook_StampsUpdatedAt()
    {
        DateTime stamp = new(2026, 2, 2, 0, 0, 0, DateTimeKind.Utc);
        using TestDatabase db = new(b => b.OnUpdate<AuditedEntity>(e => e.UpdatedAt = stamp));
        db.Table<AuditedEntity>().Schema.CreateTable();
        db.Table<AuditedEntity>().Add(new AuditedEntity { Name = "first" });

        AuditedEntity row = db.Table<AuditedEntity>().Single();
        row.Name = "renamed";
        db.Table<AuditedEntity>().Update(row);

        Assert.Equal(stamp, db.Table<AuditedEntity>().Single().UpdatedAt);
    }

    [Fact]
    public void OnRemove_FuncHookReturnsFalse_SoftDeletePattern_RoundTrip()
    {
        using TestDatabase db = new(b => b.OnRemove<AuditedEntity>((database, e) =>
        {
            e.IsDeleted = true;
            database.Table<AuditedEntity>().Update(e);
            return false;
        }));
        db.Table<AuditedEntity>().Schema.CreateTable();
        db.Table<AuditedEntity>().Add(new AuditedEntity { Name = "still here" });

        AuditedEntity row = db.Table<AuditedEntity>().Single();
        int reported = db.Table<AuditedEntity>().Remove(row);

        Assert.Equal(0, reported);
        AuditedEntity stored = db.Table<AuditedEntity>().Single();
        Assert.True(stored.IsDeleted);
        Assert.Equal("still here", stored.Name);
    }

    [Fact]
    public void OnAddRange_HookFiresPerRow_RoundTripCount()
    {
        int hookCount = 0;
        using TestDatabase db = new(b => b.OnAdd<AuditedEntity>(_ => hookCount++));
        db.Table<AuditedEntity>().Schema.CreateTable();

        int affected = db.Table<AuditedEntity>().AddRange(new[]
        {
            new AuditedEntity { Name = "a" },
            new AuditedEntity { Name = "b" },
            new AuditedEntity { Name = "c" },
        });

        Assert.Equal(3, affected);
        Assert.Equal(3, hookCount);
        Assert.Equal(3, db.Table<AuditedEntity>().Count());
    }

    [Fact]
    public void OnAddRange_OneHookCancelsOneRow_OthersInsert()
    {
        using TestDatabase db = new(b => b.OnAdd<AuditedEntity>((_, e) => e.Name != "skip"));
        db.Table<AuditedEntity>().Schema.CreateTable();

        int affected = db.Table<AuditedEntity>().AddRange(new[]
        {
            new AuditedEntity { Name = "a" },
            new AuditedEntity { Name = "skip" },
            new AuditedEntity { Name = "c" },
        });

        Assert.Equal(2, affected);
        Assert.Equal(2, db.Table<AuditedEntity>().Count());
        Assert.DoesNotContain(db.Table<AuditedEntity>().ToList(), e => e.Name == "skip");
    }

    [Fact]
    public void OnUpdateRange_HookFiresPerRow_CountsAllRows()
    {
        int hookCount = 0;
        using TestDatabase db = new(b => b.OnUpdate<AuditedEntity>(_ => hookCount++));
        db.Table<AuditedEntity>().Schema.CreateTable();
        db.Table<AuditedEntity>().AddRange(new[]
        {
            new AuditedEntity { Name = "a" },
            new AuditedEntity { Name = "b" },
            new AuditedEntity { Name = "c" },
        });

        List<AuditedEntity> rows = db.Table<AuditedEntity>().ToList();
        foreach (AuditedEntity row in rows)
        {
            row.Name += "-updated";
        }

        int affected = db.Table<AuditedEntity>().UpdateRange(rows);

        Assert.Equal(3, affected);
        Assert.Equal(3, hookCount);
        Assert.All(db.Table<AuditedEntity>().ToList(), e => Assert.EndsWith("-updated", e.Name));
    }

    [Fact]
    public void OnUpdateRange_OneHookCancelsOneRow_OthersUpdate()
    {
        using TestDatabase db = new(b => b.OnUpdate<AuditedEntity>((_, e) => e.Name != "skip-update"));
        db.Table<AuditedEntity>().Schema.CreateTable();
        db.Table<AuditedEntity>().AddRange(new[]
        {
            new AuditedEntity { Name = "a" },
            new AuditedEntity { Name = "b" },
            new AuditedEntity { Name = "c" },
        });

        List<AuditedEntity> rows = db.Table<AuditedEntity>().ToList();
        rows[1].Name = "skip-update";
        rows[0].Name = "a-updated";
        rows[2].Name = "c-updated";

        int affected = db.Table<AuditedEntity>().UpdateRange(rows);

        Assert.Equal(2, affected);
        List<string> stored = db.Table<AuditedEntity>().Select(e => e.Name).ToList();
        Assert.Contains("a-updated", stored);
        Assert.Contains("c-updated", stored);
        Assert.Contains("b", stored);
    }

    [Fact]
    public void OnRemoveRange_HookFiresPerRow_CountsAllRows()
    {
        int hookCount = 0;
        using TestDatabase db = new(b => b.OnRemove<AuditedEntity>(_ => hookCount++));
        db.Table<AuditedEntity>().Schema.CreateTable();
        db.Table<AuditedEntity>().AddRange(new[]
        {
            new AuditedEntity { Name = "a" },
            new AuditedEntity { Name = "b" },
            new AuditedEntity { Name = "c" },
        });

        List<AuditedEntity> rows = db.Table<AuditedEntity>().ToList();
        int affected = db.Table<AuditedEntity>().RemoveRange(rows);

        Assert.Equal(3, affected);
        Assert.Equal(3, hookCount);
        Assert.Empty(db.Table<AuditedEntity>().ToList());
    }

    [Fact]
    public void OnRemoveRange_OneHookCancelsOneRow_OthersRemove()
    {
        using TestDatabase db = new(b => b.OnRemove<AuditedEntity>((_, e) => e.Name != "keep"));
        db.Table<AuditedEntity>().Schema.CreateTable();
        db.Table<AuditedEntity>().AddRange(new[]
        {
            new AuditedEntity { Name = "a" },
            new AuditedEntity { Name = "keep" },
            new AuditedEntity { Name = "c" },
        });

        List<AuditedEntity> rows = db.Table<AuditedEntity>().ToList();
        int affected = db.Table<AuditedEntity>().RemoveRange(rows);

        Assert.Equal(2, affected);
        AuditedEntity remaining = Assert.Single(db.Table<AuditedEntity>().ToList());
        Assert.Equal("keep", remaining.Name);
    }

    [Fact]
    public void OnAddOrUpdateRange_HookFiresPerRow_CountsAllRows()
    {
        int hookCount = 0;
        using TestDatabase db = new(b => b.OnAddOrUpdate<AuditedEntity>(_ => hookCount++));
        db.Table<AuditedEntity>().Schema.CreateTable();

        int affected = db.Table<AuditedEntity>().AddOrUpdateRange(new[]
        {
            new AuditedEntity { Name = "a" },
            new AuditedEntity { Name = "b" },
            new AuditedEntity { Name = "c" },
        });

        Assert.Equal(3, affected);
        Assert.Equal(3, hookCount);
        Assert.Equal(3, db.Table<AuditedEntity>().Count());
    }

    [Fact]
    public void OnAddOrUpdateRange_OneHookCancelsOneRow_OthersExecute()
    {
        using TestDatabase db = new(b => b.OnAddOrUpdate<AuditedEntity>((_, e) => e.Name != "skip"));
        db.Table<AuditedEntity>().Schema.CreateTable();

        int affected = db.Table<AuditedEntity>().AddOrUpdateRange(new[]
        {
            new AuditedEntity { Name = "a" },
            new AuditedEntity { Name = "skip" },
            new AuditedEntity { Name = "c" },
        });

        Assert.Equal(2, affected);
        Assert.Equal(2, db.Table<AuditedEntity>().Count());
        Assert.DoesNotContain(db.Table<AuditedEntity>().ToList(), e => e.Name == "skip");
    }

    [Fact]
    public void OnAdd_HookOnDifferentEntity_DoesNotFire()
    {
        int auditedHookCount = 0;
        using TestDatabase db = new(b => b.OnAdd<Author>(_ => auditedHookCount++));
        db.Table<AuditedEntity>().Schema.CreateTable();

        db.Table<AuditedEntity>().Add(new AuditedEntity { Name = "a" });

        Assert.Equal(0, auditedHookCount);
    }

    [Fact]
    public void Hooks_FireBeforeSubclassOverrides()
    {
        DateTime stamp = new(2026, 3, 3, 0, 0, 0, DateTimeKind.Utc);
        using TestDatabase db = new(b => b.OnAdd<AuditedEntity>(e => e.CreatedAt = stamp));
        db.Table<AuditedEntity>().Schema.CreateTable();

        db.Table<AuditedEntity>().Add(new AuditedEntity { Name = "a" });

        Assert.Equal(stamp, db.Table<AuditedEntity>().Single().CreatedAt);
    }
}
