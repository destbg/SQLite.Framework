using System;
using System.Linq;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class FloatToInt64OverflowParityTests
{
    [Fact]
    public void ConvertToInt64_AboveLongRange_ClampsInsteadOfThrowing()
    {
        using TestDatabase db = new();
        db.Table<ConvertIntRow>().Schema.CreateTable();
        db.Table<ConvertIntRow>().Add(new ConvertIntRow { Id = 1, D = 1e19 });

        Assert.Throws<OverflowException>(() => Convert.ToInt64(1e19));

        long actual = db.Table<ConvertIntRow>().Where(r => r.Id == 1).Select(r => Convert.ToInt64(r.D)).First();

        Assert.Equal(long.MaxValue, actual);
    }
}
