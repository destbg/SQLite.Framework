using System.Linq;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class ConverterBoolBareWhereParityTests
{
    [Fact]
    public void BareBoolPredicate_TextConverter_MatchesNoRows()
    {
        using TestDatabase db = new(b => b.AddTypeConverter(typeof(bool), new GenYesNoBoolConverter()));
        db.Table<GenConvBoolRow>().Schema.CreateTable();
        db.Table<GenConvBoolRow>().AddRange(
        [
            new GenConvBoolRow { Id = 1, Flag = true },
            new GenConvBoolRow { Id = 2, Flag = false },
            new GenConvBoolRow { Id = 3, Flag = true }
        ]);

        List<int> actual = db.Table<GenConvBoolRow>().Where(r => r.Flag).Select(r => r.Id).OrderBy(x => x).ToList();

        Assert.Empty(actual);
    }

    [Fact]
    public void EqualsTrueBoolPredicate_TextConverter_MatchesLinqToObjects()
    {
        using TestDatabase db = new(b => b.AddTypeConverter(typeof(bool), new GenYesNoBoolConverter()));
        db.Table<GenConvBoolRow>().Schema.CreateTable();
        List<GenConvBoolRow> seed =
        [
            new GenConvBoolRow { Id = 1, Flag = true },
            new GenConvBoolRow { Id = 2, Flag = false },
            new GenConvBoolRow { Id = 3, Flag = true }
        ];
        db.Table<GenConvBoolRow>().AddRange(seed);

        List<int> expected = seed.Where(r => r.Flag).Select(r => r.Id).OrderBy(x => x).ToList();

        List<int> actual = db.Table<GenConvBoolRow>().Where(r => r.Flag == true).Select(r => r.Id).OrderBy(x => x).ToList();

        Assert.Equal(expected, actual);
    }
}
