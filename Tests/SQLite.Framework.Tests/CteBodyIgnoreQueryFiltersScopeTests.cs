using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework;
using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("CteFilterScope")]
public class CteFilterScopeRow
{
    [Key]
    public int Id { get; set; }

    public bool IsDeleted { get; set; }
}

public class CteBodyIgnoreQueryFiltersScopeTests
{
    [Fact]
    public void IgnoreQueryFiltersInsideCteBodyDropsFiltersEverywhere()
    {
        using TestDatabase db = new(b => b.AddQueryFilter<CteFilterScopeRow>(s => !s.IsDeleted));
        db.Table<CteFilterScopeRow>().Schema.CreateTable();
        db.Table<CteFilterScopeRow>().Add(new CteFilterScopeRow { Id = 1, IsDeleted = false });
        db.Table<CteFilterScopeRow>().Add(new CteFilterScopeRow { Id = 2, IsDeleted = true });

        SQLiteCte<CteFilterScopeRow> cte = db.With(() => db.Table<CteFilterScopeRow>().IgnoreQueryFilters());
        List<int> actual = (
            from c in cte
            join m in db.Table<CteFilterScopeRow>() on c.Id equals m.Id
            select m.Id).ToList().OrderBy(i => i).ToList();

        Assert.Equal([1, 2], actual);
    }

    [Fact]
    public void OuterIgnoreQueryFiltersAlsoDropsFiltersInsideCteBody()
    {
        using TestDatabase db = new(b => b.AddQueryFilter<CteFilterScopeRow>(s => !s.IsDeleted));
        db.Table<CteFilterScopeRow>().Schema.CreateTable();
        db.Table<CteFilterScopeRow>().Add(new CteFilterScopeRow { Id = 1, IsDeleted = false });
        db.Table<CteFilterScopeRow>().Add(new CteFilterScopeRow { Id = 2, IsDeleted = true });

        SQLiteCte<CteFilterScopeRow> cte = db.With(() => db.Table<CteFilterScopeRow>());
        List<int> actual = (
            from c in cte
            join m in db.Table<CteFilterScopeRow>().IgnoreQueryFilters() on c.Id equals m.Id
            select m.Id).ToList().OrderBy(i => i).ToList();

        Assert.Equal([1, 2], actual);
    }

    [Fact]
    public void SameCteTwiceKeepsFilters()
    {
        using TestDatabase db = new(b => b.AddQueryFilter<CteFilterScopeRow>(s => !s.IsDeleted));
        db.Table<CteFilterScopeRow>().Schema.CreateTable();
        db.Table<CteFilterScopeRow>().Add(new CteFilterScopeRow { Id = 1, IsDeleted = false });
        db.Table<CteFilterScopeRow>().Add(new CteFilterScopeRow { Id = 2, IsDeleted = true });

        SQLiteCte<CteFilterScopeRow> cte = db.With(() => db.Table<CteFilterScopeRow>());
        List<int> actual = (
            from a in cte
            join b in cte on a.Id equals b.Id
            select a.Id).ToList().OrderBy(i => i).ToList();

        Assert.Equal([1], actual);
    }

    [Fact]
    public void OuterFirstIgnoreQueryFiltersAlsoDropsFiltersInsideCteBody()
    {
        using TestDatabase db = new(b => b.AddQueryFilter<CteFilterScopeRow>(s => !s.IsDeleted));
        db.Table<CteFilterScopeRow>().Schema.CreateTable();
        db.Table<CteFilterScopeRow>().Add(new CteFilterScopeRow { Id = 1, IsDeleted = false });
        db.Table<CteFilterScopeRow>().Add(new CteFilterScopeRow { Id = 2, IsDeleted = true });

        SQLiteCte<CteFilterScopeRow> cte = db.With(() => db.Table<CteFilterScopeRow>());
        List<int> actual = (
            from m in db.Table<CteFilterScopeRow>().IgnoreQueryFilters()
            join c in cte on m.Id equals c.Id
            select m.Id).ToList().OrderBy(i => i).ToList();

        Assert.Equal([1, 2], actual);
    }
}
