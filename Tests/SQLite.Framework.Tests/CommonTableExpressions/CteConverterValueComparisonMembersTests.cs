using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Enums;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public readonly struct H23gScore : IEquatable<H23gScore>
{
    public H23gScore(int points)
    {
        Points = points;
    }

    public int Points { get; }

    public bool Equals(H23gScore other) => Points == other.Points;

    public override bool Equals(object? obj) => obj is H23gScore other && Equals(other);

    public override int GetHashCode() => Points.GetHashCode();

    public static bool operator ==(H23gScore left, H23gScore right) => left.Equals(right);

    public static bool operator !=(H23gScore left, H23gScore right) => !left.Equals(right);
}

public sealed class H23gScoreConverter : ISQLiteTypeConverter
{
    public SQLiteColumnType ColumnType => SQLiteColumnType.Integer;

    public object? ToDatabase(object? value) => value is H23gScore score ? (long)score.Points : 0L;

    public object? FromDatabase(object? value) => value is long stored ? new H23gScore((int)stored) : new H23gScore(0);
}

[Table("H23gScoreRows")]
public class H23gScoreRow
{
    [Key]
    public int Id { get; set; }

    public H23gScore Score { get; set; }

    public int Extra { get; set; }
}

public class CteConverterValueComparisonMembersTests
{
    [Fact]
    public void TwoConverterValueEqualityMembersBesideArrayMemberKeepTheirOwnValues()
    {
        using TestDatabase db = Setup();

        H23gScore low = new(10);
        H23gScore high = new(30);

        List<(int Id, bool IsLow, bool IsHigh)> expected = Rows()
            .Select(r => new { r.Id, IsLow = r.Score == low, IsHigh = r.Score == high, Tags = new[] { r.Extra } })
            .Select(x => (x.Id, x.IsLow, x.IsHigh))
            .OrderBy(t => t.Id)
            .ToList();

        List<(int Id, bool IsLow, bool IsHigh)> actual = db.With(() => db.Table<H23gScoreRow>()
                .Select(r => new { r.Id, IsLow = r.Score == low, IsHigh = r.Score == high, Tags = new[] { r.Extra } }))
            .Select(x => new { x.Id, x.IsLow, x.IsHigh })
            .ToList()
            .Select(x => (x.Id, x.IsLow, x.IsHigh))
            .OrderBy(t => t.Id)
            .ToList();

        Assert.Equal(expected, actual);
    }

    private static List<H23gScoreRow> Rows()
    {
        return
        [
            new H23gScoreRow { Id = 1, Score = new H23gScore(10), Extra = 100 },
            new H23gScoreRow { Id = 2, Score = new H23gScore(30), Extra = 200 },
            new H23gScoreRow { Id = 3, Score = new H23gScore(50), Extra = 300 }
        ];
    }

    private static TestDatabase Setup()
    {
        TestDatabase db = new(b => b.AddTypeConverter<H23gScore>(new H23gScoreConverter()));
        db.Table<H23gScoreRow>().Schema.CreateTable();
        db.Table<H23gScoreRow>().AddRange(Rows());
        return db;
    }
}
