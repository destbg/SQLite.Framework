using SQLite.Framework.Enums;
using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;
using SQLite.Framework.Tests.Interfaces;

namespace SQLite.Framework.Tests;

public class OnActionTests
{
    [Fact]
    public void OnAction_FiresForAdd_WithStartingActionAdd()
    {
        SQLiteAction? seen = null;
        using TestDatabase db = new(b => b.OnAction((_, _, action) =>
        {
            seen = action;
            return action;
        }));
        db.Table<AuditedEntity>().Schema.CreateTable();

        db.Table<AuditedEntity>().Add(new AuditedEntity { Name = "x" });

        Assert.Equal(SQLiteAction.Add, seen);
    }

    [Fact]
    public void OnAction_FiresForUpdate_WithStartingActionUpdate()
    {
        SQLiteAction? seen = null;
        using TestDatabase db = new(b => b.OnAction((_, _, action) =>
        {
            seen = action;
            return action;
        }));
        db.Table<AuditedEntity>().Schema.CreateTable();
        db.Table<AuditedEntity>().Add(new AuditedEntity { Name = "x" });
        seen = null;

        AuditedEntity row = db.Table<AuditedEntity>().Single();
        row.Name = "renamed";
        db.Table<AuditedEntity>().Update(row);

        Assert.Equal(SQLiteAction.Update, seen);
    }

    [Fact]
    public void OnAction_FiresForRemove_WithStartingActionRemove()
    {
        SQLiteAction? seen = null;
        using TestDatabase db = new(b => b.OnAction((_, _, action) =>
        {
            seen = action;
            return action;
        }));
        db.Table<AuditedEntity>().Schema.CreateTable();
        db.Table<AuditedEntity>().Add(new AuditedEntity { Name = "x" });
        seen = null;

        AuditedEntity row = db.Table<AuditedEntity>().Single();
        db.Table<AuditedEntity>().Remove(row);

        Assert.Equal(SQLiteAction.Remove, seen);
    }

    [Fact]
    public void OnAction_FiresForAddOrUpdate_WithStartingActionAddOrUpdate()
    {
        SQLiteAction? seen = null;
        using TestDatabase db = new(b => b.OnAction((_, _, action) =>
        {
            seen = action;
            return action;
        }));
        db.Table<AuditedEntity>().Schema.CreateTable();

        db.Table<AuditedEntity>().AddOrUpdate(new AuditedEntity { Name = "x" });

        Assert.Equal(SQLiteAction.AddOrUpdate, seen);
    }

    [Fact]
    public void OnAction_FiresForUpsert_WithStartingActionAddOrUpdate()
    {
        SQLiteAction? seen = null;
        using TestDatabase db = new(b => b.OnAction((_, _, action) =>
        {
            seen = action;
            return action;
        }));
        db.Table<Book>().Schema.CreateTable();

        db.Table<Book>().Upsert(
            new Book { Id = 1, Title = "x", AuthorId = 1, Price = 1 },
            c => c.OnConflict(b => b.Id).DoNothing());

        Assert.Equal(SQLiteAction.AddOrUpdate, seen);
    }

    [Fact]
    public void OnAction_RewritesRemoveToUpdate_ImplementsSoftDeleteViaInterface()
    {
        using TestDatabase db = new(b => b.OnAction((_, entity, action) =>
        {
            if (action == SQLiteAction.Remove && entity is ISoftDelete soft)
            {
                soft.IsDeleted = true;
                return SQLiteAction.Update;
            }
            return action;
        }));
        db.Table<AuditedEntity>().Schema.CreateTable();
        db.Table<AuditedEntity>().Add(new AuditedEntity { Name = "still here" });

        AuditedEntity row = db.Table<AuditedEntity>().Single();
        int affected = db.Table<AuditedEntity>().Remove(row);

        Assert.Equal(1, affected);
        AuditedEntity stored = db.Table<AuditedEntity>().Single();
        Assert.True(stored.IsDeleted);
        Assert.Equal("still here", stored.Name);
    }

