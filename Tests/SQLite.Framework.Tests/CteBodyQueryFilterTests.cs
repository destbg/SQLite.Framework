using SQLite.Framework;
using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class CteBodyQueryFilterTests
{
    private static TestDatabase SetupDatabase()
    {
        TestDatabase db = new(b => b.AddQueryFilter<SoftDeletableBook>(s => !s.IsDeleted));
        db.Table<SoftDeletableBook>().Schema.CreateTable();
        db.Table<SoftDeletableBook>().Add(new SoftDeletableBook { Id = 1, Title = "live", IsDeleted = false });
        db.Table<SoftDeletableBook>().Add(new SoftDeletableBook { Id = 2, Title = "gone", IsDeleted = true });
        return db;
    }

    [Fact]
    public void QueryFilterAppliesInsideCteBody()
    {
        using TestDatabase db = SetupDatabase();

        SoftDeletableBook[] seed =
        [
            new SoftDeletableBook { Id = 1, Title = "live", IsDeleted = false },
            new SoftDeletableBook { Id = 2, Title = "gone", IsDeleted = true },
        ];
        List<int> oracle = seed.Where(s => !s.IsDeleted).Where(s => s.Id > 0).Select(s => s.Id).OrderBy(i => i).ToList();

        Assert.Equal([1], oracle);

        SQLiteCte<SoftDeletableBook> cte = db.With(() => db.Table<SoftDeletableBook>().Where(b => b.Id > 0));
        List<int> actual = (from b in cte select b.Id).OrderBy(i => i).ToList();

        Assert.Equal(oracle, actual);
    }
}
