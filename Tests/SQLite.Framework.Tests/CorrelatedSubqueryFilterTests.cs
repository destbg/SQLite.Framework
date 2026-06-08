using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("FilterParents")]
file sealed class FilterParent
{
    [Key]
    public int Id { get; set; }

    public required string Name { get; set; }
}

[Table("FilterChildren")]
file sealed class FilterChild
{
    [Key]
    public int Id { get; set; }

    public int ParentId { get; set; }

    public bool IsDeleted { get; set; }
}

public class CorrelatedSubqueryFilterTests
{
    [Fact]
    public void GlobalFilterAppliesInsideCorrelatedAnySubquery()
    {
        using TestDatabase db = new(b => b.AddQueryFilter<FilterChild>(c => !c.IsDeleted));
        db.Table<FilterParent>().Schema.CreateTable();
        db.Table<FilterChild>().Schema.CreateTable();

        FilterParent[] parents =
        [
            new FilterParent { Id = 1, Name = "p1" },
            new FilterParent { Id = 2, Name = "p2" },
        ];
        FilterChild[] children =
        [
            new FilterChild { Id = 1, ParentId = 1, IsDeleted = true },
            new FilterChild { Id = 2, ParentId = 2, IsDeleted = false },
        ];
        db.Table<FilterParent>().AddRange(parents);
        db.Table<FilterChild>().AddRange(children);

        List<FilterChild> visibleChildren = children.Where(c => !c.IsDeleted).ToList();
        List<string> oracle = parents
            .Where(p => visibleChildren.Any(c => c.ParentId == p.Id))
            .Select(p => p.Name)
            .OrderBy(x => x)
            .ToList();

        List<string> actual = db.Table<FilterParent>()
            .Where(p => db.Table<FilterChild>().Any(c => c.ParentId == p.Id))
            .Select(p => p.Name)
            .OrderBy(x => x)
            .ToList();

        Assert.Equal(["p2"], oracle);
        Assert.Equal(oracle, actual);
    }
}
