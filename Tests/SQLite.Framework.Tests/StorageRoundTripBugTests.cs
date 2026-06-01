using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Enums;
using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("BugDecimalRows")]
file sealed class DecimalRow
{
    [Key]
    public int Id { get; set; }

    public decimal Value { get; set; }
}

[Table("BugCharRows")]
file sealed class CharRow
{
    [Key]
    public int Id { get; set; }

    public char Value { get; set; }
}

public class StorageRoundTripBugTests
{
    [Fact]
    public void DecimalMaxValueReadsBackWithoutOverflow()
    {
        using TestDatabase db = new();
        db.Table<DecimalRow>().Schema.CreateTable();
        db.Table<DecimalRow>().Add(new DecimalRow { Id = 1, Value = decimal.MaxValue });

        decimal read = db.Table<DecimalRow>().Single().Value;

        Assert.Equal(decimal.MaxValue, read);
    }

    [Fact]
    public void DecimalMinValueReadsBackWithoutOverflow()
    {
        using TestDatabase db = new();
        db.Table<DecimalRow>().Schema.CreateTable();
        db.Table<DecimalRow>().Add(new DecimalRow { Id = 1, Value = decimal.MinValue });

        decimal read = db.Table<DecimalRow>().Single().Value;

        Assert.Equal(decimal.MinValue, read);
    }

    [Fact]
    public void LoneSurrogateCharRoundTripsUnderIntegerStorage()
    {
        using TestDatabase db = new(b => b.UseCharStorage(CharStorageMode.Integer));
        db.Table<CharRow>().Schema.CreateTable();
        char original = '\uD83D';
        db.Table<CharRow>().Add(new CharRow { Id = 1, Value = original });

        char read = db.Table<CharRow>().Single().Value;

        Assert.Equal(original, read);
    }
}
