using System.Linq.Expressions;
using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;
using SQLite.Framework.Tests.Interfaces;

namespace SQLite.Framework.Tests;

public class QueryFilterTests
{
    [Fact]
    public void TypedFilter_AppliesToWhereQuery()
    {
        using TestDatabase db = new(b => b.AddQueryFilter<SoftDeletableBook>(s => !s.IsDeleted));
        db.Table<SoftDeletableBook>().Schema.CreateTable();
        db.Table<SoftDeletableBook>().AddRange(new[]
        {
            new SoftDeletableBook { Id = 1, Title = "live", IsDeleted = false },
            new SoftDeletableBook { Id = 2, Title = "gone", IsDeleted = true },
        });

        List<SoftDeletableBook> rows = db.Table<SoftDeletableBook>().ToList();

        Assert.Single(rows);
        Assert.Equal("live", rows[0].Title);
    }

    [Fact]
    public void TypedFilter_ComposesWithUserWhere()
    {
        using TestDatabase db = new(b => b.AddQueryFilter<SoftDeletableBook>(s => !s.IsDeleted));
        db.Table<SoftDeletableBook>().Schema.CreateTable();
        db.Table<SoftDeletableBook>().AddRange(new[]
        {
            new SoftDeletableBook { Id = 1, Title = "live-a", IsDeleted = false },
            new SoftDeletableBook { Id = 2, Title = "live-b", IsDeleted = false },
            new SoftDeletableBook { Id = 3, Title = "gone-a", IsDeleted = true },
        });

        List<SoftDeletableBook> rows = db.Table<SoftDeletableBook>()
            .Where(b => b.Title.StartsWith("live"))
            .ToList();

        Assert.Equal(2, rows.Count);
        Assert.All(rows, r => Assert.False(r.IsDeleted));
    }

    [Fact]
    public void InterfaceFilter_AppliesToEveryEntityImplementingInterface()
    {
        using TestDatabase db = new(b => b.AddQueryFilter<ISoftDelete>(e => !e.IsDeleted));
        db.Table<SoftDeletableBook>().Schema.CreateTable();
        db.Table<AuditedEntity>().Schema.CreateTable();
        db.Table<SoftDeletableBook>().AddRange(new[]
        {
            new SoftDeletableBook { Id = 1, Title = "live", IsDeleted = false },
            new SoftDeletableBook { Id = 2, Title = "gone", IsDeleted = true },
        });
        db.Table<AuditedEntity>().Add(new AuditedEntity { Name = "live", IsDeleted = false });
        db.Table<AuditedEntity>().Add(new AuditedEntity { Name = "gone", IsDeleted = true });

        List<SoftDeletableBook> books = db.Table<SoftDeletableBook>().ToList();
        List<AuditedEntity> audited = db.Table<AuditedEntity>().ToList();

        Assert.Single(books);
        Assert.Equal("live", books[0].Title);
        Assert.Single(audited);
        Assert.Equal("live", audited[0].Name);
    }

    [Fact]
    public void IgnoreQueryFilters_DropsTheFilter()
    {
        using TestDatabase db = new(b => b.AddQueryFilter<SoftDeletableBook>(s => !s.IsDeleted));
        db.Table<SoftDeletableBook>().Schema.CreateTable();
        db.Table<SoftDeletableBook>().AddRange(new[]
        {
            new SoftDeletableBook { Id = 1, Title = "live", IsDeleted = false },
            new SoftDeletableBook { Id = 2, Title = "gone", IsDeleted = true },
        });

        List<SoftDeletableBook> rows = db.Table<SoftDeletableBook>().IgnoreQueryFilters().ToList();

        Assert.Equal(2, rows.Count);
    }

    [Fact]
    public void NoFilter_NoInjection()
    {
        using TestDatabase db = new();
        db.Table<SoftDeletableBook>().Schema.CreateTable();
        db.Table<SoftDeletableBook>().AddRange(new[]
        {
            new SoftDeletableBook { Id = 1, Title = "a", IsDeleted = false },
            new SoftDeletableBook { Id = 2, Title = "b", IsDeleted = true },
        });

        List<SoftDeletableBook> rows = db.Table<SoftDeletableBook>().ToList();

        Assert.Equal(2, rows.Count);
    }

