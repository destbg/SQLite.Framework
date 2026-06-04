using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Enums;
using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

#if !SQLITE_FRAMEWORK_BUNDLED && !SQLITECIPHER && !NO_SQLITEPCL_RAW_BATTERIES
[Table("WinOrders")]
file sealed class WinOrder
{
    [Key]
    public int Id { get; set; }

    public int CustomerId { get; set; }

    public double Amount { get; set; }
}

file sealed class WinSumResult
{
    public int Id { get; set; }

    public double Total { get; set; }
}

public class WindowProbeTests
{
    [Fact]
    public void GroupsFrameIsGuardedByMinimumVersion()
    {
        using TestDatabase db = new(b => b.UseMinimumSqliteVersion(SQLiteMinimumVersion.V3_27));
        db.Table<WinOrder>().Schema.CreateTable();

        Assert.Throws<NotSupportedException>(() =>
            db.Table<WinOrder>()
                .Select(o => new WinSumResult
                {
                    Id = o.Id,
                    Total = SQLiteWindowFunctions.Sum(o.Amount)
                        .Over()
                        .OrderBy(o.Amount)
                        .Groups(SQLiteFrameBoundary.Preceding(1), SQLiteFrameBoundary.Following(1))
                })
                .ToList());
    }
}
#endif
