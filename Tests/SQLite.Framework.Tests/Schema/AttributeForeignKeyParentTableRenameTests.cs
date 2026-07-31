using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework;
using SQLite.Framework.Attributes;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H26jFkParents")]
public class H26jFkParent
{
    [Key]
    public int Id { get; set; }
}

[Table("H26jFkChildren")]
public class H26jFkChild
{
    [Key]
    public int Id { get; set; }

    [ReferencesTable(typeof(H26jFkParent))]
    public int ParentId { get; set; }
}

public sealed class H26jAttributeForeignKeyParentRenameDatabase : TestDatabase
{
    public H26jAttributeForeignKeyParentRenameDatabase()
        : base(useFile: true)
    {
    }

    protected override void OnModelCreating(SQLiteModelBuilder builder)
    {
        builder.Entity<H26jFkParent>().ToTable("H26jFkParentsRenamed");
    }
}

public class AttributeForeignKeyParentTableRenameTests
{
    [Fact]
    public void AnAttributeForeignKeyFollowsTheParentTableNameSetByTheModel()
    {
        using H26jAttributeForeignKeyParentRenameDatabase db = new();
        db.Schema.CreateTable<H26jFkParent>();
        db.Schema.CreateTable<H26jFkChild>();

        db.Table<H26jFkParent>().AddRange(ParentRows());
        db.Table<H26jFkChild>().AddRange(ChildRows());

        List<int> expected = ChildRows()
            .Select(c => c.ParentId)
            .OrderBy(v => v)
            .ToList();

        List<int> actual = db.Table<H26jFkChild>()
            .Select(c => c.ParentId)
            .AsEnumerable()
            .OrderBy(v => v)
            .ToList();

        Assert.Equal(expected, actual);
    }

    private static List<H26jFkParent> ParentRows()
    {
        return
        [
            new H26jFkParent { Id = 1 },
            new H26jFkParent { Id = 2 }
        ];
    }

    private static List<H26jFkChild> ChildRows()
    {
        return
        [
            new H26jFkChild { Id = 1, ParentId = 1 },
            new H26jFkChild { Id = 2, ParentId = 2 }
        ];
    }
}
