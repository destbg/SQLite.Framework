using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class InterpolatedStaticReceiverHoleRow
{
    [System.ComponentModel.DataAnnotations.Key]
    public int Id { get; set; }
    public int Value { get; set; }
}

public class InterpolatedStaticReceiverHoleProjectionParityTests
{
    private static TestDatabase Seed()
    {
        TestDatabase db = new();
        db.Table<InterpolatedStaticReceiverHoleRow>().Schema.CreateTable();
        db.Table<InterpolatedStaticReceiverHoleRow>().Add(new InterpolatedStaticReceiverHoleRow { Id = 1, Value = -5 });
        db.Table<InterpolatedStaticReceiverHoleRow>().Add(new InterpolatedStaticReceiverHoleRow { Id = 2, Value = 7 });
        return db;
    }

    [Fact]
    public void StaticMethodCallInsideInterpolationHole_MatchesLinqToObjects()
    {
        using TestDatabase db = Seed();

        List<string> oracle = db.Table<InterpolatedStaticReceiverHoleRow>().AsEnumerable()
            .OrderBy(p => p.Id)
            .Select(p => $"m={Math.Abs(p.Value)}")
            .ToList();
        List<string> actual = db.Table<InterpolatedStaticReceiverHoleRow>()
            .OrderBy(p => p.Id)
            .Select(p => $"m={Math.Abs(p.Value)}")
            .ToList();

        Assert.Equal(oracle, actual);
    }
}
