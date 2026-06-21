#if !SQLITE_FRAMEWORK_SOURCE_GENERATOR
using System.Linq;
using System.Text;
using System.ComponentModel.DataAnnotations;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

internal sealed class CeNaNRow
{
    [Key]
    public int Id { get; set; }

    public string Name { get; set; } = "";

    public double Price { get; set; }
}

public class ClientEvalNaNComparisonParityTests
{
    [Fact]
    public void ClientEvalComparisonsAgainstNaN_MatchLinqToObjects()
    {
        using TestDatabase db = new();
        db.Table<CeNaNRow>().Schema.CreateTable();
        db.Table<CeNaNRow>().Add(new CeNaNRow { Id = 1, Name = "a", Price = 1.55 });
        db.Table<CeNaNRow>().Add(new CeNaNRow { Id = 2, Name = "b", Price = 2.25 });

        double nan = double.NaN;
        float fnan = float.NaN;

        CeNaNRow[] seed =
        [
            new CeNaNRow { Id = 1, Name = "a", Price = 1.55 },
            new CeNaNRow { Id = 2, Name = "b", Price = 2.25 }
        ];

        var expected = seed.OrderBy(x => x.Id)
            .Select(x => new
            {
                N = x.Name.Normalize(NormalizationForm.FormD),
                Gt = x.Price > nan,
                Ge = x.Price >= nan,
                Lt = x.Price < nan,
                Le = x.Price <= nan,
                GtLeft = nan > x.Price,
                FloatGt = (float)x.Price > fnan
            })
            .ToList();

        var actual = db.Table<CeNaNRow>().OrderBy(x => x.Id)
            .Select(x => new
            {
                N = x.Name.Normalize(NormalizationForm.FormD),
                Gt = x.Price > nan,
                Ge = x.Price >= nan,
                Lt = x.Price < nan,
                Le = x.Price <= nan,
                GtLeft = nan > x.Price,
                FloatGt = (float)x.Price > fnan
            })
            .ToList();

        Assert.Equal(expected, actual);
    }
}
#endif
