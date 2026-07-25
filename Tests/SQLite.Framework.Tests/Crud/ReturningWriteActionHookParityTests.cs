using SQLite.Framework.Enums;
using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class ReturningWriteActionHookParityTests
{
    [Fact]
    public void ReturningRemoveHonorsSkipAction()
    {
        using TestDatabase db = new(b => b.OnAction((_, _, action) =>
            action == SQLiteAction.Remove ? SQLiteAction.Skip : action));
        db.Table<AuditedEntity>().Schema.CreateTable();
        db.Table<AuditedEntity>().Add(new AuditedEntity { Name = "a" });

        AuditedEntity row = db.Table<AuditedEntity>().Single();
        db.Table<AuditedEntity>().Returning().Remove(row);

        Assert.Equal(1, db.Table<AuditedEntity>().Count());
    }

    [Fact]
    public void ReturningAddHonorsSkipAction()
    {
        using TestDatabase db = new(b => b.OnAction((_, _, action) =>
            action == SQLiteAction.Add ? SQLiteAction.Skip : action));
        db.Table<AuditedEntity>().Schema.CreateTable();

        db.Table<AuditedEntity>().Returning().Add(new AuditedEntity { Name = "a" });

        Assert.Equal(0, db.Table<AuditedEntity>().Count());
    }
}
