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
        db.Table<AuditedEntity>().CreateTable();

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
        db.Table<AuditedEntity>().CreateTable();

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
        db.Table<AuditedEntity>().CreateTable();

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
        db.Table<AuditedEntity>().CreateTable();

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
        db.Table<AuditedEntity>().CreateTable();
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
        db.Table<AuditedEntity>().CreateTable();
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
        db.Table<AuditedEntity>().CreateTable();

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
        db.Table<AuditedEntity>().CreateTable();

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
    public void OnAdd_HookOnDifferentEntity_DoesNotFire()
    {
        int auditedHookCount = 0;
        using TestDatabase db = new(b => b.OnAdd<Author>(_ => auditedHookCount++));
        db.Table<AuditedEntity>().CreateTable();

        db.Table<AuditedEntity>().Add(new AuditedEntity { Name = "a" });

        Assert.Equal(0, auditedHookCount);
    }

    [Fact]
    public void Hooks_FireBeforeSubclassOverrides()
    {
        // Sanity check that an OnAdd hook is observable before the row hits AddOrRemoveItem.
        // We capture the entity's state in the hook and check the same state was persisted.
        DateTime stamp = new(2026, 3, 3, 0, 0, 0, DateTimeKind.Utc);
        using TestDatabase db = new(b => b.OnAdd<AuditedEntity>(e => e.CreatedAt = stamp));
        db.Table<AuditedEntity>().CreateTable();

        db.Table<AuditedEntity>().Add(new AuditedEntity { Name = "a" });

        Assert.Equal(stamp, db.Table<AuditedEntity>().Single().CreatedAt);
    }
}