    [Fact]
    public void OnAction_ReturnsSkip_NoSqlIssued()
    {
        using TestDatabase db = new(b => b.OnAction((_, _, _) => SQLiteAction.Skip));
        db.Table<AuditedEntity>().Schema.CreateTable();

        int affected = db.Table<AuditedEntity>().Add(new AuditedEntity { Name = "x" });

        Assert.Equal(0, affected);
        Assert.Empty(db.Table<AuditedEntity>().ToList());
    }

    [Fact]
    public void OnAction_RewritesAddToAddOrUpdate_ReplacesExistingRow()
    {
        using TestDatabase db = new(b => b.OnAction((_, _, action) =>
            action == SQLiteAction.Add ? SQLiteAction.AddOrUpdate : action));
        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().Add(new Book { Id = 1, Title = "first", AuthorId = 1, Price = 1 });

        db.Table<Book>().Add(new Book { Id = 1, Title = "second", AuthorId = 2, Price = 2 });

        Book stored = db.Table<Book>().Single();
        Assert.Equal("second", stored.Title);
    }

    [Fact]
    public void OnAction_CanMutateEntity_BeforeWrite()
    {
        using TestDatabase db = new(b => b.OnAction((_, entity, action) =>
        {
            if (action == SQLiteAction.Add && entity is AuditedEntity audited)
            {
                audited.Name = "rewritten";
            }
            return action;
        }));
        db.Table<AuditedEntity>().Schema.CreateTable();

        db.Table<AuditedEntity>().Add(new AuditedEntity { Name = "original" });

        Assert.Equal("rewritten", db.Table<AuditedEntity>().Single().Name);
    }

    [Fact]
    public void OnAction_MultipleHooksChain_LastWins()
    {
        List<SQLiteAction> seen = [];
        using TestDatabase db = new(b => b
            .OnAction((_, _, action) =>
            {
                seen.Add(action);
                return SQLiteAction.Skip;
            })
            .OnAction((_, _, action) =>
            {
                seen.Add(action);
                return SQLiteAction.Add;
            }));
        db.Table<AuditedEntity>().Schema.CreateTable();

        int affected = db.Table<AuditedEntity>().Add(new AuditedEntity { Name = "x" });

        Assert.Equal(1, affected);
        Assert.Equal([SQLiteAction.Add, SQLiteAction.Skip], seen);
    }

    [Fact]
    public void OnAction_ReceivesDatabaseInstance()
    {
        SQLiteDatabase? captured = null;
        using TestDatabase db = new(b => b.OnAction((database, _, action) =>
        {
            captured = database;
            return action;
        }));
        db.Table<AuditedEntity>().Schema.CreateTable();

        db.Table<AuditedEntity>().Add(new AuditedEntity { Name = "x" });

        Assert.Same(db, captured);
    }

