using System.Linq;
using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;
using SQLite.Framework.Tests.Interfaces;

namespace SQLite.Framework.Tests;

public class IgnoreQueryFiltersJoinScopeParityTests
{
    private static TestDatabase CreateDb()
    {
        TestDatabase db = new(b => b.AddQueryFilter<ISoftDelete>(e => !e.IsDeleted));
        db.Table<SoftDeletableBook>().Schema.CreateTable();
        db.Table<AuditedEntity>().Schema.CreateTable();
        db.Table<SoftDeletableBook>().Add(new SoftDeletableBook { Id = 1, Title = "b1", IsDeleted = false });
        db.Table<SoftDeletableBook>().Add(new SoftDeletableBook { Id = 2, Title = "b2", IsDeleted = true });
        db.Table<AuditedEntity>().Add(new AuditedEntity { Id = 1, Name = "a1", IsDeleted = false });
        db.Table<AuditedEntity>().Add(new AuditedEntity { Id = 2, Name = "a2", IsDeleted = true });
        return db;
    }

    [Fact]
    public void IgnoreQueryFilters_BeforeJoin_DropsJoinedTableFilterToo()
    {
        using TestDatabase db = CreateDb();

        List<string> wholeQuery = db.Table<SoftDeletableBook>()
            .Join(db.Table<AuditedEntity>(), b => b.Id, a => a.Id, (b, a) => a.Name)
            .IgnoreQueryFilters()
            .OrderBy(n => n)
            .ToList();

        List<string> beforeJoin = db.Table<SoftDeletableBook>()
            .IgnoreQueryFilters()
            .Join(db.Table<AuditedEntity>(), b => b.Id, a => a.Id, (b, a) => a.Name)
            .OrderBy(n => n)
            .ToList();

        Assert.Equal(wholeQuery, beforeJoin);
    }
}
