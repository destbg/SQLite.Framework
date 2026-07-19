using System.Collections;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public interface IH20AttLeadChild
{
    int ParentId { get; }
}

[Table("H20AttLeadParent")]
public class H20AttLeadParent
{
    [Key]
    public int Id { get; set; }

    public string Name { get; set; } = "";
}

[Table("H20AttLeadChild")]
public class H20AttLeadChild : IH20AttLeadChild
{
    [Key]
    public int Id { get; set; }

    public int ParentId { get; set; }

    public bool IsDeleted { get; set; }
}

public class QueryFilterWiderQueryableCaptureTests
{
    private static (TestDatabase Db, H20AttLeadParent[] Parents, H20AttLeadChild[] Children) Seed()
    {
        TestDatabase db = new(b => b.AddQueryFilter<H20AttLeadChild>(c => !c.IsDeleted));
        db.Table<H20AttLeadParent>().Schema.CreateTable();
        db.Table<H20AttLeadChild>().Schema.CreateTable();
        H20AttLeadParent[] parents =
        [
            new() { Id = 1, Name = "p1" },
            new() { Id = 2, Name = "p2" },
            new() { Id = 3, Name = "p3" },
        ];
        H20AttLeadChild[] children =
        [
            new() { Id = 1, ParentId = 1, IsDeleted = false },
            new() { Id = 2, ParentId = 2, IsDeleted = true },
            new() { Id = 3, ParentId = 2, IsDeleted = false },
        ];
        db.Table<H20AttLeadParent>().AddRange(parents);
        db.Table<H20AttLeadChild>().AddRange(children);
        return (db, parents, children);
    }

    [Fact]
    public void NonGenericQueryableCapturedTableAppliesFilter()
    {
        (TestDatabase db, H20AttLeadParent[] parents, H20AttLeadChild[] children) = Seed();
        using (db)
        {
            H20AttLeadChild[] visible = children.Where(c => !c.IsDeleted).ToArray();
            List<int> expected = parents
                .Where(p => visible.Any(c => c.ParentId == p.Id))
                .Select(p => p.Id)
                .OrderBy(i => i)
                .ToList();

            IQueryable source = db.Table<H20AttLeadChild>();
            List<int> actual = db.Table<H20AttLeadParent>()
                .Where(p => source.Cast<H20AttLeadChild>().Any(c => c.ParentId == p.Id))
                .Select(p => p.Id)
                .OrderBy(i => i)
                .ToList();

            Assert.Equal(expected, actual);
        }
    }

    [Fact]
    public void CovariantObjectQueryableCapturedTableAppliesFilter()
    {
        (TestDatabase db, H20AttLeadParent[] parents, H20AttLeadChild[] children) = Seed();
        using (db)
        {
            H20AttLeadChild[] visible = children.Where(c => !c.IsDeleted).ToArray();
            List<int> expected = parents
                .Where(p => visible.Any(c => c.ParentId == p.Id))
                .Select(p => p.Id)
                .OrderBy(i => i)
                .ToList();

            IQueryable<object> source = db.Table<H20AttLeadChild>();
            List<int> actual = db.Table<H20AttLeadParent>()
                .Where(p => source.Cast<H20AttLeadChild>().Any(c => c.ParentId == p.Id))
                .Select(p => p.Id)
                .OrderBy(i => i)
                .ToList();

            Assert.Equal(expected, actual);
        }
    }

    [Fact]
    public void InterfaceQueryableCapturedTableAppliesFilter()
    {
        (TestDatabase db, H20AttLeadParent[] parents, H20AttLeadChild[] children) = Seed();
        using (db)
        {
            H20AttLeadChild[] visible = children.Where(c => !c.IsDeleted).ToArray();
            List<int> expected = parents
                .Where(p => visible.Any(c => c.ParentId == p.Id))
                .Select(p => p.Id)
                .OrderBy(i => i)
                .ToList();

            IQueryable<IH20AttLeadChild> source = db.Table<H20AttLeadChild>();
            List<int> actual = db.Table<H20AttLeadParent>()
                .Where(p => source.Any(c => c.ParentId == p.Id))
                .Select(p => p.Id)
                .OrderBy(i => i)
                .ToList();

            Assert.Equal(expected, actual);
        }
    }
}
