using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class UlongEnumAggregateTests
{
    private static TestDatabase Seed()
    {
        TestDatabase db = new();
        db.Table<UlongEnumRow>().Schema.CreateTable();
        db.Table<UlongEnumRow>().Add(new UlongEnumRow { Id = 1, Value = SampleUlongEnum.Mid });
        db.Table<UlongEnumRow>().Add(new UlongEnumRow { Id = 2, Value = SampleUlongEnum.Max });
        db.Table<UlongEnumRow>().Add(new UlongEnumRow { Id = 3, Value = SampleUlongEnum.AboveLongMax });
        return db;
    }

    [Fact]
    public void Max_UlongBackedEnum_MixedSignedRange_MatchesDotNet()
    {
        using TestDatabase db = Seed();

        List<UlongEnumRow> seed =
        [
            new UlongEnumRow { Id = 1, Value = SampleUlongEnum.Mid },
            new UlongEnumRow { Id = 2, Value = SampleUlongEnum.Max },
            new UlongEnumRow { Id = 3, Value = SampleUlongEnum.AboveLongMax },
        ];

        SampleUlongEnum oracle = seed.Max(x => x.Value);
        SampleUlongEnum actual = db.Table<UlongEnumRow>().Max(x => x.Value);

        Assert.Equal(SampleUlongEnum.Max, oracle);
        Assert.Equal(oracle, actual);
    }

    [Fact]
    public void Min_UlongBackedEnum_MixedSignedRange_MatchesDotNet()
    {
        using TestDatabase db = Seed();

        List<UlongEnumRow> seed =
        [
            new UlongEnumRow { Id = 1, Value = SampleUlongEnum.Mid },
            new UlongEnumRow { Id = 2, Value = SampleUlongEnum.Max },
            new UlongEnumRow { Id = 3, Value = SampleUlongEnum.AboveLongMax },
        ];

        SampleUlongEnum oracle = seed.Min(x => x.Value);
        SampleUlongEnum actual = db.Table<UlongEnumRow>().Min(x => x.Value);

        Assert.Equal(SampleUlongEnum.Mid, oracle);
        Assert.Equal(oracle, actual);
    }

    [Fact]
    public void GroupMax_UlongBackedEnum_MixedSignedRange_MatchesDotNet()
    {
        using TestDatabase db = Seed();

        List<UlongEnumRow> seed =
        [
            new UlongEnumRow { Id = 1, Value = SampleUlongEnum.Mid },
            new UlongEnumRow { Id = 2, Value = SampleUlongEnum.Max },
            new UlongEnumRow { Id = 3, Value = SampleUlongEnum.AboveLongMax },
        ];

        SampleUlongEnum oracle = seed.GroupBy(_ => 1)
            .Select(g => g.Max(x => x.Value))
            .First();

        SampleUlongEnum actual = db.Table<UlongEnumRow>()
            .GroupBy(_ => 1)
            .Select(g => g.Max(x => x.Value))
            .First();

        Assert.Equal(SampleUlongEnum.Max, oracle);
        Assert.Equal(oracle, actual);
    }

    [Fact]
    public void GroupMin_UlongBackedEnum_MixedSignedRange_MatchesDotNet()
    {
        using TestDatabase db = Seed();

        List<UlongEnumRow> seed =
        [
            new UlongEnumRow { Id = 1, Value = SampleUlongEnum.Mid },
            new UlongEnumRow { Id = 2, Value = SampleUlongEnum.Max },
            new UlongEnumRow { Id = 3, Value = SampleUlongEnum.AboveLongMax },
        ];

        SampleUlongEnum oracle = seed.GroupBy(_ => 1)
            .Select(g => g.Min(x => x.Value))
            .First();

        SampleUlongEnum actual = db.Table<UlongEnumRow>()
            .GroupBy(_ => 1)
            .Select(g => g.Min(x => x.Value))
            .First();

        Assert.Equal(SampleUlongEnum.Mid, oracle);
        Assert.Equal(oracle, actual);
    }
}
