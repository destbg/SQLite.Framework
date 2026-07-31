using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H26mSetterFoldRows")]
public class H26mSetterFoldRow
{
    [Key]
    public int Id { get; set; }

    public int Num { get; set; }
}

public class H26mGuardedBox
{
    private int amount;

    public int Amount
    {
        get => amount;
        set
        {
            if (value < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }

            amount = value;
        }
    }
}

public class CapturedInitializerSetterExceptionParityTests
{
    [Fact]
    public void ReportsAThrowingCapturedPropertySetterTheSameWayAsLinqToObjects()
    {
        int rejected = RejectedAmount();
        using TestDatabase db = Seed();
        List<H26mSetterFoldRow> local = Rows();

        Assert.Throws<ArgumentOutOfRangeException>(() => local
            .Where(r => r.Num == new H26mGuardedBox { Amount = rejected }.Amount)
            .Select(r => r.Id)
            .ToList());

        Assert.Throws<ArgumentOutOfRangeException>(() => db.Table<H26mSetterFoldRow>()
            .Where(r => r.Num == new H26mGuardedBox { Amount = rejected }.Amount)
            .Select(r => r.Id)
            .ToList());
    }

    private static int RejectedAmount()
    {
        return -1;
    }

    private static List<H26mSetterFoldRow> Rows()
    {
        return
        [
            new H26mSetterFoldRow { Id = 1, Num = 1 },
            new H26mSetterFoldRow { Id = 2, Num = 2 }
        ];
    }

    private static TestDatabase Seed()
    {
        TestDatabase db = new();
        db.Table<H26mSetterFoldRow>().Schema.CreateTable();
        db.Table<H26mSetterFoldRow>().AddRange(Rows());
        return db;
    }
}