    [Fact]
    public void OnActionRange_FiresPerRow()
    {
        int hookCount = 0;
        using TestDatabase db = new(b => b.OnAction((_, _, action) =>
        {
            hookCount++;
            return action;
        }));
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
    public void OnActionRange_SkipsRowsThatReturnSkip()
    {
        using TestDatabase db = new(b => b.OnAction((_, entity, action) =>
        {
            if (entity is AuditedEntity { Name: "skip" }) return SQLiteAction.Skip;
            return action;
        }));
        db.Table<AuditedEntity>().Schema.CreateTable();

        int affected = db.Table<AuditedEntity>().AddRange(new[]
        {
            new AuditedEntity { Name = "a" },
            new AuditedEntity { Name = "skip" },
            new AuditedEntity { Name = "c" },
        });

        Assert.Equal(2, affected);
        Assert.DoesNotContain(db.Table<AuditedEntity>().ToList(), e => e.Name == "skip");
    }

    [Fact]
    public void OnActionRange_RewritesRemoveToUpdate_PerRow()
    {
        using TestDatabase db = new(b => b.OnAction((_, entity, action) =>
        {
            if (action == SQLiteAction.Remove && entity is ISoftDelete soft)
            {
                soft.IsDeleted = true;
                return SQLiteAction.Update;
            }
            return action;
        }));
        db.Table<AuditedEntity>().Schema.CreateTable();
        db.Table<AuditedEntity>().AddRange(new[]
        {
            new AuditedEntity { Name = "a" },
            new AuditedEntity { Name = "b" },
        });

        List<AuditedEntity> rows = db.Table<AuditedEntity>().ToList();
        int affected = db.Table<AuditedEntity>().RemoveRange(rows);

        Assert.Equal(2, affected);
        Assert.Equal(2, db.Table<AuditedEntity>().Count());
        Assert.All(db.Table<AuditedEntity>().ToList(), e => Assert.True(e.IsDeleted));
    }

    [Fact]
    public void OnAction_AddOrUpdateRewrittenToAdd_UsesPlainInsert()
    {
        using TestDatabase db = new(b => b.OnAction((_, _, action) =>
            action == SQLiteAction.AddOrUpdate ? SQLiteAction.Add : action));
        db.Table<Book>().Schema.CreateTable();

        db.Table<Book>().AddOrUpdate(new Book { Id = 1, Title = "first", AuthorId = 1, Price = 1 });

        Book stored = db.Table<Book>().Single();
        Assert.Equal("first", stored.Title);
    }

    [Fact]
    public void OnAction_UpsertRewrittenToSkip_NoSqlIssued()
    {
        using TestDatabase db = new(b => b.OnAction((_, _, _) => SQLiteAction.Skip));
        db.Table<Book>().Schema.CreateTable();

        int affected = db.Table<Book>().Upsert(
            new Book { Id = 1, Title = "x", AuthorId = 1, Price = 1 },
            c => c.OnConflict(b => b.Id).DoNothing());

        Assert.Equal(0, affected);
        Assert.Empty(db.Table<Book>().ToList());
    }

    [Fact]
    public void OnActionUpdateRange_FiresPerRow_DispatchesAction()
    {
        List<SQLiteAction> seen = [];
        using TestDatabase db = new(b => b.OnAction((_, _, action) =>
        {
            seen.Add(action);
            return action;
        }));
        db.Table<AuditedEntity>().Schema.CreateTable();
        db.Table<AuditedEntity>().AddRange(new[]
        {
            new AuditedEntity { Name = "a" },
            new AuditedEntity { Name = "b" },
        });
        seen.Clear();

        List<AuditedEntity> rows = db.Table<AuditedEntity>().ToList();
        rows[0].Name = "a-updated";
        rows[1].Name = "b-updated";
        int affected = db.Table<AuditedEntity>().UpdateRange(rows);

        Assert.Equal(2, affected);
        Assert.Equal([SQLiteAction.Update, SQLiteAction.Update], seen);
        Assert.All(db.Table<AuditedEntity>().ToList(), e => Assert.EndsWith("-updated", e.Name));
    }

    [Fact]
    public void OnActionUpdateRange_HookSkipsOneRow_OthersUpdate()
    {
        using TestDatabase db = new(b => b.OnAction((_, entity, action) =>
        {
            if (entity is AuditedEntity { Name: "skip-me" }) return SQLiteAction.Skip;
            return action;
        }));
        db.Table<AuditedEntity>().Schema.CreateTable();
        db.Table<AuditedEntity>().AddRange(new[]
        {
            new AuditedEntity { Name = "a" },
            new AuditedEntity { Name = "b" },
        });

        List<AuditedEntity> rows = db.Table<AuditedEntity>().ToList();
        rows[0].Name = "a-updated";
        rows[1].Name = "skip-me";
        int affected = db.Table<AuditedEntity>().UpdateRange(rows);

        Assert.Equal(1, affected);
        List<string> stored = db.Table<AuditedEntity>().Select(e => e.Name).ToList();
        Assert.Contains("a-updated", stored);
        Assert.Contains("b", stored);
    }

    [Fact]
    public void OnActionAddOrUpdateRange_DefaultBranch_KeepsAddOrUpdateAction()
    {
        List<SQLiteAction> seen = [];
        using TestDatabase db = new(b => b.OnAction((_, _, action) =>
        {
            seen.Add(action);
            return action;
        }));
        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().Add(new Book { Id = 1, Title = "original", AuthorId = 1, Price = 1 });
        seen.Clear();

        int affected = db.Table<Book>().AddOrUpdateRange(new[]
        {
            new Book { Id = 1, Title = "replaced", AuthorId = 1, Price = 2 },
            new Book { Id = 2, Title = "new", AuthorId = 1, Price = 3 },
        });

        Assert.Equal(2, affected);
        Assert.Equal([SQLiteAction.AddOrUpdate, SQLiteAction.AddOrUpdate], seen);
        Book replaced = db.Table<Book>().Single(b => b.Id == 1);
        Assert.Equal("replaced", replaced.Title);
    }

    [Fact]
    public void OnActionAddOrUpdateRange_RewritesOneRowToSkip_DispatchBranch()
    {
        using TestDatabase db = new(b => b.OnAction((_, entity, action) =>
        {
            if (entity is Book { Id: 2 }) return SQLiteAction.Skip;
            return action;
        }));
        db.Table<Book>().Schema.CreateTable();

        int affected = db.Table<Book>().AddOrUpdateRange(new[]
        {
            new Book { Id = 1, Title = "a", AuthorId = 1, Price = 1 },
            new Book { Id = 2, Title = "b", AuthorId = 1, Price = 2 },
            new Book { Id = 3, Title = "c", AuthorId = 1, Price = 3 },
        });

        Assert.Equal(2, affected);
        Assert.Equal(2, db.Table<Book>().Count());
        Assert.DoesNotContain(db.Table<Book>().ToList(), b => b.Id == 2);
    }

    [Fact]
    public void OnActionRange_Upsert_RoutesPerRow()
    {
        using TestDatabase db = new(b => b.OnAction((_, entity, action) =>
        {
            if (entity is Book { Id: 2 }) return SQLiteAction.Skip;
            return action;
        }));
        db.Table<Book>().Schema.CreateTable();

        int affected = db.Table<Book>().UpsertRange(new[]
        {
            new Book { Id = 1, Title = "a", AuthorId = 1, Price = 1 },
            new Book { Id = 2, Title = "b", AuthorId = 1, Price = 2 },
            new Book { Id = 3, Title = "c", AuthorId = 1, Price = 3 },
        }, c => c.OnConflict(b => b.Id).DoUpdateAll());

        Assert.Equal(2, affected);
        Assert.Equal(2, db.Table<Book>().Count());
        Assert.DoesNotContain(db.Table<Book>().ToList(), b => b.Id == 2);
    }

    [Fact]
    public void OnAction_PerEntityHookSkipsBeforeActionHook()
    {
        bool actionCalled = false;
        using TestDatabase db = new(b => b
            .OnAdd<AuditedEntity>((_, _) => false)
            .OnAction((_, _, action) =>
            {
                actionCalled = true;
                return action;
            }));
        db.Table<AuditedEntity>().Schema.CreateTable();

        int affected = db.Table<AuditedEntity>().Add(new AuditedEntity { Name = "x" });

        Assert.Equal(0, affected);
        Assert.False(actionCalled);
    }

    [Fact]
    public void OnAction_UnknownActionThrows()
    {
        using TestDatabase db = new(b => b.OnAction((_, _, _) => (SQLiteAction)999));
        db.Table<AuditedEntity>().Schema.CreateTable();

        Assert.Throws<InvalidOperationException>(() =>
            db.Table<AuditedEntity>().Add(new AuditedEntity { Name = "x" }));
    }
}
