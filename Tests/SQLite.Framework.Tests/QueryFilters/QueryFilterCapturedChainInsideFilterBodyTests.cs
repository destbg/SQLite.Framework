using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H24oChainParents")]
public class H24oChainParent
{
    [Key]
    public int Id { get; set; }

    public string Name { get; set; } = "";
}

[Table("H24oChainChildren")]
public class H24oChainChild
{
    [Key]
    public int Id { get; set; }

    public int ParentId { get; set; }

    public int Score { get; set; }

    public bool IsDeleted { get; set; }
}

public class H24oChainHolder
{
    public IQueryable<H24oChainChild>? Chain { get; set; }
}

public class QueryFilterCapturedChainInsideFilterBodyTests
{
    [Fact]
    public void FilterBodyOverACapturedChainStillAppliesTheChainTableFilter()
    {
        H24oChainHolder holder = new();
        using TestDatabase db = new(b => b
            .AddQueryFilter<H24oChainChild>(c => !c.IsDeleted)
            .AddQueryFilter<H24oChainParent>(p => holder.Chain!.Any(c => c.ParentId == p.Id)));
        db.Table<H24oChainParent>().Schema.CreateTable();
        db.Table<H24oChainChild>().Schema.CreateTable();

        List<H24oChainParent> parents = Parents();
        List<H24oChainChild> children = Children();
        db.Table<H24oChainParent>().AddRange(parents);
        db.Table<H24oChainChild>().AddRange(children);

        holder.Chain = db.Table<H24oChainChild>().Where(c => c.Score > 0);

        List<H24oChainChild> visible = children.Where(c => !c.IsDeleted).Where(c => c.Score > 0).ToList();
        List<int> expected = parents
            .Where(p => visible.Any(c => c.ParentId == p.Id))
            .Select(p => p.Id)
            .OrderBy(i => i)
            .ToList();

        List<int> actual = db.Table<H24oChainParent>()
            .Select(p => p.Id)
            .ToList()
            .OrderBy(i => i)
            .ToList();

        Assert.Equal(expected, actual);
    }

    private static List<H24oChainParent> Parents()
    {
        return
        [
            new H24oChainParent { Id = 1, Name = "p1" },
            new H24oChainParent { Id = 2, Name = "p2" },
            new H24oChainParent { Id = 3, Name = "p3" }
        ];
    }

    private static List<H24oChainChild> Children()
    {
        return
        [
            new H24oChainChild { Id = 1, ParentId = 1, Score = 5, IsDeleted = false },
            new H24oChainChild { Id = 2, ParentId = 2, Score = 5, IsDeleted = true },
            new H24oChainChild { Id = 3, ParentId = 3, Score = 0, IsDeleted = false }
        ];
    }
}
