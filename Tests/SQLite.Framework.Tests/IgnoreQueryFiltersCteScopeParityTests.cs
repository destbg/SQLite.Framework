using System.Linq;
using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class IgnoreQueryFiltersCteScopeParityTests
{
    private static TestDatabase Seed()
    {
        TestDatabase db = new(b => b.AddQueryFilter<SoftDeletableBook>(s => !s.IsDeleted));
        db.Table<SoftDeletableBook>().Schema.CreateTable();
        db.Table<SoftDeletableBook>().Add(new SoftDeletableBook { Id = 1, Title = "live", IsDeleted = false });
        db.Table<SoftDeletableBook>().Add(new SoftDeletableBook { Id = 2, Title = "gone", IsDeleted = true });
        return db;
    }

    [Fact]
    public void IgnoreQueryFiltersOverCte_DropsFilterEverywhere()
    {
        using TestDatabase db = Seed();

        List<int> oracle = [1, 2];

        SQLiteCte<SoftDeletableBook> cte = db.With(() => db.Table<SoftDeletableBook>());
        List<int> actual = (from b in cte select b.Id).IgnoreQueryFilters().OrderBy(i => i).ToList();

        Assert.Equal(oracle, actual);
    }

    [Fact]
    public void IgnoreQueryFiltersDirect_DropsFilter_Control()
    {
        using TestDatabase db = Seed();

        List<int> oracle = [1, 2];

        List<int> actual = db.Table<SoftDeletableBook>().IgnoreQueryFilters().Select(s => s.Id).OrderBy(i => i).ToList();

        Assert.Equal(oracle, actual);
    }
}
