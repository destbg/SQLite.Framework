using System.Linq;
using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class CapturedCheckedNarrowingCastParityTests
{
    [Fact]
    public void CapturedCheckedConvertInRange_MatchesLinqToObjects()
    {
        using TestDatabase db = new();
        db.Table<NumericType>().Schema.CreateTable();
        db.Table<NumericType>().Add(new NumericType { Id = 1, ByteValue = 200 });
        db.Table<NumericType>().Add(new NumericType { Id = 2, ByteValue = 10 });

        int captured = 200;

        NumericType[] seed =
        [
            new NumericType { Id = 1, ByteValue = 200 },
            new NumericType { Id = 2, ByteValue = 10 }
        ];
        List<int> expected = seed.Where(x => x.ByteValue == checked((byte)captured)).Select(x => x.Id).OrderBy(i => i).ToList();

        List<int> actual = db.Table<NumericType>().Where(x => x.ByteValue == checked((byte)captured)).Select(x => x.Id).OrderBy(i => i).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void CapturedCheckedNegateInRange_MatchesLinqToObjects()
    {
        int[] data = [-3, -1, 0, 3, 5];
        using TestDatabase db = new();
        db.Table<NumericType>().Schema.CreateTable();
        for (int i = 0; i < data.Length; i++)
        {
            db.Table<NumericType>().Add(new NumericType { Id = i + 1, IntValue = data[i] });
        }

        int captured = 3;

        List<int> expected = data.Where(v => v == checked(-captured)).OrderBy(v => v).ToList();

        List<int> actual = db.Table<NumericType>().Where(n => n.IntValue == checked(-captured)).Select(n => n.IntValue).OrderBy(v => v).ToList();

        Assert.Equal(expected, actual);
    }
}
