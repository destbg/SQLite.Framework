using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class GuidNewGuidProjectionTests
{
    [Fact]
    public void NewGuidInProjectionProducesADistinctValuePerRow()
    {
        using TestDatabase db = new();
        db.Table<NumericType>().Schema.CreateTable();
        for (int i = 1; i <= 3; i++)
        {
            db.Table<NumericType>().Add(new NumericType { Id = i });
        }

        int oracleDistinct = new[] { 1, 2, 3 }.Select(_ => Guid.NewGuid()).Distinct().Count();
        Assert.Equal(3, oracleDistinct);

        List<Guid> actual = db.Table<NumericType>().OrderBy(x => x.Id).Select(x => Guid.NewGuid()).ToList();

        Assert.Equal(3, actual.Distinct().Count());
    }
}
