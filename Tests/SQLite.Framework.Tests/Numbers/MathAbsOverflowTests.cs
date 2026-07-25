using SQLite.Framework.Exceptions;
using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class MathAbsOverflowTests
{
    [Fact]
    public void AbsLongMinValueThrowsSqliteException()
    {
        using TestDatabase db = new();
        db.Table<NumericType>().Schema.CreateTable();
        db.Table<NumericType>().Add(new NumericType { Id = 1, LongValue = long.MinValue });

        Assert.Throws<OverflowException>(() =>
        {
            long _ = new[] { long.MinValue }.Select(v => Math.Abs(v)).First();
        });

        Assert.Throws<SQLiteException>(() =>
            db.Table<NumericType>().Where(x => x.Id == 1).Select(x => Math.Abs(x.LongValue)).First());
    }
}
