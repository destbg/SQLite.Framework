using System.ComponentModel.DataAnnotations;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class NullableTimeOnlySubtractionWrapParityTests
{
    public class NtoRow
    {
        [Key]
        public int Id { get; set; }
        public TimeOnly? Time { get; set; }
    }

    [Fact]
    public void NullableTimeOnlyMinusTimeOnly_WrapsToDay_MatchesLinqToObjects()
    {
        using TestDatabase db = new();
        db.Table<NtoRow>().Schema.CreateTable();
        db.Table<NtoRow>().Add(new NtoRow { Id = 1, Time = new TimeOnly(1, 0, 0) });

        TimeSpan? oracle = new NtoRow { Id = 1, Time = new TimeOnly(1, 0, 0) }.Time - new TimeOnly(3, 0, 0);
        TimeSpan? actual = db.Table<NtoRow>()
            .Where(x => x.Id == 1)
            .Select(x => x.Time - new TimeOnly(3, 0, 0))
            .First();

        Assert.Equal(oracle, actual);
    }
}
