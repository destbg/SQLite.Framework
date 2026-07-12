using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("FqCaptureParent")]
public class FqCaptureParent
{
    [Key]
    public int Id { get; set; }

    public string Name { get; set; } = "";
}

[Table("FqCaptureChild")]
public class FqCaptureChild
{
    [Key]
    public int Id { get; set; }

    public int ParentId { get; set; }

    public string Title { get; set; } = "";

    public bool IsDeleted { get; set; }
}

public class QueryFilterCapturedQueryableSourceTests
{
    private static (TestDatabase Db, FqCaptureParent[] Parents, FqCaptureChild[] Children) Seed()
    {
        TestDatabase db = new(b => b.AddQueryFilter<FqCaptureChild>(c => !c.IsDeleted));
        db.Table<FqCaptureParent>().Schema.CreateTable();
        db.Table<FqCaptureChild>().Schema.CreateTable();

        FqCaptureParent[] parents =
        [
            new() { Id = 1, Name = "p1" },
            new() { Id = 2, Name = "p2" },
            new() { Id = 3, Name = "p3" },
        ];
        FqCaptureChild[] children =
        [
            new() { Id = 1, ParentId = 1, Title = "live-1", IsDeleted = false },
            new() { Id = 2, ParentId = 2, Title = "gone-2", IsDeleted = true },
            new() { Id = 3, ParentId = 2, Title = "gone-3", IsDeleted = true },
        ];
        db.Table<FqCaptureParent>().AddRange(parents);
        db.Table<FqCaptureChild>().AddRange(children);
        return (db, parents, children);
    }

    [Fact]
    public void AnySubqueryOverCapturedQueryableTypedTableAppliesFilter()
    {
        (TestDatabase db, FqCaptureParent[] parents, FqCaptureChild[] children) = Seed();
        using (db)
        {
            FqCaptureChild[] visible = children.Where(c => !c.IsDeleted).ToArray();
            List<int> expected = parents
                .Where(p => visible.Any(c => c.ParentId == p.Id))
                .Select(p => p.Id)
                .OrderBy(i => i)
                .ToList();

            IQueryable<FqCaptureChild> source = db.Table<FqCaptureChild>();
            List<int> actual = db.Table<FqCaptureParent>()
                .Where(p => source.Any(c => c.ParentId == p.Id))
                .Select(p => p.Id)
                .OrderBy(i => i)
                .ToList();

            Assert.Equal(expected, actual);
        }
    }

    [Fact]
    public void ContainsSubqueryOverCapturedQueryableTypedTableAppliesFilter()
    {
        (TestDatabase db, FqCaptureParent[] parents, FqCaptureChild[] children) = Seed();
        using (db)
        {
            FqCaptureChild[] visible = children.Where(c => !c.IsDeleted).ToArray();
            List<int> expected = parents
                .Where(p => visible.Select(c => c.ParentId).Contains(p.Id))
                .Select(p => p.Id)
                .OrderBy(i => i)
                .ToList();

            IQueryable<FqCaptureChild> source = db.Table<FqCaptureChild>();
            List<int> actual = db.Table<FqCaptureParent>()
                .Where(p => source.Select(c => c.ParentId).Contains(p.Id))
                .Select(p => p.Id)
                .OrderBy(i => i)
                .ToList();

            Assert.Equal(expected, actual);
        }
    }

    [Fact]
    public void CorrelatedCountOverCapturedQueryableTypedTableAppliesFilter()
    {
        (TestDatabase db, FqCaptureParent[] parents, FqCaptureChild[] children) = Seed();
        using (db)
        {
            FqCaptureChild[] visible = children.Where(c => !c.IsDeleted).ToArray();
            List<int> expected = parents
                .OrderBy(p => p.Id)
                .Select(p => visible.Count(c => c.ParentId == p.Id))
                .ToList();

            IQueryable<FqCaptureChild> source = db.Table<FqCaptureChild>();
            List<int> actual = db.Table<FqCaptureParent>()
                .OrderBy(p => p.Id)
                .Select(p => source.Count(c => c.ParentId == p.Id))
                .ToList();

            Assert.Equal(expected, actual);
        }
    }

}
