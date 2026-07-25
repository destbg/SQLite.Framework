using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Enums;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public enum H21cBigMode : ulong
{
    Zero = 0,
    Half = 1UL << 62,
    High = 1UL << 63,
    Top = ulong.MaxValue,
}

[Table("H21cBigModeRows")]
public class H21cBigModeRow
{
    [Key]
    public int Id { get; set; }

    public H21cBigMode Mode { get; set; }
}

public class EnumTextStorageUnsignedRealCastTests
{
    [Fact]
    public void RealCastUnderTextStorageMatchesDotNet()
    {
        using TestDatabase db = NewDb(EnumStorageMode.Text, out List<H21cBigModeRow> rows);

        List<double> expected = rows.OrderBy(r => r.Id).Select(r => (double)r.Mode).ToList();
        List<double> actual = db.Table<H21cBigModeRow>().OrderBy(x => x.Id).Select(x => (double)x.Mode).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void RealCastUnderIntegerStorageMatchesDotNet()
    {
        using TestDatabase db = NewDb(EnumStorageMode.Integer, out List<H21cBigModeRow> rows);

        List<double> expected = rows.OrderBy(r => r.Id).Select(r => (double)r.Mode).ToList();
        List<double> actual = db.Table<H21cBigModeRow>().OrderBy(x => x.Id).Select(x => (double)x.Mode).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void RealCastThroughUnderlyingTypeUnderTextStorageMatchesDotNet()
    {
        using TestDatabase db = NewDb(EnumStorageMode.Text, out List<H21cBigModeRow> rows);

        List<double> expected = rows.OrderBy(r => r.Id).Select(r => (double)(ulong)r.Mode).ToList();
        List<double> actual = db.Table<H21cBigModeRow>().OrderBy(x => x.Id).Select(x => (double)(ulong)x.Mode).ToList();

        Assert.Equal(expected, actual);
    }

    private static TestDatabase NewDb(EnumStorageMode mode, out List<H21cBigModeRow> rows)
    {
        TestDatabase db = new(b => b.EnumStorage = mode);
        db.Table<H21cBigModeRow>().Schema.CreateTable();
        rows =
        [
            new H21cBigModeRow { Id = 1, Mode = H21cBigMode.Zero },
            new H21cBigModeRow { Id = 2, Mode = H21cBigMode.Half },
            new H21cBigModeRow { Id = 3, Mode = H21cBigMode.High },
            new H21cBigModeRow { Id = 4, Mode = H21cBigMode.Top },
        ];
        db.Table<H21cBigModeRow>().AddRange(rows);
        return db;
    }
}
