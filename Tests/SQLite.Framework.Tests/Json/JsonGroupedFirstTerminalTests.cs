using SQLite.Framework;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class JsonGroupedFirstTerminalTests
{
    [Fact]
    public void TheFirstGroupKeyOfAJsonListMatchesLinq()
    {
        using TestDatabase db = Setup(nameof(TheFirstGroupKeyOfAJsonListMatchesLinq));

        int expected = new List<int> { 7, 8, 7 }
            .GroupBy(n => n)
            .Select(g => g.Key)
            .First();

        int actual = db.Table<CarryJsonDoc>()
            .Where(d => d.Id == 1)
            .Select(d => d.Numbers.GroupBy(n => n).Select(g => g.Key).First())
            .First();

        Assert.Equal(expected, actual);
    }

    private static TestDatabase Setup(string methodName)
    {
        TestDatabase db = new(
            b => b.TypeConverters[typeof(List<int>)] = new SQLiteJsonConverter<List<int>>(CarryJsonContext.Default.ListInt32),
            methodName);
        db.Table<CarryJsonDoc>().Schema.CreateTable();
        db.Table<CarryJsonDoc>().Add(new CarryJsonDoc { Id = 1, Numbers = [7, 8, 7] });
        return db;
    }
}
