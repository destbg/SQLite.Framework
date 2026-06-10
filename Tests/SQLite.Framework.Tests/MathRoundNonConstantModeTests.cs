using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class MathRoundNonConstantModeTests
{
    private static TestDatabase Prices(params double[] prices)
    {
        TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        int id = 1;
        foreach (double p in prices)
        {
            db.Table<Book>().Add(new Book { Id = id++, Title = "t", AuthorId = 1, Price = p });
        }
        return db;
    }

    [Fact]
    public void MathRound_ThreeArg_NonConstantMode_DoesNotCrash()
    {
        using TestDatabase db = Prices(2.5);

        MidpointRounding mode = MidpointRounding.AwayFromZero;

        double oracle = Math.Round(2.5, 0, mode);

        double actual = db.Table<Book>()
            .Where(b => b.Id == 1)
            .Select(b => Math.Round(b.Price, 0, mode))
            .First();

        Assert.Equal(oracle, actual);
    }

    [Fact]
    public void MathRound_TwoArg_NonConstantMode_DoesNotTreatModeAsDigitCount()
    {
        using TestDatabase db = Prices(2.5);

        MidpointRounding mode = MidpointRounding.AwayFromZero;

        double oracle = Math.Round(2.5, mode);

        double actual = db.Table<Book>()
            .Where(b => b.Id == 1)
            .Select(b => Math.Round(b.Price, mode))
            .First();

        Assert.Equal(oracle, actual);
    }

    [Theory]
    [InlineData(MidpointRounding.AwayFromZero)]
    [InlineData(MidpointRounding.ToEven)]
    public void MathRound_TwoArg_NonConstantMode_PinnedValues(MidpointRounding mode)
    {
        using TestDatabase db = Prices(2.5, 3.5, 4.5);

        List<double> oracle = new[] { 2.5, 3.5, 4.5 }
            .Select(x => Math.Round(x, mode))
            .ToList();

        List<double> actual = db.Table<Book>()
            .OrderBy(b => b.Id)
            .Select(b => Math.Round(b.Price, mode))
            .ToList();

        Assert.Equal(oracle, actual);
    }
}
