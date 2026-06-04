using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Enums;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("DateOnlyFormatRows")]
file sealed class DateOnlyFormatRow
{
    [Key]
    public int Id { get; set; }

    public DateOnly Day { get; set; }
}

[Table("DateTimeFormatRows")]
file sealed class DateTimeFormatRow
{
    [Key]
    public int Id { get; set; }

    public DateTime Moment { get; set; }
}

[Table("DecimalFormatRows")]
file sealed class DecimalFormatRow
{
    [Key]
    public int Id { get; set; }

    public decimal Amount { get; set; }
}

public class StorageFormatReadProbeTests
{
    [Fact]
    public void DateOnlyCustomDayFirstFormatRoundTrips()
    {
        using TestDatabase db = new(b =>
        {
            b.DateOnlyStorage = DateOnlyStorageMode.Text;
            b.DateOnlyFormat = "dd/MM/yyyy";
        });
        db.Table<DateOnlyFormatRow>().Schema.CreateTable();
        db.Table<DateOnlyFormatRow>().Add(new DateOnlyFormatRow { Id = 1, Day = new DateOnly(2000, 2, 25) });

        DateOnly back = db.Table<DateOnlyFormatRow>().First().Day;

        Assert.Equal(new DateOnly(2000, 2, 25), back);
    }

    [Fact]
    public void DateTimeTextFormattedAllDigitFormatRoundTrips()
    {
        using TestDatabase db = new(b =>
        {
            b.DateTimeStorage = DateTimeStorageMode.TextFormatted;
            b.DateTimeFormat = "yyyyMMddHHmmss";
        });
        db.Table<DateTimeFormatRow>().Schema.CreateTable();
        db.Table<DateTimeFormatRow>().Add(new DateTimeFormatRow { Id = 1, Moment = new DateTime(2000, 2, 3, 4, 5, 6) });

        DateTime back = db.Table<DateTimeFormatRow>().First().Moment;

        Assert.Equal(new DateTime(2000, 2, 3, 4, 5, 6), back);
    }

    [Fact]
    public void DecimalTextCurrencyFormatRoundTrips()
    {
        using TestDatabase db = new(b => b.UseDecimalStorage(DecimalStorageMode.Text, "C2"));
        db.Table<DecimalFormatRow>().Schema.CreateTable();
        db.Table<DecimalFormatRow>().Add(new DecimalFormatRow { Id = 1, Amount = 1234.5m });

        decimal back = db.Table<DecimalFormatRow>().First().Amount;

        Assert.Equal(1234.5m, back);
    }
}
