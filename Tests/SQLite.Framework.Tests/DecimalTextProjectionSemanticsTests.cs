using SQLite.Framework.Enums;
using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class DecimalTextProjectionSemanticsTests
{
    private static TestDatabase CreateDb()
    {
        TestDatabase db = new(b => b.UseDecimalStorage(DecimalStorageMode.Text));
        db.Table<NumericType>().Schema.CreateTable();
        db.Table<NumericType>().Add(new NumericType { Id = 1, DecimalValue = 10.0m });
        db.Table<NumericType>().Add(new NumericType { Id = 2, DecimalValue = 10.00m });
        return db;
    }

    [Fact]
    public void Distinct_TextStoredDecimal_DedupsByStoredText()
    {
        using TestDatabase db = CreateDb();

        List<decimal> actual = db.Table<NumericType>().OrderBy(x => x.Id).Select(x => x.DecimalValue).Distinct().ToList();

        Assert.Equal(2, actual.Count);
    }

    [Fact]
    public void SubqueryContains_TextStoredDecimal_ComparesStoredText()
    {
        using TestDatabase db = CreateDb();

        bool actual = db.Table<NumericType>()
            .Where(x => x.Id == 1)
            .Select(x => db.Table<NumericType>().Where(o => o.Id == 2).Select(o => o.DecimalValue).Contains(x.DecimalValue))
            .First();

        Assert.False(actual);
    }
}