    [Fact]
    public void Filter_AppliesToCount()
    {
        using TestDatabase db = new(b => b.AddQueryFilter<SoftDeletableBook>(s => !s.IsDeleted));
        db.Table<SoftDeletableBook>().Schema.CreateTable();
        db.Table<SoftDeletableBook>().AddRange(new[]
        {
            new SoftDeletableBook { Id = 1, Title = "a", IsDeleted = false },
            new SoftDeletableBook { Id = 2, Title = "b", IsDeleted = true },
            new SoftDeletableBook { Id = 3, Title = "c", IsDeleted = false },
        });

        Assert.Equal(2, db.Table<SoftDeletableBook>().Count());
    }

    [Fact]
    public void Filter_AppliesToExecuteDelete()
    {
        using TestDatabase db = new(b => b.AddQueryFilter<SoftDeletableBook>(s => !s.IsDeleted));
        db.Table<SoftDeletableBook>().Schema.CreateTable();
        db.Table<SoftDeletableBook>().AddRange(new[]
        {
            new SoftDeletableBook { Id = 1, Title = "a", IsDeleted = false },
            new SoftDeletableBook { Id = 2, Title = "b", IsDeleted = true },
        });

        int affected = db.Table<SoftDeletableBook>().ExecuteDelete();

        Assert.Equal(1, affected);
        List<SoftDeletableBook> remaining = db.Table<SoftDeletableBook>().IgnoreQueryFilters().ToList();
        Assert.Single(remaining);
        Assert.True(remaining[0].IsDeleted);
    }

    [Fact]
    public void Filter_AppliesToExecuteUpdate()
    {
        using TestDatabase db = new(b => b.AddQueryFilter<SoftDeletableBook>(s => !s.IsDeleted));
        db.Table<SoftDeletableBook>().Schema.CreateTable();
        db.Table<SoftDeletableBook>().AddRange(new[]
        {
            new SoftDeletableBook { Id = 1, Title = "a", IsDeleted = false },
            new SoftDeletableBook { Id = 2, Title = "b", IsDeleted = true },
        });

        int affected = db.Table<SoftDeletableBook>().ExecuteUpdate(s => s.Set(b => b.Title, "renamed"));

        Assert.Equal(1, affected);
        List<SoftDeletableBook> all = db.Table<SoftDeletableBook>().IgnoreQueryFilters().ToList();
        Assert.Equal("renamed", all.Single(b => b.Id == 1).Title);
        Assert.Equal("b", all.Single(b => b.Id == 2).Title);
    }

    [Fact]
    public void Filter_AppliesToJoinedTable()
    {
        using TestDatabase db = new(b => b.AddQueryFilter<SoftDeletableBook>(s => !s.IsDeleted));
        db.Table<SoftDeletableBook>().Schema.CreateTable();
        db.Table<Author>().Schema.CreateTable();
        db.Table<Author>().Add(new Author { Id = 1, Name = "n", Email = "e", BirthDate = default });
        db.Table<SoftDeletableBook>().AddRange(new[]
        {
            new SoftDeletableBook { Id = 1, Title = "live", IsDeleted = false },
            new SoftDeletableBook { Id = 2, Title = "gone", IsDeleted = true },
        });

        List<string> titles = db.Table<Author>()
            .Join(db.Table<SoftDeletableBook>(), _ => 1, _ => 1, (a, b) => b.Title)
            .ToList();

        Assert.Single(titles);
        Assert.Equal("live", titles[0]);
    }

    [Fact]
    public void IgnoreQueryFilters_NullSource_Throws()
    {
        IQueryable<SoftDeletableBook>? source = null;
        Assert.Throws<ArgumentNullException>(() => source!.IgnoreQueryFilters());
    }

    [Fact]
    public void MultipleFilters_AreAndCombined()
    {
        using TestDatabase db = new(b => b
            .AddQueryFilter<SoftDeletableBook>(s => !s.IsDeleted)
            .AddQueryFilter<SoftDeletableBook>(s => s.Title.Length > 1));
        db.Table<SoftDeletableBook>().Schema.CreateTable();
        db.Table<SoftDeletableBook>().AddRange(new[]
        {
            new SoftDeletableBook { Id = 1, Title = "a", IsDeleted = false },
            new SoftDeletableBook { Id = 2, Title = "ab", IsDeleted = false },
            new SoftDeletableBook { Id = 3, Title = "ab", IsDeleted = true },
        });

        List<SoftDeletableBook> rows = db.Table<SoftDeletableBook>().ToList();

        Assert.Single(rows);
        Assert.Equal(2, rows[0].Id);
    }
}
