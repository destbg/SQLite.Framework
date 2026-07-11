using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using SQLite.Framework.Attributes;
using SQLite.Framework.Enums;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("cascade_parent")]
public class CascadeParent
{
    [Key]
    public int Id { get; set; }

    public required string Name { get; set; }
}

[Table("cascade_child")]
public class CascadeChild
{
    [Key]
    public int Id { get; set; }

    [ReferencesTable(typeof(CascadeParent), OnDelete = SQLiteForeignKeyAction.Cascade)]
    public int ParentId { get; set; }

    public required string Tag { get; set; }
}

public class AddOrUpdateReplaceCascadeChildRowsTests
{
    private static TestDatabase Seed()
    {
        TestDatabase db = new();
        db.Table<CascadeParent>().Schema.CreateTable();
        db.Table<CascadeChild>().Schema.CreateTable();

        db.Table<CascadeParent>().Add(new CascadeParent { Id = 1, Name = "original" });
        db.Table<CascadeChild>().AddRange(new[]
        {
            new CascadeChild { Id = 1, ParentId = 1, Tag = "a" },
            new CascadeChild { Id = 2, ParentId = 1, Tag = "b" },
        });
        return db;
    }

    [Fact]
    public void AddOrUpdateExistingParentReplacesTheRowAndCascadesToChildRows()
    {
        using TestDatabase db = Seed();

        db.Table<CascadeParent>().AddOrUpdate(new CascadeParent { Id = 1, Name = "renamed" });

        Assert.Equal("renamed", db.Table<CascadeParent>().Single().Name);
        Assert.Equal(0, db.Table<CascadeChild>().Count());
    }

    [Fact]
    public void UpdateExistingParentKeepsChildRows()
    {
        using TestDatabase db = Seed();

        db.Table<CascadeParent>().Update(new CascadeParent { Id = 1, Name = "renamed" });

        List<(int, string)> children = db.Table<CascadeChild>()
            .OrderBy(c => c.Id).AsEnumerable().Select(c => (c.Id, c.Tag)).ToList();

        Assert.Equal("renamed", db.Table<CascadeParent>().Single().Name);
        Assert.Equal(new List<(int, string)> { (1, "a"), (2, "b") }, children);
    }

    [Fact]
    public void UpsertExistingParentKeepsChildRows()
    {
        using TestDatabase db = Seed();

        db.Table<CascadeParent>().Upsert(
            new CascadeParent { Id = 1, Name = "renamed" },
            c => c.OnConflict(p => p.Id).DoUpdateAll());

        List<(int, string)> children = db.Table<CascadeChild>()
            .OrderBy(c => c.Id).AsEnumerable().Select(c => (c.Id, c.Tag)).ToList();

        Assert.Equal("renamed", db.Table<CascadeParent>().Single().Name);
        Assert.Equal(new List<(int, string)> { (1, "a"), (2, "b") }, children);
    }
}
