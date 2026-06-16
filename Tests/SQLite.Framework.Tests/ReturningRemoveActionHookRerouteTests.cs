using System.ComponentModel.DataAnnotations;
using SQLite.Framework.Enums;
using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

internal sealed class RerouteSoftDeleteRow
{
    [Key]
    public int Id { get; set; }

    public required string Name { get; set; }

    public bool IsDeleted { get; set; }
}

public class ReturningRemoveActionHookRerouteTests
{
    private static TestDatabase SetupDatabase()
    {
        TestDatabase db = new(b => b.OnAction((_, entity, action) =>
        {
            if (action == SQLiteAction.Remove && entity is RerouteSoftDeleteRow row)
            {
                row.IsDeleted = true;
                return SQLiteAction.Update;
            }

            return action;
        }));
        db.Table<RerouteSoftDeleteRow>().Schema.CreateTable();
        db.Table<RerouteSoftDeleteRow>().Add(new RerouteSoftDeleteRow { Id = 1, Name = "a", IsDeleted = false });
        return db;
    }

    [Fact]
    public void ReturningRemoveHonorsRerouteToUpdate()
    {
        using TestDatabase db = SetupDatabase();

        RerouteSoftDeleteRow row = db.Table<RerouteSoftDeleteRow>().Single();

        db.Table<RerouteSoftDeleteRow>().Returning().Remove(row);

        List<RerouteSoftDeleteRow> remaining = db.Table<RerouteSoftDeleteRow>().ToList();

        Assert.Single(remaining);
        Assert.True(remaining[0].IsDeleted);
    }
}
