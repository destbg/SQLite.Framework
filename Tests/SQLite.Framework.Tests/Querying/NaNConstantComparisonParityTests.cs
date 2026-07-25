using System.Linq;
using System.ComponentModel.DataAnnotations;
using SQLite.Framework;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

internal sealed class NaNComparisonRow
{
    [Key]
    public int Id { get; set; }

    public double Price { get; set; }
}

public class NaNConstantComparisonParityTests
{
    [Fact]
    public void CapturedNaNComparisonInProjection_MatchesLinqToObjects()
    {
        using TestDatabase db = new();
        db.Table<NaNComparisonRow>().Schema.CreateTable();
        db.Table<NaNComparisonRow>().Add(new NaNComparisonRow { Id = 1, Price = 1.5 });

        double nan = double.NaN;
        float fnan = float.NaN;

        var oracle = new[] { new NaNComparisonRow { Id = 1, Price = 1.5 } }
            .OrderBy(x => x.Id)
            .Select(x => new { x.Id, Eq = nan == nan, Neq = nan != nan, FEq = fnan == fnan, FNeq = fnan != fnan, ColEq = x.Price == nan, ColNeq = x.Price != nan })
            .ToList();
        var actual = db.Table<NaNComparisonRow>()
            .OrderBy(x => x.Id)
            .Select(x => new { x.Id, Eq = nan == nan, Neq = nan != nan, FEq = fnan == fnan, FNeq = fnan != fnan, ColEq = x.Price == nan, ColNeq = x.Price != nan })
            .ToList();

        Assert.Equal(oracle, actual);
    }

    [Fact]
    public void ClientEvalNaNComparison_MatchesLinqToObjects()
    {
        using TestDatabase db = new();
        db.Table<NaNComparisonRow>().Schema.CreateTable();
        db.Table<NaNComparisonRow>().Add(new NaNComparisonRow { Id = 1, Price = 1.5 });

        double nan = double.NaN;

        var oracle = new[] { new NaNComparisonRow { Id = 1, Price = 1.5 } }
            .OrderBy(x => x.Id)
            .Select(x => new { Eq = InterceptorHelpers.IdentityDouble(x.Price) == nan, Neq = InterceptorHelpers.IdentityDouble(x.Price) != nan })
            .ToList();
        var actual = db.Table<NaNComparisonRow>()
            .OrderBy(x => x.Id)
            .Select(x => new { Eq = InterceptorHelpers.IdentityDouble(x.Price) == nan, Neq = InterceptorHelpers.IdentityDouble(x.Price) != nan })
            .ToList();

        Assert.Equal(oracle, actual);
    }
}
