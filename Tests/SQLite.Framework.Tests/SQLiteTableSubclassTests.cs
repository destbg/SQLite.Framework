using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class SQLiteTableSubclassTests
{
    [Fact]
    public void Subclass_CreateTable_RoundTripsThroughCustomProperty()
    {
        using AuditingDatabase db = new();
        db.Items.CreateTable();

        db.Items.Add(new SubclassedTableEntity { Name = "first" });

        List<SubclassedTableEntity> all = db.Items.ToList();

        Assert.Single(all);
        Assert.Equal("first", all[0].Name);
    }

    [Fact]
    public void Subclass_AddOrRemoveItemOverride_FiresOnAdd()
    {
        using AuditingDatabase db = new();
        db.Items.CreateTable();

        db.Items.Add(new SubclassedTableEntity { Name = "one" });

        Assert.Equal(1, db.Items.AddCallCount);
    }

    [Fact]
    public void Subclass_AddOrRemoveItemOverride_FiresOncePerRowInAddRange()
    {
        using AuditingDatabase db = new();
        db.Items.CreateTable();

        db.Items.AddRange(new[]
        {
            new SubclassedTableEntity { Name = "a" },
            new SubclassedTableEntity { Name = "b" },
            new SubclassedTableEntity { Name = "c" },
        });

        Assert.Equal(3, db.Items.AddCallCount);
    }

    [Fact]
    public void Subclass_LinqQuery_MaterializesEntity()
    {
        using AuditingDatabase db = new();
        db.Items.CreateTable();

        db.Items.Add(new SubclassedTableEntity { Name = "alpha" });
        db.Items.Add(new SubclassedTableEntity { Name = "beta" });

        List<SubclassedTableEntity> matches = db.Items.Where(e => e.Name == "alpha").ToList();

        Assert.Single(matches);
        Assert.Equal("alpha", matches[0].Name);
    }

    [Fact]
    public void Subclass_RemoveAndUpdate_GoThroughOverride()
    {
        using AuditingDatabase db = new();
        db.Items.CreateTable();

        db.Items.Add(new SubclassedTableEntity { Name = "x" });
        SubclassedTableEntity row = db.Items.Single();

        row.Name = "renamed";
        db.Items.Update(row);

        Assert.Equal("renamed", db.Items.Single().Name);

        db.Items.Remove(row);
        Assert.Empty(db.Items.ToList());
    }
}
