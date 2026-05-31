using System.Globalization;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Enums;
using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("DecimalCultureRows")]
file sealed class DecimalCultureRow
{
    [Key]
    public int Id { get; set; }
    public decimal Price { get; set; }
}

public class DecimalTextCultureTests
{
    private static readonly CultureInfo CommaDecimalCulture = new("de-DE");

    [Fact]
    public void DecimalTextWhereWorksUnderCommaDecimalCulture()
    {
        CultureInfo original = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CommaDecimalCulture;

            using TestDatabase db = new(b => b.UseDecimalStorage(DecimalStorageMode.Text));
            db.Table<DecimalCultureRow>().Schema.CreateTable();
            db.Table<DecimalCultureRow>().Add(new DecimalCultureRow { Id = 1, Price = 1.9m });

            List<DecimalCultureRow> rows = db.Table<DecimalCultureRow>().Where(r => r.Price > 1.5m).ToList();

            Assert.Single(rows);
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }

    [Fact]
    public void DecimalTextStorageIsCultureInvariantAcrossMachines()
    {
        CultureInfo original = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CommaDecimalCulture;

            using TestDatabase db = new(b => b.UseDecimalStorage(DecimalStorageMode.Text));
            db.Table<DecimalCultureRow>().Schema.CreateTable();
            db.Table<DecimalCultureRow>().Add(new DecimalCultureRow { Id = 1, Price = 1.5m });

            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

            decimal readBack = db.Table<DecimalCultureRow>().First().Price;

            Assert.Equal(1.5m, readBack);
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }
}
