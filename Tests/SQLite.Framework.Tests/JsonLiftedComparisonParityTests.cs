using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class JsonLiftedComparisonParityTests
{
    private static readonly List<int?> Values = [3, null, 10];

    private static TestDatabase CreateDb()
    {
        TestDatabase db = new(b => b.TypeConverters[typeof(List<int?>)] = new SQLiteJsonConverter<List<int?>>(TestJsonContext.Default.ListNullableInt32));
        db.Table<JlcRow>().Schema.CreateTable();
        db.Table<JlcRow>().Add(new JlcRow { Id = 1, Numbers = Values });
        return db;
    }

    [Fact]
    public void OrderByLiftedNullableComparison_MatchesLinqToObjects()
    {
        using TestDatabase db = CreateDb();

        int? oracle = Values.OrderBy(x => x > 5).First();
        int? actual = db.Table<JlcRow>().Select(r => r.Numbers.OrderBy(x => x > 5).First()).First();

        Assert.Equal(oracle, actual);
    }

    [Fact]
    public void GroupByLiftedNullableComparison_MatchesLinqToObjects()
    {
        using TestDatabase db = CreateDb();

        int oracle = Values.GroupBy(x => x > 5).Count();
        int actual = db.Table<JlcRow>().Select(r => r.Numbers.GroupBy(x => x > 5).Count()).First();

        Assert.Equal(oracle, actual);
    }
}

[Table("JlcRow")]
public class JlcRow
{
    [Key]
    public int Id { get; set; }

    public List<int?> Numbers { get; set; } = [];
}
