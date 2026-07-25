using System.Globalization;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class TextColumnCultureTests
{
    [Theory]
    [InlineData("de-DE")]
    [InlineData("fr-FR")]
    public void ExecuteScalarDoubleFromTextRespectsInvariantCulture(string culture)
    {
        CultureInfo original = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo(culture);

            using TestDatabase db = new();

            double expected = double.Parse("1234.5", CultureInfo.InvariantCulture);
            double actual = db.ExecuteScalar<double>("SELECT '1234.5'");

            Assert.Equal(expected, actual);
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }

    [Theory]
    [InlineData("de-DE")]
    [InlineData("fr-FR")]
    public void QueryDoubleFromTextRespectsInvariantCulture(string culture)
    {
        CultureInfo original = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo(culture);

            using TestDatabase db = new();

            double expected = double.Parse("3.14", CultureInfo.InvariantCulture);
            double actual = db.Query<double>("SELECT '3.14'")[0];

            Assert.Equal(expected, actual);
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }
}
