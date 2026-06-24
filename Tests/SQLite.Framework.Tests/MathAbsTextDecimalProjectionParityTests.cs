using System;
using System.Linq;
using SQLite.Framework.Enums;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class MathAbsTextDecimalProjectionParityTests
{
    [Fact]
    public void AbsProjectionOnHighPrecisionTextDecimal_RoundsThroughFloat()
    {
        using TestDatabase db = new(b => b.UseDecimalStorage(DecimalStorageMode.Text));
        db.Table<DecimalTextMathRow>().Schema.CreateTable();
        db.Table<DecimalTextMathRow>().Add(new DecimalTextMathRow { Id = 1, Amount = -9999999999999999.5m });

        decimal oracle = db.Table<DecimalTextMathRow>().AsEnumerable().Select(r => Math.Abs(r.Amount)).First();
        Assert.Equal(9999999999999999.5m, oracle);

        decimal actual = db.Table<DecimalTextMathRow>().Select(r => Math.Abs(r.Amount)).First();
        Assert.NotEqual(oracle, actual);
    }
}
